using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Account> Accounts { get; set; } = null!;
        public DbSet<Shop> Shops { get; set; } = null!;
        public DbSet<ShopStaff> ShopStaff { get; set; } = null!;
        public DbSet<Product> Products { get; set; } = null!;
        public DbSet<Category> Categories { get; set; } = null!;
        public DbSet<Tag> Tags { get; set; } = null!;
        public DbSet<ProductTag> ProductTags { get; set; } = null!;
        public DbSet<Cart> Carts { get; set; } = null!;
        public DbSet<CartItem> CartItems { get; set; } = null!;
        public DbSet<Order> Orders { get; set; } = null!;
        public DbSet<OrderItem> OrderItems { get; set; } = null!;
        public DbSet<Address> Addresses { get; set; } = null!;
        public DbSet<Review> Reviews { get; set; } = null!;
        public DbSet<WishlistItem> WishlistItems { get; set; } = null!;
        public DbSet<WalletTransaction> WalletTransactions { get; set; } = null!;
        public DbSet<Payment> Payments { get; set; } = null!;
        public DbSet<Report> Reports { get; set; } = null!;
        
        // System Wallet
        public DbSet<SystemWallet> SystemWallets { get; set; } = null!;
        public DbSet<SystemWalletTransaction> SystemWalletTransactions { get; set; } = null!;

        // Refund
        public DbSet<RefundRequest> RefundRequests { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ===== Account =====
            modelBuilder.Entity<Account>(entity =>
            {
                entity.HasIndex(a => a.Username).IsUnique();

                entity.HasOne(a => a.Cart)
                      .WithOne(c => c.User)
                      .HasForeignKey<Cart>(c => c.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(a => a.Shop)
                      .WithOne(s => s.Owner)
                      .HasForeignKey<Shop>(s => s.OwnerId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(a => a.Orders)
                      .WithOne(o => o.User)
                      .HasForeignKey(o => o.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(a => a.Addresses)
                      .WithOne(addr => addr.User)
                      .HasForeignKey(addr => addr.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(a => a.Reviews)
                      .WithOne(r => r.User)
                      .HasForeignKey(r => r.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(a => a.WishlistItems)
                      .WithOne(w => w.User)
                      .HasForeignKey(w => w.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(a => a.Reports)
                      .WithOne(r => r.Reporter)
                      .HasForeignKey(r => r.ReporterId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // ===== Shop =====
            modelBuilder.Entity<Shop>(entity =>
            {
                entity.HasMany(s => s.Products)
                      .WithOne(p => p.Shop)
                      .HasForeignKey(p => p.ShopId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(s => s.Staff)
                      .WithOne(st => st.Shop)
                      .HasForeignKey(st => st.ShopId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // ===== Cart =====
            modelBuilder.Entity<Cart>(entity =>
            {
                entity.HasMany(c => c.Items)
                      .WithOne(ci => ci.Cart)
                      .HasForeignKey(ci => ci.CartId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // ===== CartItem =====
            modelBuilder.Entity<CartItem>(entity =>
            {
                entity.HasOne(ci => ci.Product)
                      .WithMany(p => p.CartItems)
                      .HasForeignKey(ci => ci.ProductId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // ===== Product =====
            modelBuilder.Entity<Product>(entity =>
            {
                entity.HasOne(p => p.Category)
                      .WithMany(c => c.Products)
                      .HasForeignKey(p => p.CategoryId)
                      .OnDelete(DeleteBehavior.SetNull);

                entity.HasMany(p => p.ProductTags)
                      .WithOne(pt => pt.Product)
                      .HasForeignKey(pt => pt.ProductId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(p => p.Reviews)
                      .WithOne(r => r.Product)
                      .HasForeignKey(r => r.ProductId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(p => p.WishlistItems)
                      .WithOne(w => w.Product)
                      .HasForeignKey(w => w.ProductId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // ===== ProductTag =====
            modelBuilder.Entity<ProductTag>(entity =>
            {
                entity.HasIndex(pt => new { pt.ProductId, pt.TagId }).IsUnique();

                entity.HasOne(pt => pt.Tag)
                      .WithMany(t => t.ProductTags)
                      .HasForeignKey(pt => pt.TagId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // ===== Category =====
            modelBuilder.Entity<Category>(entity =>
            {
                entity.HasIndex(c => c.Name).IsUnique();
            });

            // ===== Tag =====
            modelBuilder.Entity<Tag>(entity =>
            {
                entity.HasIndex(t => t.Name).IsUnique();
            });

            // ===== Order =====
            modelBuilder.Entity<Order>(entity =>
            {
                entity.HasMany(o => o.Items)
                      .WithOne(oi => oi.Order)
                      .HasForeignKey(oi => oi.OrderId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(o => o.Shop)
                      .WithMany()
                      .HasForeignKey(o => o.ShopId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(o => o.ShippingAddress)
                      .WithMany()
                      .HasForeignKey(o => o.ShippingAddressId)
                      .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(o => o.Payment)
                      .WithOne(p => p.Order)
                      .HasForeignKey<Payment>(p => p.OrderId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(o => o.Shipper)
                      .WithMany()
                      .HasForeignKey(o => o.ShipperId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            // ===== OrderItem =====
            modelBuilder.Entity<OrderItem>(entity =>
            {
                entity.HasOne(oi => oi.Product)
                      .WithMany(p => p.OrderItems)
                      .HasForeignKey(oi => oi.ProductId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // ===== WishlistItem =====
            modelBuilder.Entity<WishlistItem>(entity =>
            {
                entity.HasIndex(w => new { w.UserId, w.ProductId }).IsUnique();
            });

            // ===== Review =====
            modelBuilder.Entity<Review>(entity =>
            {
                entity.HasIndex(r => new { r.ProductId, r.UserId }).IsUnique();
            });

            // ===== ShopStaff =====
            modelBuilder.Entity<ShopStaff>(entity =>
            {
                entity.HasIndex(ss => new { ss.ShopId, ss.AccountId }).IsUnique();
            });

            // ===== SystemWallet =====
            modelBuilder.Entity<SystemWallet>(entity =>
            {
                entity.HasIndex(w => w.WalletType).IsUnique();
            });

            // ===== SystemWalletTransaction =====
            modelBuilder.Entity<SystemWalletTransaction>(entity =>
            {
                entity.HasOne(t => t.Order)
                      .WithMany()
                      .HasForeignKey(t => t.OrderId)
                      .OnDelete(DeleteBehavior.SetNull);

                entity.HasIndex(t => t.WalletType);
                entity.HasIndex(t => t.CreatedAt);
            });

            // ===== RefundRequest =====
            modelBuilder.Entity<RefundRequest>(entity =>
            {
                entity.HasOne(r => r.Order)
                      .WithOne(o => o.RefundRequest)
                      .HasForeignKey<RefundRequest>(r => r.OrderId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(r => r.Customer)
                      .WithMany()
                      .HasForeignKey(r => r.CustomerId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(r => r.Status);
            });

            // Apply any configuration classes from assembly
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        }
    }
}
