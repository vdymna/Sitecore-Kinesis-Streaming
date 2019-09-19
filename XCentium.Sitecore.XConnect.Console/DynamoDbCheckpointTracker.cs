using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sitecore.XConnect.Streaming
{
    public class DynamoDbCheckpointTracker
    {
        private readonly string _tableName = "StreamingCheckpoint";

        private const string DeliveryStreamNameAttribute = "DeliveryStreamName";
        private const string LastCheckpointTimestampAttribute = "LastCheckpointTimestamp";
        private const string ISO8601DateFormat = "o";

        private readonly IAmazonDynamoDB _dynamoDBClient;

        public DynamoDbCheckpointTracker(string tableName, string regionName = null)
        {
            _tableName = tableName;
            var region = regionName == null ? null : RegionEndpoint.GetBySystemName(regionName);

            _dynamoDBClient = CreateDynamoDbClient(region);
        }

        // TODO: exception handeling?
        public async Task CreateCheckpoint(string kinesisStreamName, DateTime lastProcessedTimestamp)
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
                            S = lastProcessedTimestamp.ToString(ISO8601DateFormat)
                        }
                    }
                }
            };

            await _dynamoDBClient.PutItemAsync(request);
        }

        public async Task<DateTime?> GetLastCheckpointAsUtc(string kinesisStreamName)
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
                            S = kinesisStreamName
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
