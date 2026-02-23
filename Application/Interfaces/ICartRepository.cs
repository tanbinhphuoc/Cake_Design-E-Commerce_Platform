using Domain.Entities;

namespace Application.Interfaces
{
    public interface ICartRepository : IGenericRepository<Cart>
    {
        Task<Cart?> GetCartByUserIdAsync(Guid userId);
        Task<Cart?> GetCartWithItemsAndProductsAsync(Guid userId);
    }
}
