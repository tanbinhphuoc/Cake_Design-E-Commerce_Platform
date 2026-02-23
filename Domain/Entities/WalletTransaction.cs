using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities
{
    public class WalletTransaction
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        // Can be AccountId or ShopId
        [Required]
        public Guid WalletOwnerId { get; set; }

        [Required]
        [MaxLength(20)]
        public string WalletType { get; set; } = "User"; // "User", "Shop"

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required]
        [MaxLength(50)]
        public string TransactionType { get; set; } = string.Empty; // "Deposit", "Withdrawal", "Purchase", "Sale", "Refund"

        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal BalanceAfter { get; set; }

        public Guid? ReferenceId { get; set; } // OrderId, etc.

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
