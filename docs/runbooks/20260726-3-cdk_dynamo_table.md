# Runbook: Provisioning Orders DynamoDB Table using .NET CDK

## 🛠️ Step 1: Initialize the C# CDK Project
Navigate into your existing `cdk` directory within the microservices repository and initialize the .NET project template.

```bash
# Navigate to your infrastructure folder
cd cdk

# Initialize the AWS CDK application using C#
cdk init app --language csharp
```

---

## 📦 Step 2: Install DynamoDB Construct Dependencies
Add the official AWS DynamoDB package from NuGet to the underlying infrastructure project.

```bash
# Navigate to the source folder where the C# project file (.csproj) resides
cd src/Cdk

# Install the Amazon DynamoDB library
dotnet add package Amazon.CDK.AWS.DynamoDB
```

---

## 💻 Step 3: Implement the Stack Definition
Open your auto-generated stack file (e.g., `CdkStack.cs`) and replace its content with this production-ready, pay-per-request DynamoDB architecture configuration:

```csharp
using Amazon.CDK;
using Amazon.CDK.AWS.DynamoDB;
using Constructs;

namespace Cdk
{
    public class CdkStack : Stack
    {
        internal CdkStack(Construct scope, string id, IStackProps props = null) : base(scope, id, props)
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
```

---

## 🚀 Step 4: Verification and Deployment
Execute the compilation and CloudFormation provisioning loop from the root of your `cdk` directory.

```bash
# Return to the main cdk directory containing cdk.json
cd ../..

# Bootstrap your environment (Only needed once per AWS Account/Region)
cdk bootstrap

# Synthesize into pure AWS CloudFormation JSON template to check for syntax errors
cdk synth

# Deploy infrastructure resources live to AWS
cdk deploy
```

---

## ✅ Expected Results
Upon successful deployment, the console will print out the following outputs block matching your creation:
- **Outputs**: `CdkStack.OrdersTableNameOutput = Orders`
- The table will instantly reflect as `ACTIVE` inside your AWS DynamoDB Console Management layout.
