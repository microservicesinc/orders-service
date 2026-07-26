# Runbook: Partitioning a Domain-Driven Design Seeder into a Multi-Project .NET Solution

- **Date**: 2026-07-26
- **Architecture**: Clean Architecture / Multi-Project Domain-Driven Design (DDD)
- **Target Projects**: `Orders.Domain`, `Orders.Infrastructure`, `Orders.Api`
- **Objective**: Implement clean architectural isolation by placing business objects, data repositories, and startup orchestration into their respective structural projects.

---

## 📂 Target Multi-Project File Tree Placement

Based on your current solution layout, here is where every single new file belongs. This separates domain purity from the structural runtime engine:

```text
.
├── cdk/
│   └── ...
├── docs/
│   └── runbooks/
│       └── 20260726-4-ddd-layered-seeder.md    # This runbook document
├── OrdersService.slnx
└── src/
    ├── Orders.Api/
    │   ├── Configuration/
    │   │   └── DynamoDbSetup.cs               # [NEW] Registers AWS SDK & IOrderRepository mapping
    │   ├── Data/
    │   │   └── OrderSeeder.cs                 # [NEW] Coordinates development seeding loop
    │   ├── appsettings.Development.json        # [MODIFIED] Set LocalStack ServiceURL endpoint
    │   ├── Orders.Api.csproj                  # [MODIFIED] Reference Domain, Infra, and AWS NuGet
    │   └── Program.cs                         # [MODIFIED] Invoke seeder scope during app startup
    ├── Orders.Application/
    │   └── ...
    ├── Orders.Domain/
    │   ├── Core/
    │   │   └── Domain/
    │   │       └── Order.cs                   # [NEW] Pure Domain Entity (No AWS libraries)
    │   ├── Infrastructure/
    │   │   └── Data/
    │   │       └── IOrderRepository.cs        # [NEW] Contract definition for database interactions
    │   └── Orders.Domain.csproj               # [MODIFIED] Clean project file (No AWS dependencies)
    └── Orders.Infrastructure/
        ├── Data/
        │   ├── DynamoOrderRepository.cs       # [NEW] Active AWS client engine implementation
        │   └── OrderDynamoDbModel.cs          # [NEW] Single-Table map wrapper (PK/SK translation)
        └── Orders.Infrastructure.csproj       # [MODIFIED] Reference Domain project and AWSSDK.DynamoDBv2
```

---

## 🛠️ Step-by-Step Code Configuration Implementation

### 1. Orders.Domain — The Pure Core Domain Layer

#### File: `src/Orders.Domain/Core/Domain/Order.cs` [NEW]
This holds pure business objects and properties. It remains completely decoupled from AWS.

```csharp
using System;

namespace Orders.Domain.Core.Domain
{
    public class Order
    {
        public string Id { get; private set; }
        public string ItemId { get; private set; }
        public string TraceId { get; private set; }
        public string Status { get; private set; }
        public DateTime CreatedAt { get; private set; }

        public Order(string id, string itemId, string traceId, string status = "PENDING")
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Order ID cannot be empty.");
            if (string.IsNullOrWhiteSpace(itemId)) throw new ArgumentException("Item ID cannot be empty.");

            Id = id;
            ItemId = itemId;
            TraceId = traceId;
            Status = status;
            CreatedAt = DateTime.UtcNow;
        }
    }
}
```

#### File: `src/Orders.Domain/Infrastructure/Data/IOrderRepository.cs` [NEW]
The contract interface definition lives in the domain project layer, dictating how data must cross boundaries.

```csharp
using System.Threading.Tasks;
using Orders.Domain.Core.Domain;

namespace Orders.Domain.Infrastructure.Data
{
    public interface IOrderRepository
    {
        Task<bool> HasAnyOrdersAsync();
        Task SaveAsync(Order order);
    }
}
```

---

### 2. Orders.Infrastructure — Database Technical Engine

#### Project Setup: `src/Orders.Infrastructure/Orders.Infrastructure.csproj` [MODIFIED]
Ensure your infrastructure data engine targets your Domain layer project and loads the explicit AWS packages:

```xml
<ItemGroup>
  <ProjectReference Include="..\Orders.Domain\Orders.Domain.csproj" />
  <PackageReference Include="AWSSDK.DynamoDBv2" Version="3.7.*" />
</ItemGroup>
```

#### File: `src/Orders.Infrastructure/Data/OrderDynamoDbModel.cs` [NEW]
This object safely translates pure domain parameters into single-table design storage format attributes (PK and SK).

