using Amazon.SQS;
using Amazon.SQS.Model;
using System;
using System.Text.Json;
using System.Threading.Tasks;
using Orders.Domain.Core.Domain;
using Orders.Domain.Infrastructure.Data;
namespace Orders.Application.Commands
{
    public class CreateOrderCommandHandler
    {
        private readonly IOrderRepository _repository;
        private readonly IAmazonSQS _sqsClient;
        private const string InventoryQueueUrl = "http://localhost:4566/000000000000/StockUpdateQueue";

        public CreateOrderCommandHandler(IOrderRepository repository, IAmazonSQS sqsClient)
        {
            _repository = repository;
            _sqsClient = sqsClient;
        }

        public async Task<Order> HandleAsync(CreateOrderCommand command)
        {
            string generatedOrderId = $"ORD_{Guid.NewGuid().ToString("N").ToUpper()[..12]}";

            var newOrder = new Order(
                id: generatedOrderId,
                itemId: command.ItemId,
                traceId: string.IsNullOrWhiteSpace(command.TraceId) ? Guid.NewGuid().ToString("N") : command.TraceId,
                status: "PENDING"
            );

            await _repository.SaveAsync(newOrder);

            var messagePayload = new
            {
                itemId = newOrder.ItemId,
                quantityChange = -1
            };

            await _sqsClient.SendMessageAsync(new SendMessageRequest
            {
                QueueUrl = InventoryQueueUrl,
                MessageBody = JsonSerializer.Serialize(messagePayload)
            });

            return newOrder;
        }
    }
}