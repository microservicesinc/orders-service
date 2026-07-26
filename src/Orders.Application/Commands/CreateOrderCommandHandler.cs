using System;
using System.Threading.Tasks;
using Orders.Domain.Core.Domain;
using Orders.Domain.Infrastructure.Data;

namespace Orders.Application.Commands
{
    public class CreateOrderCommandHandler
    {
        private readonly IOrderRepository _repository;

        public CreateOrderCommandHandler(IOrderRepository repository)
        {
            _repository = repository;
        }

        public async Task<Order> HandleAsync(CreateOrderCommand command)
        {
            // 1. Generate an identity value conforming to business rules
            string generatedOrderId = $"ORD_{Guid.NewGuid().ToString("N").ToUpper()[..12]}";

            // 2. Instantiate the pure Domain Entity (triggers internal invariant protection validation checks)
            var newOrder = new Order(
                id: generatedOrderId,
                itemId: command.ItemId,
                traceId: string.IsNullOrWhiteSpace(command.TraceId) ? Guid.NewGuid().ToString("N") : command.TraceId
            );

            // 3. Persist the valid domain entity state to infrastructure storage layers
            await _repository.SaveAsync(newOrder);

            return newOrder;
        }
    }
}