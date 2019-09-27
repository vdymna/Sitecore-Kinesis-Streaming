# Sitecore Kinesis Firehouse Streaming Data Pipeline
 
The data pipeline to push Sitecore's experience analytics data (xDB) into Amazon Redshift in near real time. Amazon Kinesis Data Firehose delivery stream serves as the backbone for the pipeline. Amazon S3 is used as the staging area for the data before it's copied into the Redshift table. The pipeline logic includes checkpointing functionality that records the last processed data (based on a timestamp) and Amazon DynamoDB used as the storage for the checkpointing.

## Getting Started
This instructions are designed to get you up and running on your local dev machine. CloudFormation is used to setup various AWS resources (see [AWS CloudFormation Getting Started](https://aws.amazon.com/cloudformation/getting-started/) for more info) 

### Prerequisites
1. You can leverage an existing Redshift dev cluster or create new single-node cluster using [RedshiftClusterStack.yaml](aws/cloudformation/RedshiftClusterStack.yaml) CloudFormation template.
2. Connect to your Redshift dev database and create the destination table using this [redshift_schema.sql](aws/redshift/redshift_schema.sql) script.
3. Deploy [SitecoreStreamingRedshiftDestinationStack.yaml](aws/cloudformation/SitecoreStreamingRedshiftDestinationStack.yaml) CloudFormation template which will create Kinesis Firehouse delivery stream, S3 staging bucket, DynamoDB table and required IAM resources. But you do have to provide following parameters:
   * DeliveryStreamName
 
