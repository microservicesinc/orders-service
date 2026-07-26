# Runbook: Implementing Create Order Minimal API Endpoint with DDD & Clean Architecture

- **Date**: 2026-07-26
- **Architecture**: Multi-Project Clean Architecture / Domain-Driven Design (DDD)
- **Target Components**: `Orders.Domain`, `Orders.Infrastructure`, `Orders.Application`, `Orders.Api`
- **Objective**: Expose an HTTP POST Minimal API endpoint to create a new order, validating data constraints at the domain level and persisting records into the centralized LocalStack DynamoDB instance.

---

## 📂 Target Multi-Project File Tree Placement

Review the new architectural files and components added across your solution layers below:

```text
.
├── docs/
│   └── runbooks/
│       └── 20260726-7-create-order-endpoint.md  # This runbook document
└── src/
    ├── Orders.Api/
    │   ├── Endpoints/
    │   │   └── OrderEndpoints.cs              # [MODIFIED] Append the HTTP POST route mapper
    │   ├── Program.cs                         # [MODIFIED] Register CreateOrderCommandHandler
    ├── Orders.Application/
    │   └── Commands/
    │       ├── CreateOrderCommand.cs          # [NEW] Core CQRS command request model (DTO)
    │       └── CreateOrderCommandHandler.cs   # [NEW] Business handler executing domain logic
    └── Orders.Domain/
        └── Core/
            └── Domain/
                └── Order.cs                   # [REVEALED] Business invariants wrapper
```

---

## 🛠️ Step-by-Step Code Implementation

### 1. Orders.Application — CQRS Command Orchestration Layer

#### File: `src/Orders.Application/Commands/CreateOrderCommand.cs` [NEW]
Define a lightweight record representing the payload input parameters required from the API consumer:

```csharp
namespace Orders.Application.Commands
{
    // Incoming request DTO for creating an order
    public record CreateOrderCommand(string ItemId, string TraceId);
}
```

#### File: `src/Orders.Application/Commands/CreateOrderCommandHandler.cs` [NEW]
Build the orchestration engine. It translates the incoming command properties into an explicit, domain-validated instance and instructs the repository to persist it:

```csharp
using System;
using System.Threading.Tasks;
using Orders.Domain.Core.Domain;
using Orders.Domain.Infrastructure.Data;

namespace Orders.Application.Commands
{
    public class CreateOrderCommandHandler
    {
        private readonly IOrderRepository _repository;

        public CreateOrderCommandHandler(IOrderRepository repository)
        {
            _repository = repository;
        }

        public async Task<Order> HandleAsync(CreateOrderCommand command)
        {
            // 1. Generate an identity value conforming to business rules
            string generatedOrderId = $"ORD_{Guid.NewGuid().ToString("N").ToUpper()[..12]}";

            // 2. Instantiate the pure Domain Entity (triggers internal invariant protection validation checks)
            var newOrder = new Order(
                id: generatedOrderId,
                itemId: command.ItemId,
                traceId: string.IsNullOrWhiteSpace(command.TraceId) ? Guid.NewGuid().ToString("N") : command.TraceId
            );

            // 3. Persist the valid domain entity state to infrastructure storage layers
            await _repository.SaveAsync(newOrder);

            return newOrder;
        }
    }
}
```

---

### 2. Orders.Api — Presentation Layer Routing Configuration

#### File: `src/Orders.Api/Endpoints/OrderEndpoints.cs` [MODIFIED]
Append the HTTP POST Minimal API mapping routine into your global endpoints file to capture traffic and pass dependencies safely:

```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System;
using System.Threading.Tasks;
using Orders.Application.Commands; // <-- ADDED NAMESPACE
using Orders.Application.Queries;

namespace Orders.Api.Endpoints
{
    public static class OrderEndpoints
    {
        public static void MapOrderEndpoints(this IEndpointRouteBuilder routes)
        {
            // GET Route
            routes.MapGet("api/orders", async (GetAllOrdersQueryHandler queryHandler) =>
            {
                var query = new GetAllOrdersQuery();
                var results = await queryHandler.HandleAsync(query);
                return Results.Ok(results);
            })
            .WithName("GetAllOrders");

            // -----------------------------------------------------------
            // ⚠️ MODIFICATION: Append POST Minimal API Endpoint Route
            // -----------------------------------------------------------
            routes.MapPost("api/orders", async (CreateOrderCommand command, CreateOrderCommandHandler commandHandler) =>
            {
                try
                {
                    var createdOrder = await commandHandler.HandleAsync(command);
                    
                    // Return 201 Created status pointing back to resource tracking endpoints
                    return Results.Created(\$"api/orders/{createdOrder.Id}", createdOrder);
                }
                catch (ArgumentException ex)
                {
                    // Catch validation invariant issues thrown by the Domain Layer safely
                    return Results.BadRequest(new { error = ex.Message });
                }
            })
            .WithName("CreateOrder");
        }
    }
}
```

#### File: `src/Orders.Api/Program.cs` [MODIFIED]
Register your new structural write handler instance inside your service collection container initialization routine block:

```csharp
using Orders.Api.Configuration;
using Orders.Api.Data;
using Orders.Api.Endpoints;
using Orders.Application.Commands; // <-- ADDED NAMESPACE REFERENCE
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
    
    builder.Services.AddScoped<GetAllOrdersQueryHandler>();
    
    // -----------------------------------------------------------
    // ⚠️ MODIFICATION: Register command handler dependency scope
    // -----------------------------------------------------------
    builder.Services.AddScoped<CreateOrderCommandHandler>();
}

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseOpenApi();
    app.UseSwaggerUi();

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

## 🚀 Execution Verification Workflow

1. Trigger your development engine watcher loop from your terminal:
   ```bash
   cd src/Orders.Api
   dotnet watch run
   ```
2. Open up your running interactive NSwag/Swagger UI tracking environment layout.
3. Open the `POST /api/orders` route container layout and send a testing raw application payload body schema like this:
   ```json
   {
     "itemId": "ITM_992348A1B",
     "traceId": "c4b92f3577b34da6a3ce929d0e0e4736"
   }
   ```
4. Verify that the server engine processes the request cleanly, giving a `201 Created` status code and printing out the final structure containing your freshly generated `id` property attribute values.
5. Re-run your `GET /api/orders` endpoint route to confirm that your newly added order shows up perfectly in your database collection array tracking logs!
