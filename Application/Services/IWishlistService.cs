using Application.DTOs;

namespace Application.Services
{
    public interface IWishlistService
    {
        Task<List<WishlistItemDto>> GetWishlistAsync(Guid userId);
        Task<string> AddToWishlistAsync(Guid userId, Guid productId);
        Task<string> RemoveFromWishlistAsync(Guid userId, Guid productId);
    }
}
