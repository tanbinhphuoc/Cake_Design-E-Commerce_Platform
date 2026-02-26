using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;

namespace Application.Services
{
    public class ShopService : IShopService
    {
        private readonly IUnitOfWork _uow;
        public ShopService(IUnitOfWork uow) { _uow = uow; }

        public async Task<Guid?> GetShopIdForUserAsync(Guid userId)
        {
            var shop = await _uow.Shops.GetByOwnerIdAsync(userId);
            if (shop != null) return shop.Id;
            var staff = await _uow.ShopStaff.GetByAccountIdAsync(userId);
            return staff?.ShopId;
        }

        public async Task<object> RequestShopAsync(Guid userId, UpdateShopDto dto)
        {
            var account = await _uow.Accounts.GetByIdWithShopAsync(userId);
            if (account == null) throw new UnauthorizedAccessException();
            if (account.Role == "ShopOwner" && account.Shop != null) throw new InvalidOperationException("You are already a shop owner.");
            if (account.Role != "Customer") throw new InvalidOperationException("Only customers can request.");

            // Validate required fields
            if (string.IsNullOrWhiteSpace(dto.ShopName)) throw new ArgumentException("Shop name is required.");
            if (string.IsNullOrWhiteSpace(dto.Phone)) throw new ArgumentException("Phone is required.");
            if (string.IsNullOrWhiteSpace(dto.Address)) throw new ArgumentException("Address is required.");

            var shop = new Shop
            {
                Id = Guid.NewGuid(), OwnerId = account.Id,
                ShopName = dto.ShopName,
                Description = dto.Description ?? "", AvatarUrl = dto.AvatarUrl ?? "",
                BannerUrl = dto.BannerUrl ?? "", Address = dto.Address,
                Phone = dto.Phone, IsActive = false,
                WalletBalance = 0, // Shop wallet not used separately, owner's Account.WalletBalance is the wallet
                CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
            };
            await _uow.Shops.AddAsync(shop);
            account.IsApproved = false; account.UpdatedAt = DateTime.UtcNow;
            await _uow.SaveChangesAsync();
            return new { Message = "Shop request submitted. Awaiting admin approval.", ShopId = shop.Id };
        }

        public async Task<string> ApproveShopAsync(Guid userId)
        {
            var account = await _uow.Accounts.GetByIdWithShopAsync(userId);
            if (account == null) throw new ArgumentException("User not found.");
            if (account.Shop == null) throw new ArgumentException("No pending shop request.");
            if (account.IsApproved && account.Role == "ShopOwner") throw new InvalidOperationException("Shop is already approved.");
            account.IsApproved = true; account.Role = "ShopOwner"; account.Shop.IsActive = true;
            account.Shop.UpdatedAt = DateTime.UtcNow; account.UpdatedAt = DateTime.UtcNow;
            await _uow.SaveChangesAsync();
            return $"Shop approved for '{account.Username}'.";
        }

        public async Task<ShopProfileDto?> GetMyShopAsync(Guid userId)
        {
            var shop = await _uow.Shops.GetByOwnerIdAsync(userId);
            if (shop == null) return null;
            // Get owner's wallet balance as the shop wallet
            var owner = await _uow.Accounts.GetByIdAsync(shop.OwnerId);
            return new ShopProfileDto
            {
                Id = shop.Id, OwnerId = shop.OwnerId, ShopName = shop.ShopName,
                Description = shop.Description, AvatarUrl = shop.AvatarUrl,
                BannerUrl = shop.BannerUrl, Address = shop.Address,
                Phone = shop.Phone, IsActive = shop.IsActive, CreatedAt = shop.CreatedAt
            };
        }

        public async Task<string> UpdateMyShopAsync(Guid userId, UpdateShopDto dto)
        {
            var shop = await _uow.Shops.GetByOwnerIdAsync(userId);
            if (shop == null) throw new ArgumentException("Shop not found.");
            if (dto.ShopName != null) shop.ShopName = dto.ShopName;
            if (dto.Description != null) shop.Description = dto.Description;
            if (dto.AvatarUrl != null) shop.AvatarUrl = dto.AvatarUrl;
            if (dto.BannerUrl != null) shop.BannerUrl = dto.BannerUrl;
            if (dto.Address != null) shop.Address = dto.Address;
            if (dto.Phone != null) shop.Phone = dto.Phone;
            shop.UpdatedAt = DateTime.UtcNow;
            await _uow.SaveChangesAsync();
            return "Shop updated successfully.";
        }

