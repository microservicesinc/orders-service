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