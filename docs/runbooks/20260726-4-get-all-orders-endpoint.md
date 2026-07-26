# Runbook: Implementing Get All Orders Minimal API Endpoint with DDD & Clean Architecture

- **Date**: 2026-07-26
- **Architecture**: Multi-Project Clean Architecture / Domain-Driven Design (DDD)
- **Target Components**: `Orders.Domain`, `Orders.Infrastructure`, `Orders.Application`, `Orders.Api`
- **Objective**: Expose a clean, fast Minimal API HTTP GET endpoint to retrieve all seeded synthetic order entries from the centralized LocalStack DynamoDB instance using the CQRS pattern.

---

## 📂 Target Multi-Project File Tree Placement

Review the structural updates mapped across your decoupled architecture layers below:

```text
.
├── docs/
│   └── runbooks/
│       └── 20260726-6-get-all-orders-endpoint.md # This runbook document
└── src/
    ├── Orders.Api/
    │   ├── Endpoints/
    │   │   └── OrderEndpoints.cs              # [NEW] Minimal API route definition mapper
    │   ├── Program.cs                         # [MODIFIED] Invoke MapOrderEndpoints routing setups
    ├── Orders.Application/
    │   └── Queries/
    │       ├── GetAllOrdersQuery.cs           # [NEW] Core CQRS query definition object
    │       └── GetAllOrdersQueryHandler.cs    # [NEW] Business handler orchestrating data fetch
    ├── Orders.Domain/
    │   └── Infrastructure/
    │   │   └── Data/
    │   │       └── IOrderRepository.cs        # [MODIFIED] Added GetAllAsync signature contract
    └── Orders.Infrastructure/
        └── Data/
            └── DynamoOrderRepository.cs       # [MODIFIED] Implemented DynamoDB table Scan logic
```

---

## 🛠️ Step-by-Step Code Implementation

### 1. Orders.Domain — Core Interface Enhancement

#### File: `src/Orders.Domain/Infrastructure/Data/IOrderRepository.cs` [MODIFIED]
Add the asynchronous multi-record collection tracking signature to the baseline repository interface boundary:

```csharp
using System.Collections.Generic;
using System.Threading.Tasks;
using Orders.Domain.Core.Domain;

namespace Orders.Domain.Infrastructure.Data
{
    public interface IOrderRepository
    {
        Task<bool> HasAnyOrdersAsync();
        Task SaveAsync(Order order);
        Task<IEnumerable<Order>> GetAllAsync(); // <-- ADDED CONTRACT SIGNATURE
    }
}
```

---

### 2. Orders.Infrastructure — Data Retrieval Engine

#### File: `src/Orders.Infrastructure/Data/DynamoOrderRepository.cs` [MODIFIED]
Implement the data conversion scan mechanism to query all table rows and reconstruct clean domain entities:

```csharp
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Orders.Domain.Core.Domain;
using Orders.Domain.Infrastructure.Data;

namespace Orders.Infrastructure.Data
{
    public class DynamoOrderRepository : IOrderRepository
    {
        private readonly IAmazonDynamoDB _dynamoClient;
        private const string TableName = "Orders";

        public DynamoOrderRepository(IAmazonDynamoDB dynamoClient)
        {
            _dynamoClient = dynamoClient;
        }

        public async Task<bool> HasAnyOrdersAsync()
        {
            var request = new ScanRequest { TableName = TableName, Limit = 1 };
            var response = await _dynamoClient.ScanAsync(request);
            return response.Count > 0;
        }

        public async Task SaveAsync(Order order)
        {
            var dbModel = OrderDynamoDbModel.FromDomain(order);
            var request = new PutItemRequest
            {
                TableName = TableName,
                Item = new Dictionary<string, AttributeValue>
                {
                    { "PK", new AttributeValue { S = dbModel.PK } },
                    { "SK", new AttributeValue { S = dbModel.SK } },
                    { "id", new AttributeValue { S = dbModel.Id } },
                    { "itemId", new AttributeValue { S = dbModel.ItemId } },
                    { "traceId", new AttributeValue { S = dbModel.TraceId } },
                    { "status", new AttributeValue { S = dbModel.Status } },
                    { "createdAt", new AttributeValue { S = dbModel.CreatedAt } }
                }
            };
            await _dynamoClient.PutItemAsync(request);
        }

        // -----------------------------------------------------------
        // 🛠️ IMPLEMENTATION: Scan table rows and map back to Domain
        // -----------------------------------------------------------
        public async Task<IEnumerable<Order>> GetAllAsync()
        {
            var request = new ScanRequest { TableName = TableName };
            var response = await _dynamoClient.ScanAsync(request);
            var orders = new List<Order>();

            foreach (var item in response.Items)
            {
                var order = new Order(
                    id: item.TryGetValue("id", out var idVal) ? idVal.S : string.Empty,
                    itemId: item.TryGetValue("itemId", out var itemVal) ? itemVal.S : string.Empty,
                    traceId: item.TryGetValue("traceId", out var traceVal) ? traceVal.S : string.Empty,
                    status: item.TryGetValue("status", out var statusVal) ? statusVal.S : "PENDING"
                );
                orders.Add(order);
            }

            return orders;
        }
    }
}
```

