using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;

namespace Application.Services
{
    public class CouponService : ICouponService
    {
        private readonly IUnitOfWork _uow;
        public CouponService(IUnitOfWork uow) { _uow = uow; }

        // ===== Shop Owner =====

        public async Task<object> CreateShopCouponAsync(Guid ownerId, CreateCouponDto dto)
        {
            var shop = await _uow.Shops.GetByOwnerIdAsync(ownerId);
            if (shop == null) throw new ArgumentException("Shop not found.");

            ValidateCouponDto(dto);
            await EnsureCodeUnique(dto.Code);

            var coupon = new Coupon
            {
                Id = Guid.NewGuid(),
                Code = dto.Code.ToUpper(),
                CouponType = "Shop",
                ShopId = shop.Id,
                DiscountType = dto.DiscountType,
                DiscountValue = dto.DiscountValue,
                MaxDiscountAmount = dto.MaxDiscountAmount,
                MinOrderAmount = dto.MinOrderAmount,
                MaxUsageCount = dto.MaxUsageCount,
                MaxUsagePerUser = dto.MaxUsagePerUser,
                IsActive = true,
                ExpiresAt = dto.ExpiresAt,
                CreatedAt = DateTime.UtcNow
            };

            await _uow.Coupons.AddAsync(coupon);
            await _uow.SaveChangesAsync();
            return new { Message = "Shop coupon created.", CouponId = coupon.Id, coupon.Code };
        }

        public async Task<List<CouponDto>> GetShopCouponsAsync(Guid ownerId)
        {
            var shop = await _uow.Shops.GetByOwnerIdAsync(ownerId);
            if (shop == null) throw new ArgumentException("Shop not found.");
            var coupons = await _uow.Coupons.GetByShopIdAsync(shop.Id);
            return coupons.Select(MapCoupon).ToList();
        }

        public async Task<string> UpdateCouponAsync(Guid ownerId, Guid couponId, UpdateCouponDto dto)
        {
            var shop = await _uow.Shops.GetByOwnerIdAsync(ownerId);
            if (shop == null) throw new ArgumentException("Shop not found.");
            var coupon = await _uow.Coupons.GetByIdAsync(couponId);
            if (coupon == null || coupon.ShopId != shop.Id) throw new ArgumentException("Coupon not found.");

            if (dto.DiscountValue.HasValue) coupon.DiscountValue = dto.DiscountValue.Value;
            if (dto.MaxDiscountAmount.HasValue) coupon.MaxDiscountAmount = dto.MaxDiscountAmount;
            if (dto.MinOrderAmount.HasValue) coupon.MinOrderAmount = dto.MinOrderAmount.Value;
            if (dto.MaxUsageCount.HasValue) coupon.MaxUsageCount = dto.MaxUsageCount.Value;
            if (dto.MaxUsagePerUser.HasValue) coupon.MaxUsagePerUser = dto.MaxUsagePerUser;
            if (dto.IsActive.HasValue) coupon.IsActive = dto.IsActive.Value;
            if (dto.ExpiresAt.HasValue) coupon.ExpiresAt = dto.ExpiresAt;

            await _uow.SaveChangesAsync();
            return "Coupon updated.";
        }

        public async Task<string> DeactivateCouponAsync(Guid ownerId, Guid couponId)
        {
            var shop = await _uow.Shops.GetByOwnerIdAsync(ownerId);
            if (shop == null) throw new ArgumentException("Shop not found.");
            var coupon = await _uow.Coupons.GetByIdAsync(couponId);
            if (coupon == null || coupon.ShopId != shop.Id) throw new ArgumentException("Coupon not found.");
            coupon.IsActive = false;
            await _uow.SaveChangesAsync();
            return "Coupon deactivated.";
        }

        // ===== Admin =====

