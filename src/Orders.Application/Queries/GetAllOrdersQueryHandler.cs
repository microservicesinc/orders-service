using System.Collections.Generic;
using System.Threading.Tasks;
using Orders.Domain.Core.Domain;
using Orders.Domain.Infrastructure.Data;

namespace Orders.Application.Queries
{
    public class GetAllOrdersQueryHandler
    {
        private readonly IOrderRepository _repository;

        public GetAllOrdersQueryHandler(IOrderRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<Order>> HandleAsync(GetAllOrdersQuery query)
        {
            return await _repository.GetAllAsync();
        }
    }
}