---

### 3. Orders.Application — CQRS Query Orchestration Layer

#### File: `src/Orders.Application/Queries/GetAllOrdersQuery.cs` [NEW]
Define a lightweight data object representing the lookup action request:

```csharp
namespace Orders.Application.Queries
{
    // Represents the record query message contract
    public record GetAllOrdersQuery();
}
```

#### File: `src/Orders.Application/Queries/GetAllOrdersQueryHandler.cs` [NEW]
Build the underlying operational execution flow that requests data straight from the isolated domain abstractions:

```csharp
using System.Collections.Generic;
using System.Threading.Tasks;
using Orders.Domain.Core.Domain;
using Orders.Domain.Infrastructure.Data;

namespace Orders.Application.Queries
{
    public class GetAllOrdersQueryHandler
    {
        private readonly IOrderRepository _repository;

        public GetAllOrdersQueryHandler(IOrderRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<Order>> HandleAsync(GetAllOrdersQuery query)
        {
            return await _repository.GetAllAsync();
        }
    }
}
```

---

### 4. Orders.Api — Presentation Mapping

#### File: `src/Orders.Api/Endpoints/OrderEndpoints.cs` [NEW]
Create a dedicated Minimal API endpoint mapping class to decouple path definitions from `Program.cs`:

```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System.Threading.Tasks;
using Orders.Application.Queries;

namespace Orders.Api.Endpoints
{
    public static class OrderEndpoints
    {
        public static void MapOrderEndpoints(this IEndpointRouteBuilder routes)
        {
            routes.MapGet("api/orders", async (GetAllOrdersQueryHandler queryHandler) =>
            {
                var query = new GetAllOrdersQuery();
                var results = await queryHandler.HandleAsync(query);
                return Results.Ok(results);
            })
            .WithName("GetAllOrders")
            .WithOpenApi(); // Generates tracking metadata layout inside your Swagger view
        }
    }
}
```

#### File: `src/Orders.Api/Program.cs` [MODIFIED]
Register application query infrastructure and append the Minimal API router builder extensions cleanly:

```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orders.Api.Configuration;
using Orders.Api.Data;
using Orders.Api.Endpoints;
using Orders.Application.Queries; 
using Orders.Domain.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();

// NSwag OpenApi spec generation engine configuration
builder.Services.AddOpenApiDocument(options => 
{
    options.Title = "Orders API";
    options.Version = "v1";
});

// Configure development dependencies safely before builder.Build()
if (builder.Environment.IsDevelopment())
{
    // This extension method automatically registers IAmazonDynamoDB AND maps IOrderRepository to DynamoOrderRepository
    builder.Services.AddLocalDynamoDb(builder.Configuration);
    
    // Register the Application Layer query handler abstraction
    builder.Services.AddScoped<GetAllOrdersQueryHandler>();
}

// Build the application host instance container (Locks service graph as Read-Only)
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseOpenApi();      // Serves the OpenAPI specification file
    app.UseSwaggerUi();    // Serves the interactive Swagger UI interface webpage

    // Database seeding runtime sequence
    using var scope = app.Services.CreateScope();
    var orderRepository = scope.ServiceProvider.GetRequiredService<IOrderRepository>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    await OrderSeeder.SeedAsync(orderRepository, logger);
}

app.UseHttpsRedirection();

// Map Minimal API route endpoints configurations
app.MapOrderEndpoints();

app.Run();
```

---

## 🚀 Execution Verification Workflow

1. Start up your continuous hot reload watcher from the API directory workspace:
   ```bash
   cd src/Orders.Api
   dotnet watch run
   ```
2. Open your web browser and navigate directly to your Swagger index:
   `http://localhost:<YOUR_PORT>/swagger/index.html`
3. Execute the new `GET /api/orders` endpoint route block.
4. Verify that the server sends a valid `200 OK` JSON array back containing the 5 custom trace-linked entries seeded on runtime startup!
