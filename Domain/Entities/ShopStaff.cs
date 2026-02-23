using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities
{
    public class ShopStaff
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid ShopId { get; set; }

        [Required]
        public Guid AccountId { get; set; }

        [MaxLength(50)]
        public string Role { get; set; } = "Staff"; // "Staff", "Manager"

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey(nameof(ShopId))]
        public virtual Shop Shop { get; set; } = null!;

        [ForeignKey(nameof(AccountId))]
        public virtual Account Account { get; set; } = null!;
    }
}
