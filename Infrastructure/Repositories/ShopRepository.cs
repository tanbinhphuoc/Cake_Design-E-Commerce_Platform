using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class ShopRepository : GenericRepository<Shop>, IShopRepository
    {
        public ShopRepository(AppDbContext context) : base(context) { }

        public async Task<Shop?> GetByOwnerIdAsync(Guid ownerId)
        {
            return await _dbSet.FirstOrDefaultAsync(s => s.OwnerId == ownerId);
        }

        public async Task<Shop?> GetByIdWithProductsAsync(Guid shopId)
        {
            return await _dbSet
                .Include(s => s.Products)
                .FirstOrDefaultAsync(s => s.Id == shopId);
        }
    }
}
