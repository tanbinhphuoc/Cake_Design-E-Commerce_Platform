using System.ComponentModel.DataAnnotations;

namespace Domain.Entities
{
    public class Tag
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(50)]
        public string Name { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual ICollection<ProductTag> ProductTags { get; set; } = new List<ProductTag>();
    }
}
