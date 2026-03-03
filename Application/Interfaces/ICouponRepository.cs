using Domain.Entities;

namespace Application.Interfaces
{
    public interface ICouponRepository : IGenericRepository<Coupon>
    {
        Task<Coupon?> GetByCodeAsync(string code);
        Task<List<Coupon>> GetByShopIdAsync(Guid shopId);
        Task<List<Coupon>> GetSystemCouponsAsync();
    }
}
