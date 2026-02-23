using Application.DTOs;

namespace Application.Services
{
    public interface ICartService
    {
        Task<object> GetCartAsync(Guid userId);
        Task<string> AddToCartAsync(Guid userId, AddToCartDto dto);
        Task<string> UpdateCartItemAsync(Guid userId, UpdateCartItemDto dto);
        Task<string> RemoveCartItemAsync(Guid userId, Guid productId);
        Task<string> ClearCartAsync(Guid userId);
    }
}
