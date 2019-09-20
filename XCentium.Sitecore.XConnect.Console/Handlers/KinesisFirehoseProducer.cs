using Amazon;
using Amazon.KinesisFirehose;
using Amazon.KinesisFirehose.Model;
using Microsoft.Extensions.Configuration;
using Sitecore.DataStreaming.Extensions;
using Sitecore.DataStreaming.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Sitecore.DataStreaming.Handlers
{
    public class KinesisFirehoseProducer : IDisposable
    {
        public string DeliveryStreamName { get; }

        private readonly IAmazonKinesisFirehose _kinesisFirehoseClient;

        private readonly IConfiguration _config;
        private readonly ILogger _logger;

        //private readonly string _deliveryStreamName;
        private readonly RegionEndpoint _region;

        private const int TotalMaxRetries = 6;
        private int _totalRetries = 0;
        private const int ServiceUnavailableMaxRetries = 4;
        private int _serviceUnavailableRetries = 0;
        private const int FailedRecordsMaxRetries = 4;
        private int _failedRecordsMaxRetries = 0;


        public KinesisFirehoseProducer(IConfiguration config)
        {
            _config = config;
            _logger = new ConsoleLogger();

            DeliveryStreamName = _config.GetValue<string>("aws:kinesisStream");
            _region = RegionEndpoint.GetBySystemName(_config.GetValue<string>("aws:region"));

            if (string.IsNullOrEmpty(DeliveryStreamName)) throw new ArgumentNullException("deliveryStreamName");

            _kinesisFirehoseClient = CreateKinesisFirehouseClient(_region);
        }

        public async Task PutRecordsAsJson<T>(List<T> records)
        {
            var kinesisRecords = records
                .Select(r => r.ToJsonWithNewline().ToKinesisRecord())
                .ToList();
            
            foreach (var chunkOfRecords in kinesisRecords.SplitIntoChunks())
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
                response = await _kinesisFirehoseClient.PutRecordBatchAsync(DeliveryStreamName, kinesisRecords);

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

        public void Dispose()
        {
            _kinesisFirehoseClient.Dispose();
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
    }
}
