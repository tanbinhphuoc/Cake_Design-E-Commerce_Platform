using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class ReviewRepository : GenericRepository<Review>, IReviewRepository
    {
        public ReviewRepository(AppDbContext context) : base(context) { }

        public async Task<List<Review>> GetReviewsByProductIdAsync(Guid productId)
        {
            return await _dbSet
                .Include(r => r.User)
                .Where(r => r.ProductId == productId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<Review?> GetUserReviewForProductAsync(Guid userId, Guid productId)
        {
            return await _dbSet.FirstOrDefaultAsync(r => r.ProductId == productId && r.UserId == userId);
        }
    }
}
