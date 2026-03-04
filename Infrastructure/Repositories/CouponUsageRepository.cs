using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class CouponUsageRepository : GenericRepository<CouponUsage>, ICouponUsageRepository
    {
        public CouponUsageRepository(AppDbContext context) : base(context) { }

        public async Task<int> GetUserUsageCountAsync(Guid userId, Guid couponId)
        {
            return await _dbSet.CountAsync(u => u.UserId == userId && u.CouponId == couponId);
        }

        public async Task<List<CouponUsage>> GetByOrderIdAsync(Guid orderId)
        {
            return await _dbSet
                .Include(u => u.Coupon)
                .Where(u => u.OrderId == orderId)
                .ToListAsync();
        }
    }
}