        public async Task<ShopPublicDto?> GetShopByIdAsync(Guid shopId)
        {
            var shop = await _uow.Shops.GetByIdWithProductsAsync(shopId);
            if (shop == null || !shop.IsActive) return null;
            return new ShopPublicDto { Id = shop.Id, ShopName = shop.ShopName, Description = shop.Description, AvatarUrl = shop.AvatarUrl, BannerUrl = shop.BannerUrl, Address = shop.Address, ProductCount = shop.Products.Count(p => p.IsActive), CreatedAt = shop.CreatedAt };
        }

        public async Task<Guid> AddStaffAsync(Guid ownerId, AddStaffDto dto)
        {
            var shop = await _uow.Shops.GetByOwnerIdAsync(ownerId);
            if (shop == null) throw new ArgumentException("Shop not found.");
            var staffAccount = await _uow.Accounts.GetByIdAsync(dto.AccountId);
            if (staffAccount == null) throw new ArgumentException("Account not found.");
            if (staffAccount.Id == ownerId) throw new InvalidOperationException("Cannot add yourself as staff.");
            if (await _uow.ShopStaff.GetByShopAndAccountAsync(shop.Id, dto.AccountId) != null) throw new InvalidOperationException("Already staff.");
            var staff = new ShopStaff { Id = Guid.NewGuid(), ShopId = shop.Id, AccountId = dto.AccountId, Role = dto.Role, CreatedAt = DateTime.UtcNow };
            staffAccount.Role = "Staff"; staffAccount.UpdatedAt = DateTime.UtcNow;
            await _uow.ShopStaff.AddAsync(staff);
            await _uow.SaveChangesAsync();
            return staff.Id;
        }

        public async Task<List<StaffDto>> GetStaffAsync(Guid ownerId)
        {
            var shop = await _uow.Shops.GetByOwnerIdAsync(ownerId);
            if (shop == null) throw new ArgumentException("Shop not found.");
            var list = await _uow.ShopStaff.GetByShopIdWithAccountAsync(shop.Id);
            return list.Select(ss => new StaffDto { Id = ss.Id, AccountId = ss.AccountId, Username = ss.Account.Username, Role = ss.Role, CreatedAt = ss.CreatedAt }).ToList();
        }

        public async Task<string> RemoveStaffAsync(Guid ownerId, Guid staffId)
        {
            var shop = await _uow.Shops.GetByOwnerIdAsync(ownerId);
            if (shop == null) throw new ArgumentException("Shop not found.");
            var staff = await _uow.ShopStaff.FirstOrDefaultAsync(ss => ss.Id == staffId && ss.ShopId == shop.Id);
            if (staff == null) throw new ArgumentException("Staff not found.");
            var acc = await _uow.Accounts.GetByIdAsync(staff.AccountId);
            if (acc != null) { acc.Role = "Customer"; acc.UpdatedAt = DateTime.UtcNow; }
            _uow.ShopStaff.Remove(staff);
            await _uow.SaveChangesAsync();
            return "Staff removed successfully.";
        }

        public async Task<List<ProductDetailDto>> GetMyProductsAsync(Guid userId)
        {
            var shopId = await GetShopIdForUserAsync(userId);
            if (shopId == null) throw new ArgumentException("Shop not found.");
            var products = await _uow.Products.FindAsync(p => p.ShopId == shopId);
            var result = new List<ProductDetailDto>();
            foreach (var p in products.OrderByDescending(p => p.CreatedAt))
            {
                var full = await _uow.Products.GetByIdWithDetailsAsync(p.Id);
                if (full != null) result.Add(MapProduct(full));
            }
            return result;
        }

        public async Task<object> CreateProductAsync(Guid userId, CreateProductExtendedDto dto)
        {
            var account = await _uow.Accounts.GetByIdWithShopAsync(userId);
            if (account == null) throw new UnauthorizedAccessException();
            var shopId = await GetShopIdForUserAsync(userId);
            if (shopId == null) throw new ArgumentException("Shop not found.");
            if (!account.IsApproved) throw new InvalidOperationException("Shop not approved yet.");

            // Validation
            if (string.IsNullOrWhiteSpace(dto.Name)) throw new ArgumentException("Product name is required.");
            if (dto.Name.Length > 200) throw new ArgumentException("Product name must not exceed 200 characters.");
            if (dto.Price <= 0) throw new ArgumentException("Price must be greater than 0.");
            if (dto.Stock < 0) throw new ArgumentException("Stock cannot be negative.");

            // Validate CategoryId exists if provided
            if (dto.CategoryId.HasValue)
            {
                var category = await _uow.Categories.GetByIdAsync(dto.CategoryId.Value);
                if (category == null) throw new ArgumentException($"Category with ID '{dto.CategoryId.Value}' not found. Please create the category first or provide a valid CategoryId.");
            }

            // Validate TagIds exist if provided
            if (dto.TagIds != null)
            {
                foreach (var tagId in dto.TagIds)
                {
                    var tag = await _uow.Tags.GetByIdAsync(tagId);
                    if (tag == null) throw new ArgumentException($"Tag with ID '{tagId}' not found.");
                }
            }

            var product = new Product
            {
                Id = Guid.NewGuid(), ShopId = shopId.Value, CategoryId = dto.CategoryId,
                Name = dto.Name, Price = dto.Price, Description = dto.Description ?? "",
                ImageUrl = dto.ImageUrl ?? "", Stock = dto.Stock, IsActive = true,
                CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
            };
            await _uow.Products.AddAsync(product);

            if (dto.TagIds != null)
                foreach (var tagId in dto.TagIds)
                    await _uow.ProductTags.AddAsync(new ProductTag { Id = Guid.NewGuid(), ProductId = product.Id, TagId = tagId });

            await _uow.SaveChangesAsync();
            return new { Message = "Product created successfully.", ProductId = product.Id, product.Name, product.Price, product.Stock };
        }

