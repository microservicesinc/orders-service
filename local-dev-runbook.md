# Orders Microservice - Local Development Runbook

This guide outlines the step-by-step chronological workflow to spin up infrastructure using AWS CDK, launch the .NET 10 Minimal API locally via LocalStack, and test endpoints using the integrated Swagger (NSwag) UI.

---

## 📋 Prerequisites

Ensure you have the following global utilities installed on your host machine:
* **Docker & Docker Compose**
* **Node.js** (for CDK utilities)
* **.NET 10 SDK**

```bash
# Install the necessary global AWS CDK Local tools
npm install -g aws-cdk-local aws-cdk
```

---

## 🚀 Chronological Execution Steps

### Step 2: Start the LocalStack Container
The centralized LocalStack container must be running from your shared organization infrastructure workspace folder.
```bash
# Verify the centralized local cloud instance is active
docker ps | grep localstack
```

### Step 2: Bootstrap the Local CDK Environment
Initialize the local container space with the tracking parameters and deployment staging assets required by AWS CDK. *(Required only on the very first execution setup).*
```bash
cd cdk
cdklocal bootstrap
```

### Step 3: Deploy Infrastructure via CDK
Compile your C# CDK stack definitions and provision the `Orders` database table inside your LocalStack container.
```bash
cdklocal deploy --require-approval never
cd ..
```

### Step 4: Verify Local App Configuration
Ensure your development profile matches your centralized container mapping layout. Open `src/Orders.Api/appsettings.Development.json` and verify it contains your endpoint configuration:
```json
"AWS": {
  "ServiceURL": "http://localhost:4566",
  "Region": "us-east-1"
}
```

### Step 5: Launch the .NET Minimal API Application
Move into your application presentation directory and run the continuous hot reload code watcher execution thread loop.
```bash
cd src/Orders.Api
dotnet watch run
```
*(On boot, the application pipeline automatically validates database boundaries and pushes 5 synthetic trace-linked records to your table if it is completely empty).*

---

## 🧪 Verification and Testing

### 1. Interactive Documentation (Swagger UI)
Once the compiled runtime engine initializes, open your web browser layout and navigate directly to your local endpoint:
👉 **[http://localhost:5233/swagger/index.html](http://localhost:5233/swagger/index.html)** *(Verify the actual running application port from your terminal console launch settings).*

### 2. Manual CLI Table Verification
To guarantee that your .NET CDK scripts created your physical table correctly inside the active container block, run:
```bash
# Expected output list includes: "Orders"
aws dynamodb list-tables --endpoint-url http://localhost:4566 --region us-east-1
```

### 3. Clean Infrastructure Teardown
To wipe out deployment assets and clear out mock records to a clean slate, terminate your active compilation processes and run:
```bash
# Return to the infrastructure layout folder and destroy resources
cd cdk
cdklocal destroy --force
```

---

## 🛠️ Testing and Local CLI Reference Commands

Set up your terminal alias mapping rule to prevent having to type out long endpoint URL arguments:
```bash
alias awslocal="aws --endpoint-url=http://localhost:4566 --region us-east-1"
```

### Scan Seeded Table Elements Directly
```bash
# Pull down all existing data keys from the table
awslocal dynamodb scan --table-name Orders
```

### Send Manual POST Create Order Payload Wire Frames
```bash
# Submit an HTTP POST request block directly to the Minimal API engine
curl -X POST "http://localhost:5233/api/orders" \
  -H "accept: application/json" \
  -H "Content-Type: application/json" \
  -d "{ \"itemId\": \"item1\", \"traceId\": \"4bf92f3577b34da6a3ce929d0e0e4736\" }"
```

### Getting the item1 data (stock)
```sh
aws dynamodb get-item   --table-name InventoryOrdersTable   --endpoint-url http://localhost:4566   --region us-east-1 \
--keyy '{"PK": {"S": "ITEM#item1"}, "SK": {"S": "METADATA"}}'
```

### Track Collection Growth Live
```bash
# Continuously poll the Minimal API endpoint route to view database items live
watch -n 2 curl -X GET "http://localhost:5233/api/orders" -H "accept: application/json"
```
