using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities
{
    public class Shop
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid OwnerId { get; set; }

        [Required]
        [MaxLength(200)]
        public string ShopName { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string Description { get; set; } = string.Empty;

        [MaxLength(500)]
        public string AvatarUrl { get; set; } = string.Empty;

        [MaxLength(500)]
        public string BannerUrl { get; set; } = string.Empty;

        [MaxLength(500)]
        public string Address { get; set; } = string.Empty;

        public int? ProvinceId { get; set; }
        public int? DistrictId { get; set; }
        public string? WardCode { get; set; }

        [MaxLength(20)]
        public string Phone { get; set; } = string.Empty;

        [Column(TypeName = "decimal(5,2)")]
        public decimal CommissionRate { get; set; } = 5.00m; // % hoa hồng, admin set cho từng shop

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey(nameof(OwnerId))]
        public virtual Account Owner { get; set; } = null!;

        public virtual ICollection<Product> Products { get; set; } = new List<Product>();
        public virtual ICollection<ShopStaff> Staff { get; set; } = new List<ShopStaff>();
        public virtual ICollection<WalletTransaction> WalletTransactions { get; set; } = new List<WalletTransaction>();
        public virtual ICollection<Coupon> Coupons { get; set; } = new List<Coupon>();
    }
}
