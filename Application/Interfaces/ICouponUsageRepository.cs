using Domain.Entities;

namespace Application.Interfaces
{
    public interface ICouponUsageRepository : IGenericRepository<CouponUsage>
    {
        Task<int> GetUserUsageCountAsync(Guid userId, Guid couponId);
        Task<List<CouponUsage>> GetByOrderIdAsync(Guid orderId);
    }
}
