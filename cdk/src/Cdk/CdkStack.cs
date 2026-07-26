using Amazon.CDK;
using Amazon.CDK.AWS.DynamoDB;
using Constructs;

namespace Cdk
{
    public class CdkStack : Stack
    {
        public CdkStack(Construct scope, string id, IStackProps props = null) : base(scope, id, props)
        {
            // Define the Orders DynamoDB Table using Single-Table Design
            var ordersTable = new Table(this, "OrdersTable", new TableProps
            {
                TableName = "Orders",
                BillingMode = BillingMode.PAY_PER_REQUEST,

                // Partition Key (e.g., ORDER#12345)
                PartitionKey = new Attribute
                {
                    Name = "PK",
                    Type = AttributeType.STRING
                },

                // Sort Key (e.g., METADATA or ITEM#9876)
                SortKey = new Attribute
                {
                    Name = "SK",
                    Type = AttributeType.STRING
                },

                // Dev/Test Environment Policy: Cleanup table on stack deletion
                RemovalPolicy = RemovalPolicy.DESTROY,
                
                // Automatically purge items using a TTL attribute
                TimeToLiveAttribute = "TTL"
            });

            // Output table name reference for application ingestion
            new CfnOutput(this, "OrdersTableNameOutput", new CfnOutputProps
            {
                Value = ordersTable.TableName,
                Description = "The runtime physical name of the DynamoDB Orders table"
            });
        }
    }
}
