#!/bin/bash
cd ..
set -e

echo "========================================================="
echo "🏗️  Bootstrapping .NET 10 Clean Architecture Microservice"
echo "========================================================="

# 1. Create a clean solution file wrapper
dotnet new sln -n OrdersService

# 2. Compile decoupled class libraries and the web api layer targeting .NET 10
dotnet new classlib -n Orders.Domain -o src/Orders.Domain -f net10.0
dotnet new classlib -n Orders.Application -o src/Orders.Application -f net10.0
dotnet new classlib -n Orders.Infrastructure -o src/Orders.Infrastructure -f net10.0
dotnet new webapi -n Orders.Api -o src/Orders.Api -f net10.0

# 3. Bind all projects into your solution file context
dotnet sln add src/Orders.Domain/Orders.Domain.csproj
dotnet sln add src/Orders.Application/Orders.Application.csproj
dotnet sln add src/Orders.Infrastructure/Orders.Infrastructure.csproj
dotnet sln add src/Orders.Api/Orders.Api.csproj

echo "---------------------------------------------------------"
echo "🔗 Wiring Inter-Project Dependencies (Clean Architecture)"
echo "---------------------------------------------------------"
# Domain sits at the absolute core (Zero external references)

# Application only depends on Domain
dotnet add src/Orders.Application/Orders.Application.csproj reference src/Orders.Domain/Orders.Domain.csproj

# Infrastructure depends on Application (and transitively Domain)
dotnet add src/Orders.Infrastructure/Orders.Infrastructure.csproj reference src/Orders.Application/Orders.Application.csproj

# Api handles composition and depends on Infrastructure to wire Dependency Injection
dotnet add src/Orders.Api/Orders.Api.csproj reference src/Orders.Infrastructure/Orders.Infrastructure.csproj

echo "---------------------------------------------------------"
echo "📦 Injecting Modern OpenAPI & Swagger UI NuGet Packages"
echo "---------------------------------------------------------"
# In .NET 10, OpenAPI generation is native but Swagger UI requires an explicit package pull
dotnet add src/Orders.Api/Orders.Api.csproj package Microsoft.AspNetCore.OpenApi
dotnet add src/Orders.Api/Orders.Api.csproj package Swashbuckle.AspNetCore.SwaggerUi
dotnet add src/Orders.Api/Orders.Api.csproj package Scalar.AspNetCore

# Add AWS SDK package foundations directly to Infrastructure where data-access code lives
dotnet add src/Orders.Infrastructure/Orders.Infrastructure.csproj package AWSSDK.DynamoDBv2
dotnet add src/Orders.Infrastructure/Orders.Infrastructure.csproj package AWSSDK.SQS

echo "========================================================="
echo "✅ Bootstrap Complete! Open OrdersService.sln to begin."
echo "========================================================="
