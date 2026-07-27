using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using System.Collections.Generic;
using System.Threading.Tasks;
using Orders.Domain.Core.Domain;
using Orders.Domain.Infrastructure.Data;

namespace Orders.Infrastructure.Data
{
    public class DynamoOrderRepository : IOrderRepository
    {
        private readonly IAmazonDynamoDB _dynamoClient;
        private const string TableName = "Orders";

        public DynamoOrderRepository(IAmazonDynamoDB dynamoClient)
        {
            _dynamoClient = dynamoClient;
        }

        public async Task<bool> HasAnyOrdersAsync()
        {
            var request = new ScanRequest { TableName = TableName, Limit = 1 };
            var response = await _dynamoClient.ScanAsync(request);
            return response.Count > 0;
        }

        public async Task SaveAsync(Order order)
        {
            var dbModel = OrderDynamoDbModel.FromDomain(order);
            
            var request = new PutItemRequest
            {
                TableName = TableName,
                Item = new Dictionary<string, AttributeValue>
                {
                    { "PK", new AttributeValue { S = dbModel.PK } },
                    { "SK", new AttributeValue { S = dbModel.SK } },
                    { "id", new AttributeValue { S = dbModel.Id } },
                    { "itemId", new AttributeValue { S = dbModel.ItemId } },
                    { "traceId", new AttributeValue { S = dbModel.TraceId } },
                    { "status", new AttributeValue { S = dbModel.Status } },
                    { "createdAt", new AttributeValue { S = dbModel.CreatedAt } }
                }
            };

            await _dynamoClient.PutItemAsync(request);
        }

        public async Task<IEnumerable<Order>> GetAllAsync()
        {
            var request = new ScanRequest { TableName = TableName };
            var response = await _dynamoClient.ScanAsync(request);
            var orders = new List<Order>();

            foreach (var item in response.Items)
            {
                var order = new Order(
                    id: item.TryGetValue("id", out var idVal) ? idVal.S : string.Empty,
                    itemId: item.TryGetValue("itemId", out var itemVal) ? itemVal.S : string.Empty,
                    traceId: item.TryGetValue("traceId", out var traceVal) ? traceVal.S : string.Empty,
                    status: item.TryGetValue("status", out var statusVal) ? statusVal.S : "PENDING"
                );
                orders.Add(order);
            }

            return orders;
        }

        public async Task<Order?> GetByIdAsync(string id)
        {
            var request = new ScanRequest
            {
                TableName = TableName,
                FilterExpression = "id = :idValue",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    { ":idValue", new AttributeValue { S = id } }
                }
            };

            var response = await _dynamoClient.ScanAsync(request);
            if (response.Items.Count == 0) return null;

            var item = response.Items[0];
            return new Order(
                id: item.TryGetValue("id", out var idVal) ? idVal.S : string.Empty,
                itemId: item.TryGetValue("itemId", out var itemVal) ? itemVal.S : string.Empty,
                traceId: item.TryGetValue("traceId", out var traceVal) ? traceVal.S : string.Empty,
                status: item.TryGetValue("status", out var statusVal) ? statusVal.S : "PENDING"
            );
        }
    }
}