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