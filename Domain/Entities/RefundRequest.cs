using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities
{
    public class RefundRequest
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid OrderId { get; set; }

        [Required]
        public Guid CustomerId { get; set; }

        [Required]
        [MaxLength(200)]
        public string Reason { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string Description { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? EvidenceUrls { get; set; } // JSON array of image URLs as evidence

        [Required]
        [MaxLength(30)]
        public string Status { get; set; } = "Pending"; // "Pending", "Approved", "Rejected"

        [MaxLength(2000)]
        public string? StaffNote { get; set; }

        public Guid? ResolvedBy { get; set; } // SystemStaff who resolved

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ResolvedAt { get; set; }

        // Navigation
        [ForeignKey(nameof(OrderId))]
        public virtual Order Order { get; set; } = null!;

        [ForeignKey(nameof(CustomerId))]
        public virtual Account Customer { get; set; } = null!;
    }
}
