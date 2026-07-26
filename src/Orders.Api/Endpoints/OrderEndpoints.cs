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
            .WithName("GetAllOrders");
        }
    }
}