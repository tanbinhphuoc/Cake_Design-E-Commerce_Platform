using Application.DTOs;

namespace Application.Services
{
    public interface ICouponService
    {
        // Shop Owner
        Task<object> CreateShopCouponAsync(Guid ownerId, CreateCouponDto dto);
        Task<List<CouponDto>> GetShopCouponsAsync(Guid ownerId);
        Task<string> UpdateCouponAsync(Guid ownerId, Guid couponId, UpdateCouponDto dto);
        Task<string> DeactivateCouponAsync(Guid ownerId, Guid couponId);

        // Admin
        Task<object> CreateSystemCouponAsync(CreateCouponDto dto);
        Task<List<CouponDto>> GetSystemCouponsAsync();

        // Customer
        Task<CouponValidationResult> ValidateCouponAsync(string code, Guid userId, decimal orderAmount, Guid? shopId);

        // Internal (used by OrderService)
        Task<decimal> CalculateDiscountAsync(string code, decimal amount);
        Task RecordUsageAsync(Guid couponId, Guid userId, Guid orderId, decimal discountApplied);
        Task ReverseUsageAsync(Guid orderId);
        Task<object> CreateCompensationCouponAsync(Guid userId, decimal orderAmount);
    }

    public class CouponValidationResult
    {
        public bool IsValid { get; set; }
        public string? ErrorMessage { get; set; }
        public Guid? CouponId { get; set; }
        public string CouponType { get; set; } = string.Empty;
        public decimal DiscountAmount { get; set; }
    }
}
