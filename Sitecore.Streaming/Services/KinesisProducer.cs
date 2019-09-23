using Amazon;
using Amazon.KinesisFirehose;
using Amazon.KinesisFirehose.Model;
using Amazon.Runtime;
using Microsoft.Extensions.Configuration;
using Polly;
using Polly.Retry;
using Sitecore.Streaming.Extensions;
using Sitecore.Streaming.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Sitecore.Streaming.Services
{
    public class KinesisProducer : IDisposable
    {
        public string DeliveryStreamName { get; }

        private readonly IAmazonKinesisFirehose _kinesisFirehoseClient;

        private readonly IConfiguration _config;
        private readonly ILogger _logger;

        private readonly RegionEndpoint _region;

        private const int ServiceExceptionMaxRetries = 4;
        private const int FailedRecordsMaxRetries = 10;

        private int _failedRecordsRetriesCount = 0;


        public KinesisProducer(IConfiguration config, ILogger logger)
        {
            _config = config;
            _logger = logger;

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

            // each record record must be <= 1,000 KB
            // whole request must be under 4 MB
            foreach (var chunkOfRecords in kinesisRecords.SplitIntoChunks())
            {
                _logger.LogInfo($"Putting {chunkOfRecords.Count} record(s) into the Kinesis stream.");
                var response = await AttemptPutRecords(chunkOfRecords);

                ResetRetryCounter();
            }
        }


        public async Task<PutRecordBatchResponse> AttemptPutRecords(List<Record> kinesisRecords)
        {
            var retryPolicy = GetExceptionRetryPolicy();

            PutRecordBatchResponse response = null;
            kinesisRecords = kinesisRecords.Take(3).ToList();

            await retryPolicy.ExecuteAsync(async () =>
            {
                response = await _kinesisFirehoseClient.PutRecordBatchAsync(DeliveryStreamName, kinesisRecords);

                if (response.FailedPutCount > 0)
                {
                    await RetryFailedRecords(response, kinesisRecords);
                }
            });

            return response;
        }

        private async Task RetryFailedRecords(PutRecordBatchResponse response, List<Record> originalRecords)
        {
            var failedRecords = new List<Record>();

            for (int i = 0; i < originalRecords.Count; i++)
            {
                var recordResponse = response.RequestResponses[i];
                if (!string.IsNullOrEmpty(recordResponse?.ErrorCode))
                {
                    var originalRecord = originalRecords[i];
                    failedRecords.Add(originalRecord);
                }
            }

            if (failedRecords.Count > 0 && _failedRecordsRetriesCount < FailedRecordsMaxRetries)
            {
                _failedRecordsRetriesCount++;
                Thread.Sleep(TimeSpan.FromSeconds(_failedRecordsRetriesCount * 2));
                _logger.LogInfo($"Retrying {failedRecords.Count} failed record(s).");

                await AttemptPutRecords(failedRecords);
            }
            else if (failedRecords.Count > 0)
            {
                foreach (var record in failedRecords)
                {
                    _logger.LogInfo(string.Format("Not able to put a record: {0}", new StreamReader(record.Data).ReadToEnd()));
                }
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

        private AsyncRetryPolicy GetExceptionRetryPolicy()
        {
            return Policy
                .Handle<AmazonKinesisFirehoseException>(ex => (int)ex.StatusCode / 100 == 5)
                .Or<AmazonServiceException>(ex => (int)ex.StatusCode / 100 == 5)
                .WaitAndRetryAsync(
                    ServiceExceptionMaxRetries,
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    (ex, timeSpan) =>
                    {
                        _logger.LogError(ex);
                        _logger.LogInfo("Retrying...");
                    }
                );
        }

        private void ResetRetryCounter()
        {
            _failedRecordsRetriesCount = 0; // reset this value
        }
    }
}
