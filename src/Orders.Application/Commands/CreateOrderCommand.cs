namespace Orders.Application.Commands
{
    // Incoming request DTO for creating an order
    public record CreateOrderCommand(string ItemId, string TraceId);
}