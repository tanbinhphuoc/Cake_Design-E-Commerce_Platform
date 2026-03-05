using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class OrderRepository : GenericRepository<Order>, IOrderRepository
    {
        public OrderRepository(AppDbContext context) : base(context) { }

        public async Task<Order?> GetByIdWithDetailsAsync(Guid id)
        {
            return await _dbSet
                .Include(o => o.Items).ThenInclude(oi => oi.Product)
                .Include(o => o.Shop)
                .Include(o => o.User)
                .Include(o => o.ShippingAddress)
                .FirstOrDefaultAsync(o => o.Id == id);
        }

        public async Task<Order?> GetByIdWithItemsAsync(Guid id)
        {
            return await _dbSet
                .Include(o => o.Items).ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == id);
        }

        public async Task<List<Order>> GetOrdersByUserIdAsync(Guid userId)
        {
            return await _dbSet
                .Include(o => o.Items).ThenInclude(oi => oi.Product)
                .Include(o => o.Shop)
                .Include(o => o.ShippingAddress)
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Order>> GetOrdersByShopIdAsync(Guid shopId)
        {
            return await _dbSet
                .Include(o => o.Items).ThenInclude(oi => oi.Product)
                .Include(o => o.User)
                .Include(o => o.ShippingAddress)
                .Where(o => o.ShopId == shopId)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();
        }

        public async Task<decimal> GetTotalRevenueAsync()
        {
            return await _dbSet
                .Where(o => o.Status == "Completed")
                .SumAsync(o => o.TotalAmount);
        }

        public async Task<decimal> GetTodayRevenueAsync()
        {
            var today = DateTime.UtcNow.Date;
            return await _dbSet
                .Where(o => o.Status == "Completed" && o.CreatedAt >= today)
                .SumAsync(o => o.TotalAmount);
        }

        public async Task<bool> HasUserPurchasedProductAsync(Guid userId, Guid productId)
        {
            return await _dbSet
                .Where(o => o.UserId == userId && o.Status == "Completed")
                .AnyAsync(o => o.Items.Any(oi => oi.ProductId == productId));
        }

        public async Task<List<Order>> GetByVnPayGroupIdAsync(string groupId)
        {
            return await _dbSet
                .Include(o => o.Items).ThenInclude(oi => oi.Product)
                .Where(o => o.VnPayGroupId == groupId)
                .ToListAsync();
        }
    }
}
