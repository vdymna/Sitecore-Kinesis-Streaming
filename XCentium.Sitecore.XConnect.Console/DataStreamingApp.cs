using Amazon;
using Microsoft.Extensions.Configuration;
using Sitecore.XConnect;
using Sitecore.XConnect.Collection.Model;
using Sitecore.XConnect.Streaming.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sitecore.XConnect.Streaming.Console
{
    public class DataStreamingApp
    {
        private readonly XConnectProvider _xConnectProvider;
        private readonly DynamoDbCheckpointTracker _checkpointTracker;
        private readonly KinesisProducer _kinesisProducer;
        private readonly IConfiguration _config;

        private readonly string _regionName;
        private readonly string _kinesisStreamName;

        private const int XConnectBatchSize = 100;
        private static readonly DateTime InitialCheckpoint = new DateTime(2018, 08, 30).ToUniversalTime();

        public DataStreamingApp(IConfiguration config)
        {
            _config = config;

            _regionName = _config.GetValue<string>("aws:region");
            _kinesisStreamName = _config.GetValue<string>("aws:kinesisStream");
            var checkpointTableName = _config.GetValue<string>("aws:checkpointTable");

            _xConnectProvider = new XConnectProvider(_config);
            _checkpointTracker = new DynamoDbCheckpointTracker(checkpointTableName, _regionName);
            _kinesisProducer = new KinesisProducer(_kinesisStreamName, _regionName);
        }

        public async Task RunAsync()
        {
            var client = await _xConnectProvider.CreateXConnectClient();

            try
            {
                var lastCheckpointTimestamp = await _checkpointTracker.GetLastCheckpointAsUtc(_kinesisStreamName) 
                                                    ?? InitialCheckpoint;
                var maxStartDateTime = DateTime.UtcNow;


                var query = client.Interactions
                            .Where(i => i.Events.OfType<PageViewEvent>().Any())
                            .Where(i => i.StartDateTime > lastCheckpointTimestamp && i.StartDateTime <= maxStartDateTime);

                var expandOptions = new InteractionExpandOptions(WebVisit.DefaultFacetKey, IpInfo.DefaultFacetKey)
                {
                    Contact = new RelatedContactExpandOptions(EmailAddressList.DefaultFacetKey)
                };

                query = query.WithExpandOptions(expandOptions);

                var batchEnumerator = await query.GetBatchEnumerator(XConnectBatchSize);


                while (await batchEnumerator.MoveNext())
                {
                    var interactions = new List<object>();
                    var batch = batchEnumerator.Current; // Batch of <= 500 records

                    foreach (var interaction in batch)
                    {
                        var contact = interaction.Contact as Contact;

                        var emailListFacet = contact.GetFacet<EmailAddressList>();
                        var ipInfoFacet = interaction.GetFacet<IpInfo>();
                        var webVisitFacet = interaction.GetFacet<WebVisit>();

                        foreach (var pageView in interaction.Events.OfType<PageViewEvent>())
                        {
                            var pageViewDto = new PageViewInteractionDto()
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
                            };

                            interactions.Add(pageViewDto);
                        }

                    }

                    await _kinesisProducer.PutRecordsAsJson(interactions);
                }

            }
            catch (XdbExecutionException ex)
            {
                // Handle exception
                throw;
            }
            catch (Exception ex)
            {
                // Handle exception
                throw;
            }

            await _checkpointTracker.CreateCheckpoint(_kinesisStreamName, DateTime.UtcNow);

            System.Console.ReadKey(); // Only for testing
        }
    }
}
