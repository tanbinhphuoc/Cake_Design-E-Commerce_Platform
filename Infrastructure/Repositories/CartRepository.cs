using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class CartRepository : GenericRepository<Cart>, ICartRepository
    {
        public CartRepository(AppDbContext context) : base(context) { }

        public async Task<Cart?> GetCartByUserIdAsync(Guid userId)
        {
            return await _dbSet
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.UserId == userId);
        }

        public async Task<Cart?> GetCartWithItemsAndProductsAsync(Guid userId)
        {
            return await _dbSet
                .Include(c => c.Items)
                    .ThenInclude(ci => ci.Product)
                        .ThenInclude(p => p.Shop)
                .FirstOrDefaultAsync(c => c.UserId == userId);
        }
    }
}
