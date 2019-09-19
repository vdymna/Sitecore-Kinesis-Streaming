using Amazon;
using Amazon.KinesisFirehose;
using Amazon.KinesisFirehose.Model;
using Amazon.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using XCentium.Sitecore.XConnect.Console;

namespace Sitecore.XConnect.Streaming
{
    public class KinesisProducer : IDisposable
    {
        private readonly IAmazonKinesisFirehose _kinesisClient;

        private readonly ILogger _logger;

        public string DeliveryStreamName { get; }

        public RegionEndpoint AwsRegion { get; }

        private const int TotalMaxRetries = 6;
        private int _totalRetries = 0;
        private const int ServiceUnavailableMaxRetries = 4;
        private int _serviceUnavailableRetries = 0;
        private const int FailedRecordsMaxRetries = 4;
        private int _failedRecordsMaxRetries = 0;


        public KinesisProducer(string deliveryStreamName, string regionName = null)
        {
            DeliveryStreamName = deliveryStreamName;
            AwsRegion = regionName == null ? null : RegionEndpoint.GetBySystemName(regionName);

            _logger = new ConsoleLogger();
            _kinesisClient = CreateKinesisFirehouseClient(AwsRegion);
        }

        public async Task PutRecordsAsJson(List<object> records)
        {
            // each record record must be <= 1,000 KB
            // whole request must be under 4 MB
            var kinesisRecords = records
                .Select(r => r.ToJsonWithNewline().ToKinesisRecord())
                .ToList();
            
            foreach (var chunkOfRecords in kinesisRecords.GetRecordChunks())
            {
                _logger.LogInfo($"Putting {chunkOfRecords.Count} records into the Kinesis stream.");
                var response = await AttemptPutRecords(chunkOfRecords);
            }
        }


        public async Task<PutRecordBatchResponse> AttemptPutRecords(List<Record> kinesisRecords)
        {
            // each record record must be <= 1,000 KB
            // whole request must be under 4 MB

            PutRecordBatchResponse response = null;

            try
            {
                response = await _kinesisClient.PutRecordBatchAsync(DeliveryStreamName, kinesisRecords);

                if (response.FailedPutCount > 0)
                {
                    await RetryFailedRecordsOnly(response, kinesisRecords);
                }

            }
            catch (AmazonKinesisFirehoseException ex)
            {
                if ((int)ex.StatusCode / 100 == 5)
                {
                    // need to back off and retry
                    if (_totalRetries <= TotalMaxRetries)
                    {
                        Thread.Sleep(1000 * 5); // wait 5 seconds

                        _totalRetries++;
                        await AttemptPutRecords(kinesisRecords);
                    }
                    else
                    {
                        _totalRetries = 0; // reset retries
                        _logger.LogError(ex);
                        //throw;
                    }
                }
                else
                {
                    _logger.LogError(ex);
                    //throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex);
                //throw;
            }

            return response;
        }

        private async Task RetryFailedRecordsOnly(PutRecordBatchResponse response, List<Record> kinesisRecords)
        {
            var failedRecords = new List<Record>();

            for (int i = 0; i < kinesisRecords.Count; i++)
            {
                var recordResponse = response.RequestResponses[i];
                if (!string.IsNullOrEmpty(recordResponse.ErrorCode))
                {
                    var originalRecord = kinesisRecords[i];
                    failedRecords.Add(originalRecord);
                }
            }

            // TODD: need to track retires on failed records
            if (failedRecords.Count > 0 && _failedRecordsMaxRetries <= FailedRecordsMaxRetries)
            {
                _failedRecordsMaxRetries++;
                await AttemptPutRecords(failedRecords);
            }

        }

        private IAmazonKinesisFirehose CreateKinesisFirehouseClient(RegionEndpoint region = null)
        {
            // credentials will be read from User\.aws\ folder
            if (region != null)
            {
                return new AmazonKinesisFirehoseClient(region);
            }

            return new AmazonKinesisFirehoseClient();
        }

        public void Dispose()
        {
            _kinesisClient.Dispose();
        }
    }
}
