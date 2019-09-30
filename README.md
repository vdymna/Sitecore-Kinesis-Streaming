# Sitecore Kinesis Firehose Streaming Data Pipeline
 
The data pipeline to extract experience analytics data from Sitecore's Experience Database (xDB) and push it into Amazon Redshift in near real time. Amazon Kinesis Data Firehose delivery stream serves as the backbone for the pipeline. Amazon S3 is used as the staging area for the data before it's copied into a Redshift table. The pipeline logic includes checkpointing functionality that records the last processed datapoint (based on a timestamp) and Amazon DynamoDB used as the storage for the checkpointing.

## Getting Started
This instructions are designed to get you up and running on your local dev machine. CloudFormation is used to setup all the AWS resources (see [AWS CloudFormation Getting Started](https://aws.amazon.com/cloudformation/getting-started/) for more info).

### Prerequisites
1. You can leverage an existing Redshift dev cluster or create new single-node cluster using [RedshiftClusterStack.yaml](aws/cloudformation/RedshiftClusterStack.yaml) CloudFormation template.
2. Connect to your Redshift dev database and create destination table using this [redshift_schema.sql](aws/redshift/redshift_schema.sql) script.
3. Deploy [SitecoreStreamingRedshiftDestinationStack.yaml](aws/cloudformation/SitecoreStreamingRedshiftDestinationStack.yaml) CloudFormation template which will create Kinesis Firehose delivery stream, S3 staging bucket, DynamoDB table and required IAM resources. You do need to provide following parameters when creating the CloudFormation stack:
   * Kinesis delivery stream name
   * DynamoDB checkpoint table name
   * S3 staging bucket name
   * Redshift database connection string
   * Redshift username
   * Redshift password
   * Redshift destination table name
4. Upload [redshift-jsonpaths.json](aws/redshift/redshift-jsonpaths.json) to the S3 staging bucket.
5. Override [settings.xml](src/settings.xml) with your xConnect and AWS settings (see my [blog post](https://xcentium.com/blog/2019/09/10/using-xconnect-client-in-non-sitecore-context) on how to configure xConnect client)

### Running
xConnect required admin permissions, so you must run the solution as an administrator.
