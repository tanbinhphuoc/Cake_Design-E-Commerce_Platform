using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure
{
    public static class DataSeeder
    {
        public static async Task SeedAllAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

            if (await db.Accounts.AnyAsync(a => a.Role == "Customer"))
            {
                Console.WriteLine("[Seed] Mock data already exists. Skipping.");
                return;
            }

            Console.WriteLine("[Seed] Seeding full mock data...");

            var seedPassword = configuration["SeedData:DefaultPassword"] ?? "123456";
            var pw = BCrypt.Net.BCrypt.HashPassword(seedPassword);
            var now = DateTime.UtcNow;

            // ========== ACCOUNTS ==========
            var admin1 = new Account { Id = Guid.Parse("a0000000-0000-0000-0000-000000000001"), Username = "admin1", PasswordHash = pw, FullName = "Administrator 1", Email = "admin1@cakedesign.com", Phone = "0901000001", Role = "Admin", IsApproved = true, WalletBalance = 0, CreatedAt = now, UpdatedAt = now };
            var admin2 = new Account { Id = Guid.Parse("a0000000-0000-0000-0000-000000000002"), Username = "admin2", PasswordHash = pw, FullName = "Administrator 2", Email = "admin2@cakedesign.com", Phone = "0901000002", Role = "Admin", IsApproved = true, WalletBalance = 0, CreatedAt = now, UpdatedAt = now };

            var shopOwner1 = new Account { Id = Guid.Parse("b0000000-0000-0000-0000-000000000001"), Username = "shopowner1", PasswordHash = pw, FullName = "Nguyễn Văn Tiệm", Email = "shop1@cakedesign.com", Phone = "0902000001", Role = "ShopOwner", IsApproved = true, WalletBalance = 5000000m, CreatedAt = now, UpdatedAt = now };
            var shopOwner2 = new Account { Id = Guid.Parse("b0000000-0000-0000-0000-000000000002"), Username = "shopowner2", PasswordHash = pw, FullName = "Trần Thị Bánh", Email = "shop2@cakedesign.com", Phone = "0902000002", Role = "ShopOwner", IsApproved = true, WalletBalance = 3000000m, CreatedAt = now, UpdatedAt = now };
            var shopOwner3 = new Account { Id = Guid.Parse("b0000000-0000-0000-0000-000000000003"), Username = "shopowner3", PasswordHash = pw, FullName = "Lê Minh Pastry", Email = "shop3@cakedesign.com", Phone = "0902000003", Role = "ShopOwner", IsApproved = true, WalletBalance = 2000000m, CreatedAt = now, UpdatedAt = now };

            var cust1 = new Account { Id = Guid.Parse("c0000000-0000-0000-0000-000000000001"), Username = "customer1", PasswordHash = pw, FullName = "Phạm Thị Mai", Email = "mai@gmail.com", Phone = "0903000001", Role = "Customer", IsApproved = true, WalletBalance = 2000000m, CreatedAt = now, UpdatedAt = now };
            var cust2 = new Account { Id = Guid.Parse("c0000000-0000-0000-0000-000000000002"), Username = "customer2", PasswordHash = pw, FullName = "Hoàng Văn Nam", Email = "nam@gmail.com", Phone = "0903000002", Role = "Customer", IsApproved = true, WalletBalance = 1500000m, CreatedAt = now, UpdatedAt = now };
            var cust3 = new Account { Id = Guid.Parse("c0000000-0000-0000-0000-000000000003"), Username = "customer3", PasswordHash = pw, FullName = "Đỗ Thanh Hà", Email = "ha@gmail.com", Phone = "0903000003", Role = "Customer", IsApproved = true, WalletBalance = 3000000m, CreatedAt = now, UpdatedAt = now };
            var cust4 = new Account { Id = Guid.Parse("c0000000-0000-0000-0000-000000000004"), Username = "customer4", PasswordHash = pw, FullName = "Vũ Quốc Bảo", Email = "bao@gmail.com", Phone = "0903000004", Role = "Customer", IsApproved = true, WalletBalance = 500000m, CreatedAt = now, UpdatedAt = now };
            var cust5 = new Account { Id = Guid.Parse("c0000000-0000-0000-0000-000000000005"), Username = "customer5", PasswordHash = pw, FullName = "Ngô Minh Tú", Email = "tu@gmail.com", Phone = "0903000005", Role = "Customer", IsApproved = true, WalletBalance = 800000m, CreatedAt = now, UpdatedAt = now };

            var staff1 = new Account { Id = Guid.Parse("d0000000-0000-0000-0000-000000000001"), Username = "staff1", PasswordHash = pw, FullName = "Lý Văn Staff", Email = "staff1@cakedesign.com", Phone = "0904000001", Role = "Staff", IsApproved = true, WalletBalance = 0, CreatedAt = now, UpdatedAt = now };
            var staff2 = new Account { Id = Guid.Parse("d0000000-0000-0000-0000-000000000002"), Username = "staff2", PasswordHash = pw, FullName = "Trương Thị Staff", Email = "staff2@cakedesign.com", Phone = "0904000002", Role = "Staff", IsApproved = true, WalletBalance = 0, CreatedAt = now, UpdatedAt = now };

            var sysStaff1 = new Account { Id = Guid.Parse("f0000000-0000-0000-0000-000000000001"), Username = "systemstaff1", PasswordHash = pw, FullName = "Nguyễn Hệ Thống", Email = "sysstaff1@cakedesign.com", Phone = "0905000001", Role = "SystemStaff", IsApproved = true, WalletBalance = 0, CreatedAt = now, UpdatedAt = now };
            var sysStaff2 = new Account { Id = Guid.Parse("f0000000-0000-0000-0000-000000000002"), Username = "systemstaff2", PasswordHash = pw, FullName = "Trần Quản Lý", Email = "sysstaff2@cakedesign.com", Phone = "0905000002", Role = "SystemStaff", IsApproved = true, WalletBalance = 0, CreatedAt = now, UpdatedAt = now };

            var accounts = new[] { admin1, admin2, shopOwner1, shopOwner2, shopOwner3, cust1, cust2, cust3, cust4, cust5, staff1, staff2, sysStaff1, sysStaff2 };
            db.Accounts.AddRange(accounts);

            // ========== SHOPS ==========
            var shop1 = new Shop { Id = Guid.Parse("51000000-0000-0000-0000-000000000001"), OwnerId = shopOwner1.Id, ShopName = "Tiệm Bánh Ngọt Ngào", Description = "Chuyên bánh sinh nhật, bánh kem tươi cao cấp", AvatarUrl = "https://picsum.photos/seed/shop1/200", BannerUrl = "https://picsum.photos/seed/shop1b/800/300", Address = "123 Nguyễn Huệ, Quận 1, TP.HCM", ProvinceId = 1, DistrictId = 1, WardCode = "1", Phone = "02812345678", WalletBalance = 5000000m, IsActive = true, CreatedAt = now, UpdatedAt = now };
            var shop2 = new Shop { Id = Guid.Parse("51000000-0000-0000-0000-000000000002"), OwnerId = shopOwner2.Id, ShopName = "Bakery Hạnh Phúc", Description = "Bánh mì, croissant, pastry kiểu Pháp", AvatarUrl = "https://picsum.photos/seed/shop2/200", BannerUrl = "https://picsum.photos/seed/shop2b/800/300", Address = "456 Lê Lợi, Quận 1, TP.HCM", ProvinceId = 1, DistrictId = 1, WardCode = "2", Phone = "02887654321", WalletBalance = 3000000m, IsActive = true, CreatedAt = now, UpdatedAt = now };
            var shop3 = new Shop { Id = Guid.Parse("51000000-0000-0000-0000-000000000003"), OwnerId = shopOwner3.Id, ShopName = "Sweet Dream Pastry", Description = "Cupcake, macaron và dessert phong cách Nhật", AvatarUrl = "https://picsum.photos/seed/shop3/200", BannerUrl = "https://picsum.photos/seed/shop3b/800/300", Address = "789 Trần Hưng Đạo, Quận 5, TP.HCM", ProvinceId = 1, DistrictId = 5, WardCode = "74", Phone = "02811223344", WalletBalance = 2000000m, IsActive = true, CreatedAt = now, UpdatedAt = now };

            db.Shops.AddRange(shop1, shop2, shop3);

            // ========== CATEGORIES ==========
            var cat1 = new Category { Id = Guid.Parse("ca000000-0000-0000-0000-000000000001"), Name = "Bánh Sinh Nhật", Description = "Bánh kem sinh nhật các loại", ImageUrl = "https://picsum.photos/seed/cat1/300", SortOrder = 1, IsActive = true, CreatedAt = now };
            var cat2 = new Category { Id = Guid.Parse("ca000000-0000-0000-0000-000000000002"), Name = "Bánh Cưới", Description = "Bánh kem cho tiệc cưới", ImageUrl = "https://picsum.photos/seed/cat2/300", SortOrder = 2, IsActive = true, CreatedAt = now };
            var cat3 = new Category { Id = Guid.Parse("ca000000-0000-0000-0000-000000000003"), Name = "Cupcake", Description = "Cupcake nhỏ xinh", ImageUrl = "https://picsum.photos/seed/cat3/300", SortOrder = 3, IsActive = true, CreatedAt = now };
            var cat4 = new Category { Id = Guid.Parse("ca000000-0000-0000-0000-000000000004"), Name = "Bánh Mì & Pastry", Description = "Bánh mì, croissant, danish", ImageUrl = "https://picsum.photos/seed/cat4/300", SortOrder = 4, IsActive = true, CreatedAt = now };
            var cat5 = new Category { Id = Guid.Parse("ca000000-0000-0000-0000-000000000005"), Name = "Macaron", Description = "Macaron phong cách Pháp", ImageUrl = "https://picsum.photos/seed/cat5/300", SortOrder = 5, IsActive = true, CreatedAt = now };
            var cat6 = new Category { Id = Guid.Parse("ca000000-0000-0000-0000-000000000006"), Name = "Bánh Mousse", Description = "Bánh mousse mềm mịn", ImageUrl = "https://picsum.photos/seed/cat6/300", SortOrder = 6, IsActive = true, CreatedAt = now };

            db.Categories.AddRange(cat1, cat2, cat3, cat4, cat5, cat6);

            // ========== TAGS ==========
            var tag1 = new Tag { Id = Guid.Parse("1a000000-0000-0000-0000-000000000001"), Name = "Best Seller", CreatedAt = now };
            var tag2 = new Tag { Id = Guid.Parse("1a000000-0000-0000-0000-000000000002"), Name = "Mới", CreatedAt = now };
            var tag3 = new Tag { Id = Guid.Parse("1a000000-0000-0000-0000-000000000003"), Name = "Giảm Giá", CreatedAt = now };
            var tag4 = new Tag { Id = Guid.Parse("1a000000-0000-0000-0000-000000000004"), Name = "Organic", CreatedAt = now };
            var tag5 = new Tag { Id = Guid.Parse("1a000000-0000-0000-0000-000000000005"), Name = "Không Đường", CreatedAt = now };
            var tag6 = new Tag { Id = Guid.Parse("1a000000-0000-0000-0000-000000000006"), Name = "Premium", CreatedAt = now };
            var tag7 = new Tag { Id = Guid.Parse("1a000000-0000-0000-0000-000000000007"), Name = "Vegan", CreatedAt = now };

            db.Tags.AddRange(tag1, tag2, tag3, tag4, tag5, tag6, tag7);

            // ========== PRODUCTS ==========
            var p1 = new Product { Id = Guid.Parse("e0000000-0000-0000-0000-000000000001"), ShopId = shop1.Id, CategoryId = cat1.Id, Name = "Bánh Kem Dâu Tây 3 Tầng", Price = 450000m, Description = "Bánh kem dâu tây tươi 3 tầng, trang trí hoa tươi", ImageUrl = "https://picsum.photos/seed/cake1/400", Stock = 20, IsActive = true, AverageRating = 4.5, ReviewCount = 3, CreatedAt = now, UpdatedAt = now };
            var p2 = new Product { Id = Guid.Parse("e0000000-0000-0000-0000-000000000002"), ShopId = shop1.Id, CategoryId = cat1.Id, Name = "Bánh Kem Socola Đen", Price = 380000m, Description = "Bánh kem socola đen đậm đà, phủ ganache", ImageUrl = "https://picsum.photos/seed/cake2/400", Stock = 15, IsActive = true, AverageRating = 4.8, ReviewCount = 2, CreatedAt = now, UpdatedAt = now };
            var p3 = new Product { Id = Guid.Parse("e0000000-0000-0000-0000-000000000003"), ShopId = shop1.Id, CategoryId = cat2.Id, Name = "Bánh Cưới Hoa Hồng Trắng", Price = 2500000m, Description = "Bánh cưới 5 tầng trang trí hoa hồng trắng", ImageUrl = "https://picsum.photos/seed/cake3/400", Stock = 5, IsActive = true, AverageRating = 5.0, ReviewCount = 1, CreatedAt = now, UpdatedAt = now };
            var p4 = new Product { Id = Guid.Parse("e0000000-0000-0000-0000-000000000004"), ShopId = shop1.Id, CategoryId = cat6.Id, Name = "Mousse Chanh Dây", Price = 280000m, Description = "Mousse chanh dây tươi mát", ImageUrl = "https://picsum.photos/seed/cake4/400", Stock = 25, IsActive = true, AverageRating = 4.2, ReviewCount = 2, CreatedAt = now, UpdatedAt = now };

            var p5 = new Product { Id = Guid.Parse("e0000000-0000-0000-0000-000000000005"), ShopId = shop2.Id, CategoryId = cat4.Id, Name = "Croissant Bơ Pháp", Price = 45000m, Description = "Croissant bơ Pháp giòn xốp", ImageUrl = "https://picsum.photos/seed/cake5/400", Stock = 100, IsActive = true, AverageRating = 4.7, ReviewCount = 4, CreatedAt = now, UpdatedAt = now };
            var p6 = new Product { Id = Guid.Parse("e0000000-0000-0000-0000-000000000006"), ShopId = shop2.Id, CategoryId = cat4.Id, Name = "Bánh Mì Sourdough", Price = 65000m, Description = "Bánh mì sourdough lên men tự nhiên", ImageUrl = "https://picsum.photos/seed/cake6/400", Stock = 50, IsActive = true, AverageRating = 4.3, ReviewCount = 2, CreatedAt = now, UpdatedAt = now };
            var p7 = new Product { Id = Guid.Parse("e0000000-0000-0000-0000-000000000007"), ShopId = shop2.Id, CategoryId = cat1.Id, Name = "Bánh Kem Tiramisu", Price = 420000m, Description = "Bánh kem tiramisu kiểu Ý", ImageUrl = "https://picsum.photos/seed/cake7/400", Stock = 12, IsActive = true, AverageRating = 4.9, ReviewCount = 3, CreatedAt = now, UpdatedAt = now };
            var p8 = new Product { Id = Guid.Parse("e0000000-0000-0000-0000-000000000008"), ShopId = shop2.Id, CategoryId = cat3.Id, Name = "Cupcake Red Velvet (Hộp 6)", Price = 180000m, Description = "6 cupcake red velvet kem phô mai", ImageUrl = "https://picsum.photos/seed/cake8/400", Stock = 30, IsActive = true, AverageRating = 4.6, ReviewCount = 2, CreatedAt = now, UpdatedAt = now };

            var p9 = new Product { Id = Guid.Parse("e0000000-0000-0000-0000-000000000009"), ShopId = shop3.Id, CategoryId = cat5.Id, Name = "Macaron Hộp 12 Vị", Price = 320000m, Description = "12 macaron với các vị: dâu, socola, matcha, vanilla...", ImageUrl = "https://picsum.photos/seed/cake9/400", Stock = 40, IsActive = true, AverageRating = 4.4, ReviewCount = 3, CreatedAt = now, UpdatedAt = now };
            var p10 = new Product { Id = Guid.Parse("e0000000-0000-0000-0000-00000000000a"), ShopId = shop3.Id, CategoryId = cat3.Id, Name = "Cupcake Matcha Hộp 4", Price = 120000m, Description = "4 cupcake matcha Nhật Bản", ImageUrl = "https://picsum.photos/seed/cake10/400", Stock = 35, IsActive = true, AverageRating = 4.1, ReviewCount = 1, CreatedAt = now, UpdatedAt = now };
            var p11 = new Product { Id = Guid.Parse("e0000000-0000-0000-0000-00000000000b"), ShopId = shop3.Id, CategoryId = cat6.Id, Name = "Mousse Trà Xanh", Price = 250000m, Description = "Mousse trà xanh Uji Nhật", ImageUrl = "https://picsum.photos/seed/cake11/400", Stock = 18, IsActive = true, AverageRating = 4.7, ReviewCount = 2, CreatedAt = now, UpdatedAt = now };
            var p12 = new Product { Id = Guid.Parse("e0000000-0000-0000-0000-00000000000c"), ShopId = shop3.Id, CategoryId = cat1.Id, Name = "Bánh Kem Cheese Nhật", Price = 350000m, Description = "Bánh kem phô mai kiểu Nhật mềm mịn", ImageUrl = "https://picsum.photos/seed/cake12/400", Stock = 10, IsActive = true, AverageRating = 4.8, ReviewCount = 2, CreatedAt = now, UpdatedAt = now };

            var products = new[] { p1, p2, p3, p4, p5, p6, p7, p8, p9, p10, p11, p12 };
            db.Products.AddRange(products);

            // ========== PRODUCT TAGS ==========
            db.ProductTags.AddRange(
                new ProductTag { Id = Guid.NewGuid(), ProductId = p1.Id, TagId = tag1.Id },
                new ProductTag { Id = Guid.NewGuid(), ProductId = p1.Id, TagId = tag2.Id },
                new ProductTag { Id = Guid.NewGuid(), ProductId = p2.Id, TagId = tag1.Id },
                new ProductTag { Id = Guid.NewGuid(), ProductId = p3.Id, TagId = tag6.Id },
                new ProductTag { Id = Guid.NewGuid(), ProductId = p4.Id, TagId = tag2.Id },
                new ProductTag { Id = Guid.NewGuid(), ProductId = p5.Id, TagId = tag1.Id },
                new ProductTag { Id = Guid.NewGuid(), ProductId = p5.Id, TagId = tag4.Id },
                new ProductTag { Id = Guid.NewGuid(), ProductId = p6.Id, TagId = tag4.Id },
                new ProductTag { Id = Guid.NewGuid(), ProductId = p7.Id, TagId = tag6.Id },
                new ProductTag { Id = Guid.NewGuid(), ProductId = p8.Id, TagId = tag3.Id },
                new ProductTag { Id = Guid.NewGuid(), ProductId = p9.Id, TagId = tag1.Id },
                new ProductTag { Id = Guid.NewGuid(), ProductId = p9.Id, TagId = tag6.Id },
                new ProductTag { Id = Guid.NewGuid(), ProductId = p10.Id, TagId = tag7.Id },
                new ProductTag { Id = Guid.NewGuid(), ProductId = p11.Id, TagId = tag5.Id },
                new ProductTag { Id = Guid.NewGuid(), ProductId = p12.Id, TagId = tag2.Id }
            );

            // ========== ADDRESSES ==========
            var addr1 = new Address { Id = Guid.Parse("ad000000-0000-0000-0000-000000000001"), UserId = cust1.Id, ReceiverName = "Phạm Thị Mai", Phone = "0903000001", Street = "12 Nguyễn Trãi", Ward = "Phường Bến Thành", District = "Quận 1", City = "TP. Hồ Chí Minh", ProvinceId = 1, DistrictId = 1, WardCode = "1", IsDefault = true, CreatedAt = now };
            var addr2 = new Address { Id = Guid.Parse("ad000000-0000-0000-0000-000000000002"), UserId = cust1.Id, ReceiverName = "Phạm Thị Mai (Cty)", Phone = "0903000001", Street = "88 Cách Mạng Tháng 8", Ward = "Phường 7", District = "Quận 3", City = "TP. Hồ Chí Minh", ProvinceId = 1, DistrictId = 3, WardCode = "45", IsDefault = false, CreatedAt = now };
            var addr3 = new Address { Id = Guid.Parse("ad000000-0000-0000-0000-000000000003"), UserId = cust2.Id, ReceiverName = "Hoàng Văn Nam", Phone = "0903000002", Street = "45 Lê Duẩn", Ward = "Phường Bến Nghé", District = "Quận 1", City = "TP. Hồ Chí Minh", ProvinceId = 1, DistrictId = 1, WardCode = "2", IsDefault = true, CreatedAt = now };
            var addr4 = new Address { Id = Guid.Parse("ad000000-0000-0000-0000-000000000004"), UserId = cust3.Id, ReceiverName = "Đỗ Thanh Hà", Phone = "0903000003", Street = "99 Phạm Văn Đồng", Ward = "Phường Hiệp Bình Chánh", District = "TP. Thủ Đức", City = "TP. Hồ Chí Minh", ProvinceId = 1, DistrictId = 16, WardCode = "220", IsDefault = true, CreatedAt = now };
            var addr5 = new Address { Id = Guid.Parse("ad000000-0000-0000-0000-000000000005"), UserId = cust4.Id, ReceiverName = "Vũ Quốc Bảo", Phone = "0903000004", Street = "22 Hai Bà Trưng", Ward = "Phường Tân Định", District = "Quận 1", City = "TP. Hồ Chí Minh", ProvinceId = 1, DistrictId = 1, WardCode = "4", IsDefault = true, CreatedAt = now };
            var addr6 = new Address { Id = Guid.Parse("ad000000-0000-0000-0000-000000000006"), UserId = cust5.Id, ReceiverName = "Ngô Minh Tú", Phone = "0903000005", Street = "55 Võ Văn Tần", Ward = "Phường 6", District = "Quận 3", City = "TP. Hồ Chí Minh", ProvinceId = 1, DistrictId = 3, WardCode = "44", IsDefault = true, CreatedAt = now };

            db.Addresses.AddRange(addr1, addr2, addr3, addr4, addr5, addr6);

            // Update default address for customers
            cust1.DefaultAddressId = addr1.Id;
            cust2.DefaultAddressId = addr3.Id;
            cust3.DefaultAddressId = addr4.Id;
            cust4.DefaultAddressId = addr5.Id;
            cust5.DefaultAddressId = addr6.Id;

            // ========== CARTS ==========
            var cart1 = new Cart { Id = Guid.Parse("c1000000-0000-0000-0000-000000000001"), UserId = cust1.Id };
            var cart2 = new Cart { Id = Guid.Parse("c1000000-0000-0000-0000-000000000002"), UserId = cust2.Id };
            var cart3 = new Cart { Id = Guid.Parse("c1000000-0000-0000-0000-000000000003"), UserId = cust3.Id };
            var cart4 = new Cart { Id = Guid.Parse("c1000000-0000-0000-0000-000000000004"), UserId = cust4.Id };
            var cart5 = new Cart { Id = Guid.Parse("c1000000-0000-0000-0000-000000000005"), UserId = cust5.Id };

            db.Carts.AddRange(cart1, cart2, cart3, cart4, cart5);

            // ========== CART ITEMS ==========
            db.CartItems.AddRange(
                new CartItem { Id = Guid.NewGuid(), CartId = cart1.Id, ProductId = p5.Id, Quantity = 3 },
                new CartItem { Id = Guid.NewGuid(), CartId = cart1.Id, ProductId = p9.Id, Quantity = 1 },
                new CartItem { Id = Guid.NewGuid(), CartId = cart2.Id, ProductId = p1.Id, Quantity = 1 },
                new CartItem { Id = Guid.NewGuid(), CartId = cart3.Id, ProductId = p7.Id, Quantity = 1 },
                new CartItem { Id = Guid.NewGuid(), CartId = cart3.Id, ProductId = p12.Id, Quantity = 2 },
                new CartItem { Id = Guid.NewGuid(), CartId = cart4.Id, ProductId = p8.Id, Quantity = 2 },
                new CartItem { Id = Guid.NewGuid(), CartId = cart5.Id, ProductId = p11.Id, Quantity = 1 }
            );

            // ========== ORDERS ==========
            var ord1 = new Order { Id = Guid.Parse("0d000000-0000-0000-0000-000000000001"), UserId = cust1.Id, ShopId = shop1.Id, ShippingAddressId = addr1.Id, TotalAmount = 830000m, Status = "Completed", PaymentMethod = "Wallet", PaymentStatus = "Paid", Note = "Giao trước 5h chiều", CreatedAt = now.AddDays(-10), UpdatedAt = now.AddDays(-8) };
            var ord2 = new Order { Id = Guid.Parse("0d000000-0000-0000-0000-000000000002"), UserId = cust1.Id, ShopId = shop2.Id, ShippingAddressId = addr1.Id, TotalAmount = 135000m, Status = "Completed", PaymentMethod = "Wallet", PaymentStatus = "Paid", CreatedAt = now.AddDays(-7), UpdatedAt = now.AddDays(-5) };
            var ord3 = new Order { Id = Guid.Parse("0d000000-0000-0000-0000-000000000003"), UserId = cust2.Id, ShopId = shop1.Id, ShippingAddressId = addr3.Id, TotalAmount = 2500000m, Status = "Confirmed", PaymentMethod = "Wallet", PaymentStatus = "Paid", Note = "Bánh cưới cho ngày 15/3", CreatedAt = now.AddDays(-3), UpdatedAt = now.AddDays(-2) };
            var ord4 = new Order { Id = Guid.Parse("0d000000-0000-0000-0000-000000000004"), UserId = cust3.Id, ShopId = shop3.Id, ShippingAddressId = addr4.Id, TotalAmount = 570000m, Status = "Shipping", PaymentMethod = "Wallet", PaymentStatus = "Paid", CreatedAt = now.AddDays(-2), UpdatedAt = now.AddDays(-1) };
            var ord5 = new Order { Id = Guid.Parse("0d000000-0000-0000-0000-000000000005"), UserId = cust4.Id, ShopId = shop2.Id, ShippingAddressId = addr5.Id, TotalAmount = 420000m, Status = "Pending", PaymentMethod = "Wallet", PaymentStatus = "Pending", CreatedAt = now.AddDays(-1), UpdatedAt = now.AddDays(-1) };
            var ord6 = new Order { Id = Guid.Parse("0d000000-0000-0000-0000-000000000006"), UserId = cust5.Id, ShopId = shop3.Id, ShippingAddressId = addr6.Id, TotalAmount = 320000m, Status = "Completed", PaymentMethod = "Wallet", PaymentStatus = "Paid", CreatedAt = now.AddDays(-15), UpdatedAt = now.AddDays(-12) };
            var ord7 = new Order { Id = Guid.Parse("0d000000-0000-0000-0000-000000000007"), UserId = cust2.Id, ShopId = shop2.Id, ShippingAddressId = addr3.Id, TotalAmount = 180000m, Status = "Cancelled", PaymentMethod = "Wallet", PaymentStatus = "Refunded", Note = "Đổi ý", CreatedAt = now.AddDays(-5), UpdatedAt = now.AddDays(-4) };
            var ord8 = new Order { Id = Guid.Parse("0d000000-0000-0000-0000-000000000008"), UserId = cust3.Id, ShopId = shop1.Id, ShippingAddressId = addr4.Id, TotalAmount = 450000m, Status = "Completed", PaymentMethod = "Wallet", PaymentStatus = "Paid", CreatedAt = now.AddDays(-20), UpdatedAt = now.AddDays(-17) };

            db.Orders.AddRange(ord1, ord2, ord3, ord4, ord5, ord6, ord7, ord8);

            // ========== ORDER ITEMS ==========
            db.OrderItems.AddRange(
                new OrderItem { Id = Guid.NewGuid(), OrderId = ord1.Id, ProductId = p1.Id, Quantity = 1, PriceAtPurchase = 450000m },
                new OrderItem { Id = Guid.NewGuid(), OrderId = ord1.Id, ProductId = p2.Id, Quantity = 1, PriceAtPurchase = 380000m },
                new OrderItem { Id = Guid.NewGuid(), OrderId = ord2.Id, ProductId = p5.Id, Quantity = 3, PriceAtPurchase = 45000m },
                new OrderItem { Id = Guid.NewGuid(), OrderId = ord3.Id, ProductId = p3.Id, Quantity = 1, PriceAtPurchase = 2500000m },
                new OrderItem { Id = Guid.NewGuid(), OrderId = ord4.Id, ProductId = p9.Id, Quantity = 1, PriceAtPurchase = 320000m },
                new OrderItem { Id = Guid.NewGuid(), OrderId = ord4.Id, ProductId = p11.Id, Quantity = 1, PriceAtPurchase = 250000m },
                new OrderItem { Id = Guid.NewGuid(), OrderId = ord5.Id, ProductId = p7.Id, Quantity = 1, PriceAtPurchase = 420000m },
                new OrderItem { Id = Guid.NewGuid(), OrderId = ord6.Id, ProductId = p9.Id, Quantity = 1, PriceAtPurchase = 320000m },
                new OrderItem { Id = Guid.NewGuid(), OrderId = ord7.Id, ProductId = p8.Id, Quantity = 1, PriceAtPurchase = 180000m },
                new OrderItem { Id = Guid.NewGuid(), OrderId = ord8.Id, ProductId = p1.Id, Quantity = 1, PriceAtPurchase = 450000m }
            );

            // ========== PAYMENTS ==========
            db.Payments.AddRange(
                new Payment { Id = Guid.NewGuid(), OrderId = ord1.Id, UserId = cust1.Id, Amount = 830000m, Method = "Wallet", Status = "Completed", CreatedAt = ord1.CreatedAt, CompletedAt = ord1.CreatedAt },
                new Payment { Id = Guid.NewGuid(), OrderId = ord2.Id, UserId = cust1.Id, Amount = 135000m, Method = "Wallet", Status = "Completed", CreatedAt = ord2.CreatedAt, CompletedAt = ord2.CreatedAt },
                new Payment { Id = Guid.NewGuid(), OrderId = ord3.Id, UserId = cust2.Id, Amount = 2500000m, Method = "Wallet", Status = "Completed", CreatedAt = ord3.CreatedAt, CompletedAt = ord3.CreatedAt },
                new Payment { Id = Guid.NewGuid(), OrderId = ord4.Id, UserId = cust3.Id, Amount = 570000m, Method = "Wallet", Status = "Completed", CreatedAt = ord4.CreatedAt, CompletedAt = ord4.CreatedAt },
                new Payment { Id = Guid.NewGuid(), OrderId = ord5.Id, UserId = cust4.Id, Amount = 420000m, Method = "Wallet", Status = "Pending", CreatedAt = ord5.CreatedAt },
                new Payment { Id = Guid.NewGuid(), OrderId = ord6.Id, UserId = cust5.Id, Amount = 320000m, Method = "Wallet", Status = "Completed", CreatedAt = ord6.CreatedAt, CompletedAt = ord6.CreatedAt },
                new Payment { Id = Guid.NewGuid(), OrderId = ord7.Id, UserId = cust2.Id, Amount = 180000m, Method = "Wallet", Status = "Refunded", CreatedAt = ord7.CreatedAt, CompletedAt = ord7.UpdatedAt },
                new Payment { Id = Guid.NewGuid(), OrderId = ord8.Id, UserId = cust3.Id, Amount = 450000m, Method = "Wallet", Status = "Completed", CreatedAt = ord8.CreatedAt, CompletedAt = ord8.CreatedAt }
            );

            // ========== REVIEWS ==========
            db.Reviews.AddRange(
                new Review { Id = Guid.NewGuid(), ProductId = p1.Id, UserId = cust1.Id, Rating = 5, Comment = "Bánh rất ngon, trang trí đẹp!", CreatedAt = now.AddDays(-7), UpdatedAt = now.AddDays(-7) },
                new Review { Id = Guid.NewGuid(), ProductId = p1.Id, UserId = cust3.Id, Rating = 4, Comment = "Bánh ngon, giao hàng nhanh", CreatedAt = now.AddDays(-16), UpdatedAt = now.AddDays(-16) },
                new Review { Id = Guid.NewGuid(), ProductId = p2.Id, UserId = cust1.Id, Rating = 5, Comment = "Socola đậm đà tuyệt vời!", CreatedAt = now.AddDays(-7), UpdatedAt = now.AddDays(-7) },
                new Review { Id = Guid.NewGuid(), ProductId = p3.Id, UserId = cust2.Id, Rating = 5, Comment = "Bánh cưới rất đẹp, đúng yêu cầu", CreatedAt = now.AddDays(-1), UpdatedAt = now.AddDays(-1) },
                new Review { Id = Guid.NewGuid(), ProductId = p5.Id, UserId = cust1.Id, Rating = 5, Comment = "Croissant giòn rụm, bơ thơm!", CreatedAt = now.AddDays(-4), UpdatedAt = now.AddDays(-4) },
                new Review { Id = Guid.NewGuid(), ProductId = p5.Id, UserId = cust2.Id, Rating = 4, Comment = "Ngon nhưng hơi nhỏ", CreatedAt = now.AddDays(-3), UpdatedAt = now.AddDays(-3) },
                new Review { Id = Guid.NewGuid(), ProductId = p7.Id, UserId = cust3.Id, Rating = 5, Comment = "Tiramisu chuẩn vị Ý!", CreatedAt = now.AddDays(-1), UpdatedAt = now.AddDays(-1) },
                new Review { Id = Guid.NewGuid(), ProductId = p9.Id, UserId = cust5.Id, Rating = 4, Comment = "Macaron xinh xắn, vị ngon", CreatedAt = now.AddDays(-10), UpdatedAt = now.AddDays(-10) },
                new Review { Id = Guid.NewGuid(), ProductId = p9.Id, UserId = cust3.Id, Rating = 5, Comment = "Rất hài lòng, sẽ mua lại!", CreatedAt = now.AddDays(-1), UpdatedAt = now.AddDays(-1) },
                new Review { Id = Guid.NewGuid(), ProductId = p11.Id, UserId = cust3.Id, Rating = 5, Comment = "Mousse matcha thơm ngon", CreatedAt = now.AddDays(-1), UpdatedAt = now.AddDays(-1) },
                new Review { Id = Guid.NewGuid(), ProductId = p12.Id, UserId = cust5.Id, Rating = 5, Comment = "Cheese cake mềm mịn, không quá ngọt", CreatedAt = now.AddDays(-8), UpdatedAt = now.AddDays(-8) }
            );

            // ========== WISHLIST ITEMS ==========
            db.WishlistItems.AddRange(
                new WishlistItem { Id = Guid.NewGuid(), UserId = cust1.Id, ProductId = p3.Id, CreatedAt = now.AddDays(-5) },
                new WishlistItem { Id = Guid.NewGuid(), UserId = cust1.Id, ProductId = p7.Id, CreatedAt = now.AddDays(-3) },
                new WishlistItem { Id = Guid.NewGuid(), UserId = cust1.Id, ProductId = p12.Id, CreatedAt = now.AddDays(-1) },
                new WishlistItem { Id = Guid.NewGuid(), UserId = cust2.Id, ProductId = p9.Id, CreatedAt = now.AddDays(-4) },
                new WishlistItem { Id = Guid.NewGuid(), UserId = cust2.Id, ProductId = p11.Id, CreatedAt = now.AddDays(-2) },
                new WishlistItem { Id = Guid.NewGuid(), UserId = cust3.Id, ProductId = p5.Id, CreatedAt = now.AddDays(-6) },
                new WishlistItem { Id = Guid.NewGuid(), UserId = cust4.Id, ProductId = p1.Id, CreatedAt = now.AddDays(-3) },
                new WishlistItem { Id = Guid.NewGuid(), UserId = cust4.Id, ProductId = p9.Id, CreatedAt = now.AddDays(-1) },
                new WishlistItem { Id = Guid.NewGuid(), UserId = cust5.Id, ProductId = p3.Id, CreatedAt = now.AddDays(-7) },
                new WishlistItem { Id = Guid.NewGuid(), UserId = cust5.Id, ProductId = p7.Id, CreatedAt = now.AddDays(-2) }
            );

            // ========== WALLET TRANSACTIONS ==========
            db.WalletTransactions.AddRange(
                new WalletTransaction { Id = Guid.NewGuid(), WalletOwnerId = cust1.Id, WalletType = "User", Amount = 5000000m, TransactionType = "Deposit", Description = "Nạp tiền vào ví", BalanceAfter = 5000000m, CreatedAt = now.AddDays(-30) },
                new WalletTransaction { Id = Guid.NewGuid(), WalletOwnerId = cust1.Id, WalletType = "User", Amount = -830000m, TransactionType = "Purchase", Description = "Thanh toán đơn hàng #1", BalanceAfter = 4170000m, ReferenceId = ord1.Id, CreatedAt = ord1.CreatedAt },
                new WalletTransaction { Id = Guid.NewGuid(), WalletOwnerId = cust1.Id, WalletType = "User", Amount = -135000m, TransactionType = "Purchase", Description = "Thanh toán đơn hàng #2", BalanceAfter = 4035000m, ReferenceId = ord2.Id, CreatedAt = ord2.CreatedAt },
                new WalletTransaction { Id = Guid.NewGuid(), WalletOwnerId = cust2.Id, WalletType = "User", Amount = 5000000m, TransactionType = "Deposit", Description = "Nạp tiền vào ví", BalanceAfter = 5000000m, CreatedAt = now.AddDays(-20) },
                new WalletTransaction { Id = Guid.NewGuid(), WalletOwnerId = cust2.Id, WalletType = "User", Amount = -2500000m, TransactionType = "Purchase", Description = "Thanh toán đơn hàng #3", BalanceAfter = 2500000m, ReferenceId = ord3.Id, CreatedAt = ord3.CreatedAt },
                new WalletTransaction { Id = Guid.NewGuid(), WalletOwnerId = cust3.Id, WalletType = "User", Amount = 5000000m, TransactionType = "Deposit", Description = "Nạp tiền vào ví", BalanceAfter = 5000000m, CreatedAt = now.AddDays(-25) },
                new WalletTransaction { Id = Guid.NewGuid(), WalletOwnerId = cust3.Id, WalletType = "User", Amount = -570000m, TransactionType = "Purchase", Description = "Thanh toán đơn hàng #4", BalanceAfter = 4430000m, ReferenceId = ord4.Id, CreatedAt = ord4.CreatedAt },
                new WalletTransaction { Id = Guid.NewGuid(), WalletOwnerId = shopOwner1.Id, WalletType = "User", Amount = 830000m, TransactionType = "Sale", Description = "Nhận tiền đơn hàng #1", BalanceAfter = 5830000m, ReferenceId = ord1.Id, CreatedAt = ord1.UpdatedAt },
                new WalletTransaction { Id = Guid.NewGuid(), WalletOwnerId = shopOwner2.Id, WalletType = "User", Amount = 135000m, TransactionType = "Sale", Description = "Nhận tiền đơn hàng #2", BalanceAfter = 3135000m, ReferenceId = ord2.Id, CreatedAt = ord2.UpdatedAt }
            );

            // ========== SHOP STAFF ==========
            db.ShopStaff.AddRange(
                new ShopStaff { Id = Guid.NewGuid(), ShopId = shop1.Id, AccountId = staff1.Id, Role = "Manager", CreatedAt = now },
                new ShopStaff { Id = Guid.NewGuid(), ShopId = shop2.Id, AccountId = staff2.Id, Role = "Staff", CreatedAt = now }
            );

            // ========== REPORTS ==========
            db.Reports.AddRange(
                new Report { Id = Guid.NewGuid(), ReporterId = cust4.Id, TargetType = "Product", TargetId = p6.Id, Reason = "Mô tả không chính xác", Description = "Bánh mì nhận được không giống mô tả", Status = "Pending", CreatedAt = now.AddDays(-2) },
                new Report { Id = Guid.NewGuid(), ReporterId = cust5.Id, TargetType = "Shop", TargetId = shop2.Id, Reason = "Giao hàng chậm", Description = "Đặt hàng 3 ngày mà chưa giao", Status = "Reviewed", AdminNote = "Đã liên hệ shop để xác nhận", CreatedAt = now.AddDays(-5), ResolvedAt = now.AddDays(-3) },
                new Report { Id = Guid.NewGuid(), ReporterId = cust1.Id, TargetType = "Product", TargetId = p10.Id, Reason = "Chất lượng kém", Description = "Cupcake bị khô, không ngon", Status = "Resolved", AdminNote = "Shop đã hoàn tiền cho khách", CreatedAt = now.AddDays(-12), ResolvedAt = now.AddDays(-10) }
            );

            await db.SaveChangesAsync();

            Console.WriteLine("[Seed] ✅ Mock data seeded successfully!");
            Console.WriteLine("[Seed] Accounts: 14 (2 Admin, 3 ShopOwner, 5 Customer, 2 Staff, 2 SystemStaff)");
            Console.WriteLine("[Seed] Shops: 3 | Categories: 6 | Tags: 7");
            Console.WriteLine("[Seed] Products: 12 | ProductTags: 15");
            Console.WriteLine("[Seed] Addresses: 6 | Carts: 5 | CartItems: 7");
            Console.WriteLine("[Seed] Orders: 8 | OrderItems: 10 | Payments: 8");
            Console.WriteLine("[Seed] Reviews: 11 | WishlistItems: 10");
            Console.WriteLine("[Seed] WalletTransactions: 9 | ShopStaff: 2 | Reports: 3");
            Console.WriteLine("[Seed] ─────────────────────────────────────");
            Console.WriteLine($"[Seed] All passwords: (configured in SeedData:DefaultPassword)");
        }
    }
}
