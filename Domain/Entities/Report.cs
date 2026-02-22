using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities
{
    public class Report
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid ReporterId { get; set; }

        [Required]
        [MaxLength(20)]
        public string TargetType { get; set; } = string.Empty; // "Product", "Shop"

        [Required]
        public Guid TargetId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Reason { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "Pending"; // "Pending", "Reviewed", "Resolved", "Dismissed"

        [MaxLength(2000)]
        public string? AdminNote { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ResolvedAt { get; set; }

        // Navigation properties
        [ForeignKey(nameof(ReporterId))]
        public virtual Account Reporter { get; set; } = null!;
    }
}
