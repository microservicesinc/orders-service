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