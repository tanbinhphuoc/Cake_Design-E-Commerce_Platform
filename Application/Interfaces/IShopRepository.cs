using Domain.Entities;

namespace Application.Interfaces
{
    public interface IShopRepository : IGenericRepository<Shop>
    {
        Task<Shop?> GetByOwnerIdAsync(Guid ownerId);
        Task<Shop?> GetByIdWithProductsAsync(Guid shopId);
    }
}
