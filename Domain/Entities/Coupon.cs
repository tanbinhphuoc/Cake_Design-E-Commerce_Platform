using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities
{
    public class Coupon
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(8)]
        public string Code { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string CouponType { get; set; } = "Shop"; // "Shop" | "System"

        public Guid? ShopId { get; set; } // null nếu là System coupon

        [Required]
        [MaxLength(20)]
        public string DiscountType { get; set; } = "Fixed"; // "Fixed" | "Percentage"

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountValue { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? MaxDiscountAmount { get; set; } // Giới hạn giảm tối đa (cho Percentage)

        [Column(TypeName = "decimal(18,2)")]
        public decimal MinOrderAmount { get; set; } = 0; // Đơn hàng tối thiểu để áp dụng

        public int MaxUsageCount { get; set; } = 1; // Số lần dùng tối đa tổng cộng
        public int UsedCount { get; set; } = 0;
        public int? MaxUsagePerUser { get; set; } = 1; // Mỗi user dùng tối đa

        public bool IsActive { get; set; } = true;
        public DateTime? ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey(nameof(ShopId))]
        public virtual Shop? Shop { get; set; }

        public virtual ICollection<CouponUsage> Usages { get; set; } = new List<CouponUsage>();
    }
}
