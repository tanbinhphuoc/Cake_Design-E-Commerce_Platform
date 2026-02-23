using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class ShopStaffRepository : GenericRepository<ShopStaff>, IShopStaffRepository
    {
        public ShopStaffRepository(AppDbContext context) : base(context) { }

        public async Task<ShopStaff?> GetByShopAndAccountAsync(Guid shopId, Guid accountId)
        {
            return await _dbSet.FirstOrDefaultAsync(ss => ss.ShopId == shopId && ss.AccountId == accountId);
        }

        public async Task<ShopStaff?> GetByAccountIdAsync(Guid accountId)
        {
            return await _dbSet.FirstOrDefaultAsync(ss => ss.AccountId == accountId);
        }

        public async Task<List<ShopStaff>> GetByShopIdWithAccountAsync(Guid shopId)
        {
            return await _dbSet
                .Include(ss => ss.Account)
                .Where(ss => ss.ShopId == shopId)
                .ToListAsync();
        }
    }
}
