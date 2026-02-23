using Domain.Entities;

namespace Application.Interfaces
{
    public interface IShopStaffRepository : IGenericRepository<ShopStaff>
    {
        Task<ShopStaff?> GetByShopAndAccountAsync(Guid shopId, Guid accountId);
        Task<ShopStaff?> GetByAccountIdAsync(Guid accountId);
        Task<List<ShopStaff>> GetByShopIdWithAccountAsync(Guid shopId);
    }
}
