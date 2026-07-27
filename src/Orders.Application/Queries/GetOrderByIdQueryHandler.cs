using System.Threading.Tasks;
using Orders.Domain.Core.Domain;
using Orders.Domain.Infrastructure.Data;

namespace Orders.Application.Queries
{
    public class GetOrderByIdQueryHandler
    {
        private readonly IOrderRepository _repository;

        public GetOrderByIdQueryHandler(IOrderRepository repository)
        {
            _repository = repository;
        }

        public async Task<Order?> HandleAsync(GetOrderByIdQuery query)
        {
            return await _repository.GetByIdAsync(query.Id);
        }
    }
}