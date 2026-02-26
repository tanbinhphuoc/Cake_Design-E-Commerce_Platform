using Domain.Entities;

namespace Application.Interfaces
{
    public interface IOrderRepository : IGenericRepository<Order>
    {
        Task<Order?> GetByIdWithDetailsAsync(Guid id);
        Task<Order?> GetByIdWithItemsAsync(Guid id);
        Task<List<Order>> GetOrdersByUserIdAsync(Guid userId);
        Task<List<Order>> GetOrdersByShopIdAsync(Guid shopId);
        Task<decimal> GetTotalRevenueAsync();
        Task<decimal> GetTodayRevenueAsync();
        Task<bool> HasUserPurchasedProductAsync(Guid userId, Guid productId);
        Task<List<Order>> GetByVnPayGroupIdAsync(string groupId);
    }
}
