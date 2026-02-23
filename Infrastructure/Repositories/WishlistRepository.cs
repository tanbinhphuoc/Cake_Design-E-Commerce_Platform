using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class WishlistRepository : GenericRepository<WishlistItem>, IWishlistRepository
    {
        public WishlistRepository(AppDbContext context) : base(context) { }

        public async Task<List<WishlistItem>> GetByUserIdAsync(Guid userId)
        {
            return await _dbSet
                .Include(w => w.Product)
                .Where(w => w.UserId == userId)
                .OrderByDescending(w => w.CreatedAt)
                .ToListAsync();
        }

        public async Task<WishlistItem?> GetByUserAndProductAsync(Guid userId, Guid productId)
        {
            return await _dbSet.FirstOrDefaultAsync(w => w.UserId == userId && w.ProductId == productId);
        }
    }
}
