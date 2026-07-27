# Runbook: Integrating SQS Messaging and Get Order Endpoint in Multi-Project Solution

- **Date**: 2026-07-26
- **Architecture**: Multi-Project Clean Architecture / Domain-Driven Design (DDD)
- **Target Components**: `Orders.Domain`, `Orders.Infrastructure`, `Orders.Application`, `Orders.Api`
- **Objective**: Update the Order placement pipeline to publish an infrastructure event message to AWS SQS (`StockUpdateQueue`) while returning an HTTP 201 Created tracking layout, and expose an HTTP GET endpoint to track order records by ID.

---

## 📂 Target Multi-Project File Tree Placement

Review the updated configuration and new files mapped across your solution layers below:

```text
.
└── src/
    ├── Orders.Api/
    │   ├── Configuration/
    │   │   └── SqsSetup.cs                    # [NEW] Configures IAmazonSQS client for LocalStack
    │   ├── Endpoints/
    │   │   └── OrderEndpoints.cs              # [MODIFIED] Append GET /api/orders/{id} route block
    │   ├── appsettings.Development.json        # [MODIFIED] Add SQS connection parameter configurations
    │   └── Program.cs                         # [MODIFIED] Register SQS services and GetOrderById query
    ├── Orders.Application/
    │   ├── Commands/
    │   │   └── CreateOrderCommandHandler.cs   # [MODIFIED] Inject IAmazonSQS to dispatch message payload
    │   └── Queries/
    │       ├── GetOrderByIdQuery.cs           # [NEW] Core CQRS query definition record
    │       └── GetOrderByIdQueryHandler.cs    # [NEW] Fetch handler extracting single tracking models
    ├── Orders.Domain/
    │   └── Infrastructure/
    │   │   └── Data/
    │   │       └── IOrderRepository.cs        # [MODIFIED] Added GetByIdAsync interface contract
    └── Orders.Infrastructure/
        └── Data/
            └── DynamoOrderRepository.cs       # [MODIFIED] Implemented specific item key lookup query
```

---

## 🛠️ Step-by-Step Code Configuration Implementation

### 1. Project Dependencies & Configuration

#### File: `src/Orders.Api/appsettings.Development.json` [MODIFIED]
Ensure your local configurations provide standard fallback environment settings mapping directly to LocalStack:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AWS": {
    "ServiceURL": "http://localhost:4566",
    "Region": "us-east-1"
  }
}
```

#### File: `src/Orders.Infrastructure/Orders.Infrastructure.csproj` [MODIFIED]
Pull down the official AWS SQS SDK package directly inside your data engine project dependencies group:
```xml
<ItemGroup>
  <ProjectReference Include="..\Orders.Domain\Orders.Domain.csproj" />
  <PackageReference Include="AWSSDK.DynamoDBv2" Version="3.7.*" />
  <PackageReference Include="AWSSDK.SQS" Version="3.7.*" />
</ItemGroup>
```


#### File: `src/Orders.Application/Orders.Application.csproj` [MODIFIED]
Pull down the official AWS SQS SDK package directly inside your data engine project dependencies group:
```xml
<ItemGroup>
  <PackageReference Include="AWSSDK.DynamoDBv2" Version="3.7.*" />
  <PackageReference Include="AWSSDK.SQS" Version="3.7.*" />
</ItemGroup>
```

#### File: `src/Orders.Api/Configuration/SqsSetup.cs` [NEW]
Create this file to register your application's messaging client connected explicitly to your local development environment block:
```csharp
using Amazon.SQS;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Orders.Api.Configuration
{
    public static class SqsSetup
    {
        public static IServiceCollection AddLocalSqs(this IServiceCollection services, IConfiguration configuration)
        {
            var awsOptions = configuration.GetSection("AWS");
            var config = new AmazonSQSConfig
            {
                ServiceURL = awsOptions["ServiceURL"],
                AuthenticationRegion = awsOptions["Region"]
            };

            services.AddSingleton<IAmazonSQS>(new AmazonSQSClient(config));
            return services;
        }
    }
}
```

---

### 2. Orders.Domain & Infrastructure Layer Extensions

#### File: `src/Orders.Domain/Infrastructure/Data/IOrderRepository.cs` [MODIFIED]
Expose the specific domain lookup contract operation signature inside your interface core:
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
        Task<IEnumerable<Order>> GetAllAsync();
        Task<Order?> GetByIdAsync(string id);
    }
}
```

