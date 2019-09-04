using Amazon;
using Amazon.KinesisFirehose;
using Amazon.KinesisFirehose.Model;
using Amazon.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sitecore.XConnect.Streaming
{
    public class KinesisProducer : IDisposable
    {
        private readonly IAmazonKinesisFirehose _kinesisClient;

        public string DeliveryStreamName { get; }

        public RegionEndpoint AwsRegion { get; }

        public KinesisProducer(string deliveryStreamName, RegionEndpoint region = null)
        {
            DeliveryStreamName = deliveryStreamName;
            AwsRegion = region ?? RegionEndpoint.USWest2; // Set a default region

            _kinesisClient = CreateKinesisFirehouseClient(AwsRegion);
        }

        public async Task PutRecordsAsJson(List<object> dataRecords)
        {
            var kinesisRecords = dataRecords
                .Select(r => r.ToJson().ToKinesisRecord())
                .ToList();

            var putRecordsRequest = new PutRecordBatchRequest()
            {
                DeliveryStreamName = DeliveryStreamName,
                Records = kinesisRecords
            };

            try
            {
                var reponse = await _kinesisClient.PutRecordBatchAsync(putRecordsRequest);

                //TODO: need to check for failed records and retry if need to
            }
            catch (AmazonServiceException awsEx)
            {
                //LogThis(awsEx)
                throw;
            }
            catch (Exception ex)
            {
                //LogThis(ex)
                throw;
            }
        }

        private IAmazonKinesisFirehose CreateKinesisFirehouseClient(RegionEndpoint region)
        {
            // credentials will be read from User\.aws\ folder
            return new AmazonKinesisFirehoseClient(region);
        }

        public void Dispose()
        {
            _kinesisClient.Dispose();
        }
    }
}
