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