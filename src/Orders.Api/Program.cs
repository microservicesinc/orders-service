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
    // This extension method automatically registers IAmazonDynamoDB AND maps 
    // IOrderRepository to DynamoOrderRepository
    builder.Services.AddLocalDynamoDb(builder.Configuration);

    // This extension method automatically registers IAmazonSQS AND maps IOrderRepository 
    // to DynamoOrderRepository
    builder.Services.AddLocalSqs(builder.Configuration);
    
    // Register the Application Layer query handler abstraction
    builder.Services.AddScoped<GetAllOrdersQueryHandler>();
    builder.Services.AddScoped<GetOrderByIdQueryHandler>();
    builder.Services.AddScoped<CreateOrderCommandHandler>();
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
    //await OrderSeeder.SeedAsync(orderRepository, logger);
}

app.UseHttpsRedirection();
app.MapOrderEndpoints();
app.Run();