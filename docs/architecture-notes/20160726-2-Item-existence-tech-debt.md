# Tech Debt Note: Serverless Async Validation via AWS Lambda & SQS Choreography

- **Date**: 2026-07-26
- **Topic**: Serverless Architecture / Cross-Service Event Validation / Async Saga
- **Context**: The `POST /api/orders` endpoint accepts an `ItemId`, logs the order in DynamoDB as `PENDING`, and returns an immediate `202 Accepted` response. True structural validation is offloaded to pure serverless event chains, moving away from hosted background worker threads.

---

## 🛑 The Problem / Current Architectural Layout

The `Orders.Api` operates inside an isolated runtime context during request ingestion:
1. The endpoint writes a fast tracking row to DynamoDB as `PENDING` and immediately dispatches a message to an integration queue.
2. It assumes downstream services are valid. If a customer passes a non-existent or malformed `ItemId`, the data remains corrupt in storage until an async serverless workflow explicitly reconciles the discrepancy.

---

## 🛠️ Serverless Asynchronous Architecture Remediation

To eliminate long-running background polling daemons (`IHostedService`), the architecture transitions to a pure, event-driven serverless design using managed AWS Lambda triggers and separate SQS queues.

```text
[Orders.Api] ──(202 Accepted)──> Client
     │
(Persists PENDING)
     │
     ▼
[DynamoDB: Orders Table]
     │
     ▼ (Dispatches Event)
[SQS: OrderPlacedQueue]  <-- Specific to initialization dispatch
     │
     ▼ (Triggers Event Source)
[Inventory Lambda] ──(Queries Catalog / Checks ItemId)
     │
     └── Item Validation Failure ──> Pushes {"OrderId": "X", "Reason": "InvalidItem"} ──┐
                                                                                        ▼
                                                                        [SQS: OrderValidationFailQueue]
                                                                                        │
                                                                                        ▼ (Triggers Function)
                                                                             [Orders Lambda Updater]
                                                                                        │
                                                                       (Updates Status: REJECTED_INVALID_ITEM)
                                                                                        │
                                                                                        ▼
                                                                             [DynamoDB: Orders Table]
                                                                                        │
                                                                                        ▼
                                                                           (Continues Normal Flow...)
```

### Component Details

#### 1. Initial Dispatch Queue (`OrderPlacedQueue`)
The `Orders.Api` no longer targets the generic stock engine. It pushes a lightweight integration event package `{ "OrderId": "ORD_123", "ItemId": "ITM_999", "TraceId": "XYZ" }` onto a dedicated **`OrderPlacedQueue`**.

#### 2. Inventory Lambda Function (`inventory-service`)
An AWS Lambda function triggered directly by the `OrderPlacedQueue`.
- **Validation**: It acts as the product catalog gatekeeper, verifying if the `ItemId` exists and retrieving details (like item names).
- **Branching Action**: 
  - If validation **succeeds**, it proceeds to standard stock reservation tasks (not handled in this scope).
  - If validation **fails** (item does not exist), it skips stock checks and pushes a failure command message `{ "OrderId": "ORD_123", "Reason": "ItemIdDoesNotExist" }` directly onto an Orders-managed queue.

#### 3. Orders Error Reconciliation Queue (`OrderValidationFailQueue`)
An isolated SQS queue completely **managed and owned by the Orders microservice**. It receives failure callback payloads from the downstream inventory validators.

#### 4. Orders Lambda Updater (`orders-service`)
A dedicated, lightweight serverless function built in .NET 10 that is triggered exclusively by messages in the `OrderValidationFailQueue`.
- It processes the validation exception payload asynchronously.
- It bypasses web server contexts to map an atomic update statement straight to the DynamoDB `Orders` table, transitioning the item status from `PENDING` directly to `REJECTED_INVALID_ITEM`.
- **Continuation**: Once this state reconciliation is saved, the item exits this boundary and enters the normal transactional fallback loop.

---

## 💡 Architectural Benefits of Pure Serverless Messaging
- **Zero Compute Waste**: No continuous compute cycles or active polling background threads are consumed. Code executes and charges *only* when a validation fails.
- **Microservice Autonomy**: The Orders Microservice completely owns its database reconciliation engine and input queues (`OrderValidationFailQueue`), keeping boundaries clean.
- **Independent Scaling**: If a massive spike of orders occurs, SQS buffers the traffic, and AWS Lambda auto-scales horizontally to process validations without impacting API latency.

---

## 📖 Reference Material & Links
- [AWS Lambda deployment models for .NET 10](https://amazon.com)
- [Using AWS Lambda with Amazon SQS standard queues](https://amazon.com)
- [Serverless Saga Pattern Orchestrations](https://amazon.com)
