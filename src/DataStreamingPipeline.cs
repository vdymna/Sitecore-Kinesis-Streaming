using Microsoft.Extensions.Configuration;
using Sitecore.Streaming.Services;
using Sitecore.Streaming.Providers;
using Sitecore.XConnect;
using Sitecore.XConnect.Client;
using Sitecore.XConnect.Collection.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Sitecore.Streaming.Utilities;
using System.Diagnostics;
using Sitecore.Streaming.Dtos;

namespace Sitecore.Streaming
{
    public class DataStreamingPipeline : IDisposable
    {
        private readonly IConfiguration _config;
        private readonly ILogger _logger;

        private XConnectProvider _xConnectProvider;
        private KinesisProducer _kinesisProducer;
        private CheckpointTracker _checkpointTracker;

        private const int XConnectQueryBatchSize = 100;
        private static readonly TimeSpan SingleExecutionTime = new TimeSpan(0, 0, 20);
        private static readonly DateTime QueryInitialTimestamp = new DateTime(2018, 08, 30, 0, 0, 0, DateTimeKind.Utc);


        public DataStreamingPipeline(IConfiguration config, ILogger logger)
        {
            _config = config;
            _logger = logger;
        }

        public void Initialize()
        {
            try
            {
                _xConnectProvider = new XConnectProvider(_config);
                _kinesisProducer = new KinesisProducer(_config, _logger);
                _checkpointTracker = new CheckpointTracker(_config);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex);
                throw;
            }
        }

        public async Task RunAsync(TimeSpan maxRunTime)
        {          
            var currentExecution = Stopwatch.StartNew();

            _logger.LogInfo("Starting...");

            while (currentExecution.Elapsed < maxRunTime.Subtract(SingleExecutionTime))
            {
                await RunOnceAsync();
            }

            _logger.LogInfo("Shutting down...");

            currentExecution.Stop();
        }

        public async Task RunOnceAsync()
        {
            try
            {
                var xConnectClient = await _xConnectProvider.CreateXConnectClient();

                var lastCheckpointDate = await _checkpointTracker.GetLastCheckpoint(_kinesisProducer.DeliveryStreamName);
                var newCheckpointDate = DateTime.UtcNow;

                var queryBatchEnumerator = await CreateXConnectQuery(xConnectClient,
                                                                     lastCheckpointDate ?? QueryInitialTimestamp,
                                                                     newCheckpointDate,
                                                                     XConnectQueryBatchSize);

                _logger.LogInfo($"xConnnet query returned {queryBatchEnumerator.TotalCount} record(s).");

                while (await queryBatchEnumerator.MoveNext())
                {
                    var recordBatch = queryBatchEnumerator.Current;
                    var transformedRecords = await CreateRecordsProjection(recordBatch);

                    await _kinesisProducer.PutRecordsAsJson(transformedRecords);
                }

                await _checkpointTracker.CreateNewCheckpoint(_kinesisProducer.DeliveryStreamName, newCheckpointDate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex);
            }
        }

        private async Task<IAsyncEntityBatchEnumerator<Interaction>> CreateXConnectQuery(XConnectClient xConnectClient, 
                                                                                         DateTime startDate,
                                                                                         DateTime endDate,
                                                                                         int queryBatchSize )
        {
            var query = xConnectClient.Interactions
                        .Where(i => i.Events.OfType<PageViewEvent>().Any())
                        .Where(i => i.StartDateTime > startDate && i.StartDateTime <= endDate);

            var expandOptions = new InteractionExpandOptions(WebVisit.DefaultFacetKey, IpInfo.DefaultFacetKey)
            {
                Contact = new RelatedContactExpandOptions(EmailAddressList.DefaultFacetKey)
            };

            query = query.WithExpandOptions(expandOptions);

            return await query.GetBatchEnumerator(queryBatchSize);
        }

        private async Task<List<PageViewInteractionDto>> CreateRecordsProjection(IEnumerable<Interaction> interactions)
        {
            var projection = interactions.SelectMany(interaction =>
            {
                var contact = interaction.Contact as Contact;

                var emailListFacet = contact.GetFacet<EmailAddressList>();
                var ipInfoFacet = interaction.GetFacet<IpInfo>();
                var webVisitFacet = interaction.GetFacet<WebVisit>();

                return interaction.Events.OfType<PageViewEvent>().Select(pageView => 
                    new PageViewInteractionDto()
                    {
                        PageViewEventId = pageView.Id,
                        EventTimestamp = pageView.Timestamp.ToString("u"),
                        InteractionId = interaction.Id,
                        ContactId = contact.Id,
                        Url = pageView.Url,
                        DurationInSeconds = Convert.ToInt32(pageView.Duration.TotalSeconds),
                        SiteName = webVisitFacet.SiteName,
                        EmailAddress = emailListFacet.PreferredEmail?.SmtpAddress,
                        UserAgent = interaction.UserAgent,
                        IpAddress = ipInfoFacet.IpAddress
                    }
                );
            })
            .ToList();

            return await Task.FromResult(projection);
        }

        public void Dispose()
        {
            _kinesisProducer.Dispose();
            _checkpointTracker.Dispose();
        }
    }
}
