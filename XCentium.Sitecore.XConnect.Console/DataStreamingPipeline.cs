using Microsoft.Extensions.Configuration;
using Sitecore.DataStreaming.Handlers;
using Sitecore.DataStreaming.Providers;
using Sitecore.XConnect;
using Sitecore.XConnect.Client;
using Sitecore.XConnect.Collection.Model;
using Sitecore.XConnect.Streaming.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sitecore.DataStreaming
{
    public class DataStreamingPipeline
    {
        private readonly XConnectProvider _xConnectProvider;
        private readonly KinesisFirehoseProducer _kinesisProducer;
        private readonly DynamoDbCheckpointTracker _checkpointTracker;

        private readonly IConfiguration _config;

        private const int XConnectQueryBatchSize = 100;
        private static readonly DateTime QueryInitialTimestamp = new DateTime(2018, 08, 30, 0, 0, 0, DateTimeKind.Utc);

        public DataStreamingPipeline(IConfiguration config)
        {
            _config = config;

            _xConnectProvider = new XConnectProvider(config);
            _kinesisProducer = new KinesisFirehoseProducer(config);
            _checkpointTracker = new DynamoDbCheckpointTracker(config);
        }

        public async Task RunAsync()
        {
            var xConnectClient = await _xConnectProvider.CreateXConnectClient();

            var lastCheckpointDate = await _checkpointTracker.GetLastCheckpoint(_kinesisProducer.DeliveryStreamName);
            var newCheckpointDate = DateTime.UtcNow;

            var queryBatchEnumerator = await CreateXConnectQuery(xConnectClient,
                                                                 lastCheckpointDate ?? QueryInitialTimestamp,
                                                                 newCheckpointDate,
                                                                 XConnectQueryBatchSize);

            while (await queryBatchEnumerator.MoveNext())
            {
                var recordBatch = queryBatchEnumerator.Current;
                var transformedRecords = await CreateRecordsProjection(recordBatch);

                await _kinesisProducer.PutRecordsAsJson(transformedRecords);
            }

            await _checkpointTracker.CreateNewCheckpoint(_kinesisProducer.DeliveryStreamName, newCheckpointDate);

            System.Console.ReadKey(); // Only for testing
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

                return interaction.Events.OfType<PageViewEvent>().Select(pageView => new PageViewInteractionDto()
                {
                    ContactId = contact.Id,
                    EmailAddress = emailListFacet.PreferredEmail?.SmtpAddress,
                    InteractionId = interaction.Id,
                    SiteName = webVisitFacet.SiteName,
                    UserAgent = interaction.UserAgent,
                    IpAddress = ipInfoFacet.IpAddress,
                    PageViewEventId = pageView.Id,
                    Timestamp = pageView.Timestamp,
                    Duration = pageView.Duration,
                    Url = pageView.Url
                });
            })
            .ToList();

            return await Task.FromResult(projection);
        }
    }
}
