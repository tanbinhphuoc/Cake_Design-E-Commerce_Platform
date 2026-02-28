using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities
{
    public class Order
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid UserId { get; set; }

        [Required]
        public Guid ShopId { get; set; }

        public Guid? ShippingAddressId { get; set; }

        public Guid? ShipperId { get; set; } // Shipper assigned to this order

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal ShippingFee { get; set; } = 0;

        [MaxLength(200)]
        public string? ShippingProvider { get; set; }

        [Required]
        [MaxLength(30)]
        public string Status { get; set; } = "Pending"; 
        // "Pending", "Confirmed", "ReadyForPickup", "Shipping", "Delivered", "Completed", "Cancelled", "Returned"

        [MaxLength(500)]
        public string? Note { get; set; }

        [MaxLength(50)]
        public string PaymentMethod { get; set; } = "Wallet"; // "Wallet", "VNPay", "MoMo"

        [MaxLength(20)]
        public string PaymentStatus { get; set; } = "Pending"; // "Pending", "Paid", "Refunded", "Failed"

        [MaxLength(50)]
        public string? VnPayGroupId { get; set; } // Links multiple orders from the same VNPay checkout

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey(nameof(UserId))]
        public virtual Account User { get; set; } = null!;

        [ForeignKey(nameof(ShopId))]
        public virtual Shop Shop { get; set; } = null!;

        [ForeignKey(nameof(ShippingAddressId))]
        public virtual Address? ShippingAddress { get; set; }

        [ForeignKey(nameof(ShipperId))]
        public virtual Account? Shipper { get; set; }

        public virtual ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
        public virtual Payment? Payment { get; set; }
        public virtual RefundRequest? RefundRequest { get; set; }
    }
}
