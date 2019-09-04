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
        private readonly CheckpointTracker _checkpointTracker;
        private readonly KinesisProducer _kinesisProducer;

        private const int XConnectBatchSize = 500;

        private static readonly string KinesisStreamName = "Some-Stream-Name";

        public DataStreamingApp()
        {
            _xConnectProvider = new XConnectProvider();
            _checkpointTracker = new CheckpointTracker();
            _kinesisProducer = new KinesisProducer(KinesisStreamName);
        }

        public async Task RunAsync()
        {
            var client = await _xConnectProvider.CreateXConnectClient();

            try
            {
                var lastCheckpointTimestamp = _checkpointTracker.GetLastCheckpoint();
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
                    var pageViewData = new List<object>();
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

                            pageViewData.Add(pageViewDto);
                        }

                    }

                    await _kinesisProducer.PutRecordsAsJson(pageViewData);
                }

            }
            catch (XdbExecutionException ex)
            {
                // Handle exception
                throw;
            }
        }
    }
}
