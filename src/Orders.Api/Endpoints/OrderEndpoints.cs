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

            routes.MapPost("api/orders", async (CreateOrderCommand command, CreateOrderCommandHandler commandHandler) =>
            {
                try
                {
                    var createdOrder = await commandHandler.HandleAsync(command);
                    
                    // Return 201 Created status pointing back to resource tracking endpoints
                    return Results.Created($"api/orders/{createdOrder.Id}", createdOrder);
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