using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities
{
    public class CouponUsage
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid CouponId { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [Required]
        public Guid OrderId { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountApplied { get; set; }

        public DateTime UsedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey(nameof(CouponId))]
        public virtual Coupon Coupon { get; set; } = null!;

        [ForeignKey(nameof(UserId))]
        public virtual Account User { get; set; } = null!;

        [ForeignKey(nameof(OrderId))]
        public virtual Order Order { get; set; } = null!;
    }
}
