using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities
{
    public class ShipperDelivery
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid OrderId { get; set; }

        [Required]
        public Guid ShipperId { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal ShippingFee { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal EarnedAmount { get; set; } // 50% of ShippingFee

        [Column(TypeName = "decimal(18,2)")]
        public decimal SystemCut { get; set; } // 50% of ShippingFee

        [Column(TypeName = "decimal(18,2)")]
        public decimal ShopPayout { get; set; } // Amount paid to shop

        [Column(TypeName = "decimal(18,2)")]
        public decimal Commission { get; set; } // Commission taken from shop

        [MaxLength(30)]
        public string Status { get; set; } = "Completed"; // "Completed", "Disputed"

        public DateTime DeliveredAt { get; set; } = DateTime.UtcNow;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        [ForeignKey(nameof(OrderId))]
        public virtual Order Order { get; set; } = null!;

        [ForeignKey(nameof(ShipperId))]
        public virtual Account Shipper { get; set; } = null!;
    }
}
