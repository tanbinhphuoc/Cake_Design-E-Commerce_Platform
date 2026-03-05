using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities
{
    public class SystemWalletTransaction
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(50)]
        public string WalletType { get; set; } = "Escrow";

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required]
        [MaxLength(50)]
        public string TransactionType { get; set; } = string.Empty;
        // "HoldFromCustomer", "ReleaseToShop", "RefundToCustomer", "VNPayReceived"

        [Column(TypeName = "decimal(18,2)")]
        public decimal BalanceAfter { get; set; }

        public Guid? OrderId { get; set; }
        public Order? Order { get; set; }

        public Guid? RelatedUserId { get; set; } // Customer ho?c Shop Owner

        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
