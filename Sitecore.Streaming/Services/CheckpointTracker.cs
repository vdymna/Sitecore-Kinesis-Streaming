using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sitecore.Streaming.Services
{
    public class CheckpointTracker : IDisposable
    {
        private readonly IAmazonDynamoDB _dynamoDBClient;

        private readonly IConfiguration _config;

        private readonly string _tableName;
        private readonly RegionEndpoint _region;

        private const string DeliveryStreamNameAttribute = "DeliveryStreamName";
        private const string LastCheckpointTimestampAttribute = "LastCheckpointTimestamp";
        private const string ISO8601DateFormat = "o";


        public CheckpointTracker(IConfiguration config)
        {
            _config = config;

            _tableName = _config.GetValue<string>("aws:checkpointTable");
            _region = RegionEndpoint.GetBySystemName(_config.GetValue<string>("aws:region"));

            if (string.IsNullOrEmpty(_tableName)) throw new ArgumentNullException("checkpointTableName");

            _dynamoDBClient = CreateDynamoDbClient(_region);
        }


        public async Task CreateNewCheckpoint(string checkpointIdentifier)
        {
            await CreateNewCheckpoint(checkpointIdentifier, DateTime.UtcNow);
        }

        public async Task CreateNewCheckpoint(string kinesisStreamName, DateTime timestamp)
        {
            var request = new PutItemRequest()
            {
                TableName = _tableName,
                Item = new Dictionary<string, AttributeValue>()
                {
                    {
                        DeliveryStreamNameAttribute,
                        new AttributeValue()
                        {
                            S = kinesisStreamName
                        }
                    },
                    {
                        LastCheckpointTimestampAttribute,
                        new AttributeValue()
                        {
                            S = timestamp.ToString(ISO8601DateFormat)
                        }
                    }
                }
            };

            await _dynamoDBClient.PutItemAsync(request);
        }

        public async Task<DateTime?> GetLastCheckpoint(string checkpointIdentifier)
        {
            var request = new GetItemRequest()
            {
                TableName = _tableName,
                Key = new Dictionary<string, AttributeValue>()
                {
                    {
                        DeliveryStreamNameAttribute,
                        new AttributeValue()
                        {
                            S = checkpointIdentifier
                        }
                    }
                },
                ConsistentRead = true,
                ProjectionExpression = LastCheckpointTimestampAttribute
            };

            var response = await _dynamoDBClient.GetItemAsync(request);

            if (response.Item.Count > 0 && 
                DateTime.TryParse(response.Item[LastCheckpointTimestampAttribute].S, out var checkpoint))
            {
                return checkpoint.ToUniversalTime();
            }
            else
            {
                return null;
            }
        }

        public void Dispose()
        {
            _dynamoDBClient.Dispose();
        }

        private IAmazonDynamoDB CreateDynamoDbClient(RegionEndpoint region = null)
        {
            // credentials will be read from User\.aws\ folder
            if (region != null)
            {
                return new AmazonDynamoDBClient(region);
            }

            return new AmazonDynamoDBClient();
        }
    }
}