        public async Task<string> UpdateProductAsync(Guid userId, Guid productId, UpdateProductDto dto)
        {
            var shopId = await GetShopIdForUserAsync(userId);
            if (shopId == null) throw new ArgumentException("Shop not found.");
            var product = await _uow.Products.GetByIdWithTagsAsync(productId);
            if (product == null || product.ShopId != shopId) throw new ArgumentException("Product not found.");

            // Validate
            if (dto.Name != null && string.IsNullOrWhiteSpace(dto.Name)) throw new ArgumentException("Product name cannot be empty.");
            if (dto.Price.HasValue && dto.Price.Value <= 0) throw new ArgumentException("Price must be greater than 0.");
            if (dto.Stock.HasValue && dto.Stock.Value < 0) throw new ArgumentException("Stock cannot be negative.");

            if (dto.CategoryId.HasValue)
            {
                var category = await _uow.Categories.GetByIdAsync(dto.CategoryId.Value);
                if (category == null) throw new ArgumentException($"Category not found.");
            }

            if (dto.Name != null) product.Name = dto.Name;
            if (dto.Price.HasValue) product.Price = dto.Price.Value;
            if (dto.Description != null) product.Description = dto.Description;
            if (dto.ImageUrl != null) product.ImageUrl = dto.ImageUrl;
            if (dto.CategoryId.HasValue) product.CategoryId = dto.CategoryId;
            if (dto.Stock.HasValue) product.Stock = dto.Stock.Value;
            if (dto.IsActive.HasValue) product.IsActive = dto.IsActive.Value;
            product.UpdatedAt = DateTime.UtcNow;
            if (dto.TagIds != null)
            {
                _uow.ProductTags.RemoveRange(product.ProductTags);
                foreach (var tagId in dto.TagIds)
                    await _uow.ProductTags.AddAsync(new ProductTag { Id = Guid.NewGuid(), ProductId = product.Id, TagId = tagId });
            }
            await _uow.SaveChangesAsync();
            return "Product updated successfully.";
        }

        public async Task<string> DeleteProductAsync(Guid userId, Guid productId)
        {
            var shopId = await GetShopIdForUserAsync(userId);
            if (shopId == null) throw new ArgumentException("Shop not found.");
            var product = await _uow.Products.FirstOrDefaultAsync(p => p.Id == productId && p.ShopId == shopId);
            if (product == null) throw new ArgumentException("Product not found.");
            _uow.Products.Remove(product);
            await _uow.SaveChangesAsync();
            return "Product deleted successfully.";
        }

        public async Task<List<ProductDetailDto>> GetProductsByShopAsync(Guid shopId)
        {
            var shop = await _uow.Shops.GetByIdAsync(shopId);
            if (shop == null || !shop.IsActive) throw new ArgumentException("Shop not found.");
            var products = await _uow.Products.FindAsync(p => p.ShopId == shopId && p.IsActive);
            var result = new List<ProductDetailDto>();
            foreach (var p in products.OrderByDescending(p => p.CreatedAt))
            {
                var full = await _uow.Products.GetByIdWithDetailsAsync(p.Id);
                if (full != null) result.Add(MapProduct(full));
            }
            return result;
        }

        private static ProductDetailDto MapProduct(Product p) => new()
        { Id = p.Id, ShopId = p.ShopId, ShopName = p.Shop.ShopName, CategoryId = p.CategoryId, CategoryName = p.Category?.Name, Name = p.Name, Price = p.Price, Description = p.Description, ImageUrl = p.ImageUrl, Stock = p.Stock, IsActive = p.IsActive, AverageRating = p.AverageRating, ReviewCount = p.ReviewCount, Tags = p.ProductTags.Select(pt => pt.Tag.Name).ToList(), CreatedAt = p.CreatedAt };
    }
}