        public async Task<object> CreateSystemCouponAsync(CreateCouponDto dto)
        {
            ValidateCouponDto(dto);
            await EnsureCodeUnique(dto.Code);

            var coupon = new Coupon
            {
                Id = Guid.NewGuid(),
                Code = dto.Code.ToUpper(),
                CouponType = "System",
                ShopId = null,
                DiscountType = dto.DiscountType,
                DiscountValue = dto.DiscountValue,
                MaxDiscountAmount = dto.MaxDiscountAmount,
                MinOrderAmount = dto.MinOrderAmount,
                MaxUsageCount = dto.MaxUsageCount,
                MaxUsagePerUser = dto.MaxUsagePerUser,
                IsActive = true,
                ExpiresAt = dto.ExpiresAt,
                CreatedAt = DateTime.UtcNow
            };

            await _uow.Coupons.AddAsync(coupon);
            await _uow.SaveChangesAsync();
            return new { Message = "System coupon created.", CouponId = coupon.Id, coupon.Code };
        }

        public async Task<List<CouponDto>> GetSystemCouponsAsync()
        {
            var coupons = await _uow.Coupons.GetSystemCouponsAsync();
            return coupons.Select(MapCoupon).ToList();
        }

        // ===== Customer Validation =====

        public async Task<CouponValidationResult> ValidateCouponAsync(string code, Guid userId, decimal orderAmount, Guid? shopId)
        {
            var coupon = await _uow.Coupons.GetByCodeAsync(code.ToUpper());
            if (coupon == null)
                return new CouponValidationResult { IsValid = false, ErrorMessage = "Mã giảm giá không tồn tại." };

            if (!coupon.IsActive)
                return new CouponValidationResult { IsValid = false, ErrorMessage = "Mã giảm giá đã hết hiệu lực." };

            if (coupon.ExpiresAt.HasValue && coupon.ExpiresAt.Value < DateTime.UtcNow)
                return new CouponValidationResult { IsValid = false, ErrorMessage = "Mã giảm giá đã hết hạn." };

            if (coupon.UsedCount >= coupon.MaxUsageCount)
                return new CouponValidationResult { IsValid = false, ErrorMessage = "Mã giảm giá đã hết lượt sử dụng." };

            if (coupon.MaxUsagePerUser.HasValue)
            {
                var userUsage = await _uow.CouponUsages.GetUserUsageCountAsync(userId, coupon.Id);
                if (userUsage >= coupon.MaxUsagePerUser.Value)
                    return new CouponValidationResult { IsValid = false, ErrorMessage = "Bạn đã sử dụng hết số lần cho mã này." };
            }

            if (orderAmount < coupon.MinOrderAmount)
                return new CouponValidationResult { IsValid = false, ErrorMessage = $"Đơn hàng tối thiểu {coupon.MinOrderAmount:N0} VND để dùng mã này." };

            // Shop coupon: must match shop
            if (coupon.CouponType == "Shop" && shopId.HasValue && coupon.ShopId != shopId.Value)
                return new CouponValidationResult { IsValid = false, ErrorMessage = "Mã giảm giá không áp dụng cho shop này." };

            var discount = CalculateDiscount(coupon, orderAmount);

            return new CouponValidationResult
            {
                IsValid = true,
                CouponId = coupon.Id,
                CouponType = coupon.CouponType,
                DiscountAmount = discount
            };
        }

        // ===== Internal Methods =====

        public async Task<decimal> CalculateDiscountAsync(string code, decimal amount)
        {
            var coupon = await _uow.Coupons.GetByCodeAsync(code.ToUpper());
            if (coupon == null) return 0;
            return CalculateDiscount(coupon, amount);
        }

        public async Task RecordUsageAsync(Guid couponId, Guid userId, Guid orderId, decimal discountApplied)
        {
            var coupon = await _uow.Coupons.GetByIdAsync(couponId);
            if (coupon == null) return;

            coupon.UsedCount++;
            await _uow.CouponUsages.AddAsync(new CouponUsage
            {
                Id = Guid.NewGuid(),
                CouponId = couponId,
                UserId = userId,
                OrderId = orderId,
                DiscountApplied = discountApplied,
                UsedAt = DateTime.UtcNow
            });
        }

