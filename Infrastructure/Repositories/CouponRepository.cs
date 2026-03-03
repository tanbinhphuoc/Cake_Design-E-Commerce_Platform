using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class CouponRepository : GenericRepository<Coupon>, ICouponRepository
    {
        public CouponRepository(AppDbContext context) : base(context) { }

        public async Task<Coupon?> GetByCodeAsync(string code)
        {
            return await _dbSet
                .Include(c => c.Shop)
                .FirstOrDefaultAsync(c => c.Code == code);
        }

        public async Task<List<Coupon>> GetByShopIdAsync(Guid shopId)
        {
            return await _dbSet
                .Where(c => c.CouponType == "Shop" && c.ShopId == shopId)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Coupon>> GetSystemCouponsAsync()
        {
            return await _dbSet
                .Where(c => c.CouponType == "System")
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }
    }
}