#### File: `src/Orders.Infrastructure/Data/DynamoOrderRepository.cs` [MODIFIED]

Implement the single table design lookup function. It scans or queries for the unique item ID across rows natively:

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

        public async Task<Order?> GetByIdAsync(string id)
        {
            var request = new ScanRequest
            {
                TableName = TableName,
                FilterExpression = "id = :idValue",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    { ":idValue", new AttributeValue { S = id } }
                }
            };

            var response = await _dynamoClient.ScanAsync(request);
            if (response.Items.Count == 0) return null;

            var item = response.Items[0];
            return new Order(
                id: item.TryGetValue("id", out var idVal) ? idVal.S : string.Empty,
                itemId: item.TryGetValue("itemId", out var itemVal) ? itemVal.S : string.Empty,
                traceId: item.TryGetValue("traceId", out var traceVal) ? traceVal.S : string.Empty,
                status: item.TryGetValue("status", out var statusVal) ? statusVal.S : "PENDING"
            );
        }
    }
}
```
---

### 3. Orders.Application Layer — Messaging Integration & CQRS Lookup

#### File: src/Orders.Application/Commands/CreateOrderCommandHandler.cs [MODIFIED]

Inject IAmazonSQS directly into the write pipeline loop handler. After writing the state to DynamoDB, dispatch a serialized stock reservation instruction message straight to the Inventory Microservice queue:
```csharp
using Amazon.SQS;
using Amazon.SQS.Model;
using System;
using System.Text.Json;
using System.Threading.Tasks;
using Orders.Domain.Core.Domain;
using Orders.Domain.Infrastructure.Data;

namespace Orders.Application.Commands
{
    public class CreateOrderCommandHandler
    {
        private readonly IOrderRepository _repository;
        private readonly IAmazonSQS _sqsClient;
        private const string InventoryQueueUrl = "http://localhost:4566/000000000000/StockUpdateQueue";

        public CreateOrderCommandHandler(IOrderRepository repository, IAmazonSQS sqsClient)
        {
            _repository = repository;
            _sqsClient = sqsClient;
        }

        public async Task<Order> HandleAsync(CreateOrderCommand command)
        {
            string generatedOrderId = $"ORD_{Guid.NewGuid().ToString("N").ToUpper()[..12]}";

            var newOrder = new Order(
                id: generatedOrderId,
                itemId: command.ItemId,
                traceId: string.IsNullOrWhiteSpace(command.TraceId) ? Guid.NewGuid().ToString("N") : command.TraceId,
                status: "PENDING"
            );

            await _repository.SaveAsync(newOrder);

            var messagePayload = new
            {
                itemId = newOrder.ItemId,
                quantityChange = -6
            };

            await _sqsClient.SendMessageAsync(new SendMessageRequest
            {
                QueueUrl = InventoryQueueUrl,
                MessageBody = JsonSerializer.Serialize(messagePayload)
            });

            return newOrder;
        }
    }
}
```

#### File: src/Orders.Application/Queries/GetOrderByIdQuery.cs [NEW]

```csharp
namespace Orders.Application.Queries
{
    public record GetOrderByIdQuery(string Id);
}
```

#### File: src/Orders.Application/Queries/GetOrderByIdQueryHandler.cs [NEW]
```csharp
using System.Threading.Tasks;
using Orders.Domain.Core.Domain;
using Orders.Domain.Infrastructure.Data;

namespace Orders.Application.Queries
{
    public class GetOrderByIdQueryHandler
    {
        private readonly IOrderRepository _repository;

