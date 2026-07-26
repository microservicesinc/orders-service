using System.Threading.Tasks;
using Orders.Domain.Core.Domain;

namespace Orders.Domain.Infrastructure.Data
{
    public interface IOrderRepository
    {
        Task<bool> HasAnyOrdersAsync();
        Task SaveAsync(Order order);
        Task<IEnumerable<Order>> GetAllAsync();
    }
}