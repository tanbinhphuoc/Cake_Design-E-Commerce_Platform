using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities
{
    public class Cart
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid UserId { get; set; }

        // Navigation properties
        [ForeignKey(nameof(UserId))]
        public virtual Account User { get; set; } = null!;

        public virtual ICollection<CartItem> Items { get; set; } = new List<CartItem>();
    }
}