        public GetOrderByIdQueryHandler(IOrderRepository repository)
        {
            _repository = repository;
        }

        public async Task<Order?> HandleAsync(GetOrderByIdQuery query)
        {
            return await _repository.GetByIdAsync(query.Id);
        }
    }
}
```
---

### 4. Orders.Api Layer — Routing Endpoints Setup

#### File: src/Orders.Api/Endpoints/OrderEndpoints.cs [MODIFIED]
Map the HTTP POST creation signature to return an HTTP 201 Created status header, and append the single-item HTTP GET tracking lookup route:
```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System.Threading.Tasks;
using Orders.Application.Commands;
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
            .WithName("GetAllOrders");

            routes.MapGet("api/orders/{id}", async (string id, GetOrderByIdQueryHandler queryHandler) =>
            {
                var query = new GetOrderByIdQuery(id);
                var order = await queryHandler.HandleAsync(query);
                
                return order is not null ? Results.Ok(order) : Results.NotFound(new { message = $"Order {id} not found." });
            })
            .WithName("GetOrderById");

            routes.MapPost("api/orders", async (CreateOrderCommand command, CreateOrderCommandHandler commandHandler) =>
            {
                try
                {
                    var createdOrder = await commandHandler.HandleAsync(command);
                    return Results.Created($"api/orders/{createdOrder.Id}", createdOrder);
                }
                catch (System.ArgumentException ex)
                {
                    return Results.BadRequest(new { error = ex.Message });
                }
            })
            .WithName("CreateOrder");
        }
    }
}
```

#### File: src/Orders.Api/Program.cs [MODIFIED]
Register the fresh SQS configuration engines and query lookup handler abstractions inside your core configuration tree:
```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orders.Api.Configuration;
using Orders.Api.Data;
using Orders.Api.Endpoints;
using Orders.Application.Commands; 
using Orders.Application.Queries; 
using Orders.Domain.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApiDocument(options => 
{
    options.Title = "Orders API";
    options.Version = "v1";
});

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddLocalDynamoDb(builder.Configuration);
    builder.Services.AddLocalSqs(builder.Configuration);
    
    builder.Services.AddScoped<GetAllOrdersQueryHandler>();
    builder.Services.AddScoped<GetOrderByIdQueryHandler>();
    builder.Services.AddScoped<CreateOrderCommandHandler>();
}

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseOpenApi();
    app.UseSwaggerUI();

    using var scope = app.Services.CreateScope();
    var orderRepository = scope.ServiceProvider.GetRequiredService<IOrderRepository>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    await OrderSeeder.SeedAsync(orderRepository, logger);
}

app.UseHttpsRedirection();
app.MapOrderEndpoints();
app.Run();
```
---

## 🚀 Execution & Messaging Flow Verification

1. Ensure the target integration message tracking queue exists inside your centralized LocalStack container profile:
```sh
   aws sqs create-queue --queue-name StockUpdateQueue --endpoint-url http://localhost:4566 --region us-east-1
```

2. Start your live file change system daemon thread loop:
```sh
   cd src/Orders.Api
   dotnet watch run
```

3. Issue an HTTP POST request to append a new record item:
```sh
   curl -X POST "http://localhost:5233/api/orders" -H "Content-Type: application/json" -d "{ \"itemId\": \"item1\", \"traceId\": \"777a2f3577b34da6a3ce929d0e0e4736\" }"
```

4. Verify that the integration payload message was pushed safely onto the SQS queue for the Inventory Microservice to handle:
```sh
   aws sqs receive-message --queue-url http://localhost:4566/000000000000/StockUpdateQueue --endpoint-url http://localhost:4566 --region us-east-1
```

5. Call the new status endpoint route to pull down data records live by ID and monitor ongoing processing variations:
```sh
   curl -X GET "http://localhost:5233/api/orders/PASTE_YOUR_GENERATED_ORD_ID_HERE"
```