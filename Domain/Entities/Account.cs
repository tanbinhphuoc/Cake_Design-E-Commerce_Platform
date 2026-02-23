using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities
{
    public class Account
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(100)]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        [MaxLength(100)]
        public string Email { get; set; } = string.Empty;

        [MaxLength(20)]
        public string Phone { get; set; } = string.Empty;

        [MaxLength(500)]
        public string AvatarUrl { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal WalletBalance { get; set; } = 0;

        [Required]
        [MaxLength(20)]
        public string Role { get; set; } = "Customer"; // "Customer", "ShopOwner", "Admin", "Staff"

        public bool IsApproved { get; set; } = false;

        public Guid? DefaultAddressId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual Shop? Shop { get; set; }
        public virtual Cart? Cart { get; set; }
        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
        public virtual ICollection<Address> Addresses { get; set; } = new List<Address>();
        public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
        public virtual ICollection<WishlistItem> WishlistItems { get; set; } = new List<WishlistItem>();
        public virtual ICollection<Report> Reports { get; set; } = new List<Report>();
    }
}
