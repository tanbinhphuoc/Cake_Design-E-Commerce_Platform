using Domain.Entities;

namespace Application.Interfaces
{
    public interface IReviewRepository : IGenericRepository<Review>
    {
        Task<List<Review>> GetReviewsByProductIdAsync(Guid productId);
        Task<Review?> GetUserReviewForProductAsync(Guid userId, Guid productId);
    }
}