```csharp
using Orders.Domain.Core.Domain;

namespace Orders.Infrastructure.Data
{
    public class OrderDynamoDbModel
    {
        public string PK => $"ORDER#{Id}";
        public string SK => $"ITEM#{ItemId}";
        public string Id { get; set; } = string.Empty;
        public string ItemId { get; set; } = string.Empty;
        public string TraceId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string CreatedAt { get; set; } = string.Empty;

        public static OrderDynamoDbModel FromDomain(Order order) => new()
        {
            Id = order.Id,
            ItemId = order.ItemId,
            TraceId = order.TraceId,
            Status = order.Status,
            CreatedAt = order.CreatedAt.ToString("o")
        };
    }
}
```

#### File: `src/Orders.Infrastructure/Data/DynamoOrderRepository.cs` [NEW]
This concrete file talks to the database, fulfilling the IOrderRepository interface instructions.

```csharp
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
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
    }
}
```

---

### 3. Orders.Api — Startup App Execution Orchestration

#### Project Setup: `src/Orders.Api/Orders.Api.csproj` [MODIFIED]
Ensure your executable presentation layer references your infrastructure layer project:

```xml
<ItemGroup>
  <ProjectReference Include="..\Orders.Infrastructure\Orders.Infrastructure.csproj" />
</ItemGroup>
```

#### File: `src/Orders.Api/Configuration/DynamoDbSetup.cs` [NEW]
Maps your local settings to Dependency Injection, pairing the domain interface to the infrastructure concrete class.

```csharp
using Amazon.DynamoDBv2;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Orders.Domain.Infrastructure.Data;
using Orders.Infrastructure.Data;

namespace Orders.Api.Configuration
{
    public static class DynamoDbSetup
    {
        public static IServiceCollection AddLocalDynamoDb(this IServiceCollection services, IConfiguration configuration)
        {
            var awsOptions = configuration.GetSection("AWS");
            var config = new AmazonDynamoDBConfig
            {
                ServiceURL = awsOptions["ServiceURL"],
                AuthenticationRegion = awsOptions["Region"]
            };

            services.AddSingleton<IAmazonDynamoDB>(new AmazonDynamoDBClient(config));
            services.AddScoped<IOrderRepository, DynamoOrderRepository>();
            
            return services;
        }
    }
}
```

#### File: `src/Orders.Api/Data/OrderSeeder.cs` [NEW]
This generates synthetic entries entirely through the domain interface contract layer.

```csharp
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Orders.Domain.Core.Domain;
using Orders.Domain.Infrastructure.Data;

namespace Orders.Api.Data
{
    public static class OrderSeeder
    {
        public static async Task SeedAsync(IOrderRepository repository, ILogger logger)
        {
            try
            {
                if (await repository.HasAnyOrdersAsync())
                {
                    logger.LogInformation("Database target table already contains data rows. Seeding skipped.");
                    return;
                }

                logger.LogInformation("Database empty. Generating clean architectural Domain models...");

                for (int i = 0; i < 5; i++)
                {
                    var domainOrder = new Order(
                        id: $"ORD_{Guid.NewGuid().ToString("N").ToUpper()[..12]}",
                        itemId: $"ITM_{Guid.NewGuid().ToString("N").ToUpper()[..12]}",
                        traceId: Guid.NewGuid().ToString("N")
                    );

                    await repository.SaveAsync(domainOrder);
                }

                logger.LogInformation("Multi-project architectural database data seed execution succeeded.");
            }
            catch (Exception ex)
            {
            logger.LogError(ex, "An unexpected crash occurred inside the local data bootstrap seeder runtime loop.");
            }
            }
        }
    }
```

#### File: `src/Orders.Api/Program.cs` [MODIFIED]
Wire everything together inside your entry point:

```csharp
using Orders.Api.Configuration;
using Orders.Api.Data;
using Orders.Domain.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddLocalDynamoDb(builder.Configuration);
}

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var orderRepository = scope.ServiceProvider.GetRequiredService<IOrderRepository>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    await OrderSeeder.SeedAsync(orderRepository, logger);
}

app.Run();
```

## ⚙️ Configuration Update: Mapping the Local Pipeline

File: src/Orders.Api/appsettings.Development.json [MODIFIED]

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


## 🚀 Execution Verification Workflow

```sh
cd src/Orders.Api
dotnet watch run
```

Check the application startup outputs in your console to verify that the domain layers and infrastructure tables are properly mapped and initialized.