        public async Task ReverseUsageAsync(Guid orderId)
        {
            var usages = await _uow.CouponUsages.GetByOrderIdAsync(orderId);
            foreach (var usage in usages)
            {
                var coupon = await _uow.Coupons.GetByIdAsync(usage.CouponId);
                if (coupon != null && coupon.UsedCount > 0)
                    coupon.UsedCount--;
                _uow.CouponUsages.Remove(usage);
            }
        }

        /// <summary>
        /// Create a compensation system coupon when refund is approved (due to shipping issues).
        /// Value = 10% of order amount, max 50,000 VND
        /// </summary>
        public async Task<object> CreateCompensationCouponAsync(Guid userId, decimal orderAmount)
        {
            var compensationValue = Math.Min(orderAmount * 0.10m, 50000m);
            var code = GenerateCode();

            var coupon = new Coupon
            {
                Id = Guid.NewGuid(),
                Code = code,
                CouponType = "System",
                ShopId = null,
                DiscountType = "Fixed",
                DiscountValue = compensationValue,
                MinOrderAmount = 0,
                MaxUsageCount = 1,
                MaxUsagePerUser = 1,
                IsActive = true,
                ExpiresAt = DateTime.UtcNow.AddDays(30), // Valid for 30 days
                CreatedAt = DateTime.UtcNow
            };

            await _uow.Coupons.AddAsync(coupon);
            return new { CouponCode = coupon.Code, DiscountValue = compensationValue, ExpiresAt = coupon.ExpiresAt };
        }

        // ===== Helpers =====

        private static decimal CalculateDiscount(Coupon coupon, decimal amount)
        {
            if (coupon.DiscountType == "Percentage")
            {
                var discount = amount * coupon.DiscountValue / 100m;
                if (coupon.MaxDiscountAmount.HasValue)
                    discount = Math.Min(discount, coupon.MaxDiscountAmount.Value);
                return Math.Round(discount, 0); // Round to VND
            }
            else // Fixed
            {
                return Math.Min(coupon.DiscountValue, amount);
            }
        }

        private async Task EnsureCodeUnique(string code)
        {
            var existing = await _uow.Coupons.GetByCodeAsync(code.ToUpper());
            if (existing != null)
                throw new InvalidOperationException($"Mã '{code}' đã tồn tại.");
        }

        private static void ValidateCouponDto(CreateCouponDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Code) || dto.Code.Length > 8)
                throw new ArgumentException("Mã giảm giá phải từ 1-8 ký tự.");
            if (dto.DiscountValue <= 0)
                throw new ArgumentException("Giá trị giảm phải > 0.");
            if (dto.DiscountType != "Fixed" && dto.DiscountType != "Percentage")
                throw new ArgumentException("DiscountType phải là 'Fixed' hoặc 'Percentage'.");
            if (dto.DiscountType == "Percentage" && dto.DiscountValue > 100)
                throw new ArgumentException("Phần trăm giảm không được vượt 100%.");
        }

        private static string GenerateCode()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            return new string(Enumerable.Range(0, 8).Select(_ => chars[random.Next(chars.Length)]).ToArray());
        }

        private static CouponDto MapCoupon(Coupon c) => new()
        {
            Id = c.Id, Code = c.Code, CouponType = c.CouponType,
            ShopId = c.ShopId, DiscountType = c.DiscountType,
            DiscountValue = c.DiscountValue, MaxDiscountAmount = c.MaxDiscountAmount,
            MinOrderAmount = c.MinOrderAmount, MaxUsageCount = c.MaxUsageCount,
            UsedCount = c.UsedCount, MaxUsagePerUser = c.MaxUsagePerUser,
            IsActive = c.IsActive, ExpiresAt = c.ExpiresAt, CreatedAt = c.CreatedAt
        };
    }
}
