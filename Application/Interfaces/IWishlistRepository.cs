using Domain.Entities;

namespace Application.Interfaces
{
    public interface IWishlistRepository : IGenericRepository<WishlistItem>
    {
        Task<List<WishlistItem>> GetByUserIdAsync(Guid userId);
        Task<WishlistItem?> GetByUserAndProductAsync(Guid userId, Guid productId);
    }
}
