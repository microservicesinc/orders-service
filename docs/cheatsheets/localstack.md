# LocalStack & awslocal CLI Cheatsheet

## 1. LocalStack Management CLI (localstack)

| Action | Command |
| :--- | :--- |
| Start LocalStack | localstack start -d |
| Stop LocalStack | localstack stop |
| Check Status | localstack status |
| View Container Logs | localstack logs -f |
| Set Auth Token (Pro) | localstack auth set-token <token> |
| Clear Auth Token | localstack auth clear-token |
| Update CLI & Images | localstack update all |

---

## 2. AWS Service Interaction CLI (awslocal)

### Environment Setup (for standard aws CLI)
export AWS_ACCESS_KEY_ID="test"
export AWS_SECRET_ACCESS_KEY="test"
export AWS_DEFAULT_REGION="us-east-1"

### S3 (Simple Storage Service)

| Task | Command |
| :--- | :--- |
| List Buckets | awslocal s3 ls |
| Create Bucket | awslocal s3 mb s3://my-bucket |
| Upload File | awslocal s3 cp ./file.txt s3://my-bucket/ |
| Download File | awslocal s3 cp s3://my-bucket/file.txt ./ |
| List Bucket Contents | awslocal s3 ls s3://my-bucket/ |
| Delete Bucket | awslocal s3 rb s3://my-bucket --force |

### DynamoDB

| Task | Command |
| :--- | :--- |
| List Tables | awslocal dynamodb list-tables |
| Create Table | awslocal dynamodb create-table --table-name Orders --attribute-definitions AttributeName=OrderId,AttributeType=S --key-schema AttributeName=OrderId,KeyType=HASH --billing-mode PAY_PER_REQUEST |
| Describe Table | awslocal dynamodb describe-table --table-name Orders |
| Put Item | awslocal dynamodb put-item --table-name Orders --item '{"OrderId": {"S": "ORD-001"}, "Total": {"N": "100"}}' |
| Get Item | awslocal dynamodb get-item --table-name Orders --key '{"OrderId": {"S": "ORD-001"}}' |
| Scan Table | awslocal dynamodb scan --table-name Orders |
| Delete Table | awslocal dynamodb delete-table --table-name Orders |

### SQS (Simple Queue Service)

| Task | Command |
| :--- | :--- |
| List Queues | awslocal sqs list-queues |
| Create Queue | awslocal sqs create-queue --queue-name order-queue |
| Send Message | awslocal sqs send-message --queue-url http://sqs.us-east-1.localhost.localstack.cloud:4566/000000000000/order-queue --message-body '{"event": "OrderCreated"}' |
| Receive Message | awslocal sqs receive-message --queue-url http://sqs.us-east-1.localhost.localstack.cloud:4566/000000000000/order-queue |
| Purge Queue | awslocal sqs purge-queue --queue-url http://sqs.us-east-1.localhost.localstack.cloud:4566/000000000000/order-queue |

### SNS (Simple Notification Service)

| Task | Command |
| :--- | :--- |
| List Topics | awslocal sns list-topics |
| Create Topic | awslocal sns create-topic --name order-events |
| Publish Message | awslocal sns publish --topic-arn arn:aws:sns:us-east-1:000000000000:order-events --message "Order status updated" |
| Subscribe SQS to SNS | awslocal sns subscribe --topic-arn arn:aws:sns:us-east-1:000000000000:order-events --protocol sqs --notification-endpoint arn:aws:sqs:us-east-1:000000000000:order-queue |

### Lambda

| Task | Command |
| :--- | :--- |
| List Functions | awslocal lambda list-functions |
| Create Function | awslocal lambda create-function --function-name order-processor --runtime python3.9 --handler index.handler --role arn:aws:iam::000000000000:role/lambda-role --zip-file fileb://function.zip |
| Invoke Function | awslocal lambda invoke --function-name order-processor --payload '{"key": "value"}' response.json |
| Get Function Logs | awslocal logs tail /aws/lambda/order-processor |

### Secrets Manager & SSM Parameter Store

| Task | Command |
| :--- | :--- |
| Create Secret | awslocal secretsmanager create-secret --name db-password --secret-string "supersecret123" |
| Get Secret | awslocal secretsmanager get-secret-value --secret-id db-password |
| Put SSM Parameter | awslocal ssm put-parameter --name "/config/app-env" --value "development" --type String |
| Get SSM Parameter | awslocal ssm get-parameter --name "/config/app-env" |

---

## 3. Useful Bash Alias

alias awslocal="aws --endpoint-url=http://localhost:4566"