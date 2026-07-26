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