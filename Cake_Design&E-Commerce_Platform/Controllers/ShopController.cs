using Application.DTOs;
using Domain.Entities;
using Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Cake_Design_E_Commerce_Platform.Controllers
{
    [ApiController]
    [Route("api")]
    public class ShopController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ShopController(AppDbContext context)
        {
            _context = context;
        }

        // ===== Shop Registration =====

        /// <summary>
        /// User requests to become a shop owner. Sets Role = "ShopOwner" pending approval.
        /// </summary>
        [HttpPost("shop/request")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> RequestShop([FromBody] UpdateShopDto dto)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var account = await _context.Accounts.Include(a => a.Shop).FirstOrDefaultAsync(a => a.Id == userId);
            if (account == null) return Unauthorized();

            if (account.Role == "ShopOwner" && account.Shop != null)
            {
                return BadRequest(new { Message = "You are already a shop owner." });
            }

            if (account.Role != "Customer")
            {
                return BadRequest(new { Message = "Only customers can request to become a shop owner." });
            }

            // Create shop entity
            var shop = new Shop
            {
                Id = Guid.NewGuid(),
                OwnerId = account.Id,
                ShopName = dto.ShopName ?? account.Username + "'s Shop",
                Description = dto.Description ?? string.Empty,
                AvatarUrl = dto.AvatarUrl ?? string.Empty,
                BannerUrl = dto.BannerUrl ?? string.Empty,
                Address = dto.Address ?? string.Empty,
                Phone = dto.Phone ?? string.Empty,
                IsActive = false, // Not active until approved
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Shops.Add(shop);

            account.Role = "ShopOwner";
            account.IsApproved = false;
            account.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { Message = "Shop request submitted. Awaiting admin approval.", ShopId = shop.Id });
        }

        /// <summary>
        /// Admin approves a shop request.
        /// </summary>
        [HttpPost("admin/shop/approve/{userId:guid}")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> ApproveShop(Guid userId)
        {
            var account = await _context.Accounts.Include(a => a.Shop).FirstOrDefaultAsync(a => a.Id == userId);
            if (account == null)
                return BadRequest(new { Message = "User not found." });

            if (account.Role != "ShopOwner" || account.Shop == null)
                return BadRequest(new { Message = "This user does not have a pending shop request." });

            if (account.IsApproved)
                return BadRequest(new { Message = "Shop is already approved." });

            account.IsApproved = true;
            account.Shop.IsActive = true;
            account.Shop.UpdatedAt = DateTime.UtcNow;
            account.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { Message = $"Shop approved successfully for user '{account.Username}'." });
        }

        // ===== Shop Owner Management =====

        /// <summary>
        /// Get current shop owner's shop info.
        /// </summary>
        [HttpGet("shops/me")]
        [Authorize(Roles = "ShopOwner")]
        public async Task<IActionResult> GetMyShop()
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var shop = await _context.Shops
                .Include(s => s.Products)
                .FirstOrDefaultAsync(s => s.OwnerId == userId);

            if (shop == null)
                return NotFound(new { Message = "Shop not found." });

            return Ok(new ShopProfileDto
            {
                Id = shop.Id,
                OwnerId = shop.OwnerId,
                ShopName = shop.ShopName,
                Description = shop.Description,
                AvatarUrl = shop.AvatarUrl,
                BannerUrl = shop.BannerUrl,
                Address = shop.Address,
                Phone = shop.Phone,
                IsActive = shop.IsActive,
                CreatedAt = shop.CreatedAt
            });
        }

        /// <summary>
        /// Update current shop owner's shop info.
        /// </summary>
        [HttpPut("shops/me")]
        [Authorize(Roles = "ShopOwner")]
        public async Task<IActionResult> UpdateMyShop([FromBody] UpdateShopDto dto)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var shop = await _context.Shops.FirstOrDefaultAsync(s => s.OwnerId == userId);
            if (shop == null)
                return NotFound(new { Message = "Shop not found." });

            if (dto.ShopName != null) shop.ShopName = dto.ShopName;
            if (dto.Description != null) shop.Description = dto.Description;
            if (dto.AvatarUrl != null) shop.AvatarUrl = dto.AvatarUrl;
            if (dto.BannerUrl != null) shop.BannerUrl = dto.BannerUrl;
            if (dto.Address != null) shop.Address = dto.Address;
            if (dto.Phone != null) shop.Phone = dto.Phone;
            shop.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { Message = "Shop updated successfully." });
        }

        /// <summary>
        /// Public - Get shop by ID.
        /// </summary>
        [HttpGet("shops/{shopId:guid}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetShopById(Guid shopId)
        {
            var shop = await _context.Shops
                .Include(s => s.Products)
                .FirstOrDefaultAsync(s => s.Id == shopId && s.IsActive);

            if (shop == null)
                return NotFound(new { Message = "Shop not found." });

            return Ok(new ShopPublicDto
            {
                Id = shop.Id,
                ShopName = shop.ShopName,
                Description = shop.Description,
                AvatarUrl = shop.AvatarUrl,
                BannerUrl = shop.BannerUrl,
                Address = shop.Address,
                ProductCount = shop.Products.Count(p => p.IsActive),
                CreatedAt = shop.CreatedAt
            });
        }

        // ===== Shop Staff Management =====

        /// <summary>
        /// Add staff to shop.
        /// </summary>
        [HttpPost("shops/me/staff")]
        [Authorize(Roles = "ShopOwner")]
        public async Task<IActionResult> AddStaff([FromBody] AddStaffDto dto)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var shop = await _context.Shops.FirstOrDefaultAsync(s => s.OwnerId == userId);
            if (shop == null)
                return NotFound(new { Message = "Shop not found." });

            var staffAccount = await _context.Accounts.FindAsync(dto.AccountId);
            if (staffAccount == null)
                return BadRequest(new { Message = "Account not found." });

            // Check if already staff
            var existing = await _context.ShopStaff
                .FirstOrDefaultAsync(ss => ss.ShopId == shop.Id && ss.AccountId == dto.AccountId);
            if (existing != null)
                return BadRequest(new { Message = "This account is already staff of your shop." });

            var staff = new ShopStaff
            {
                Id = Guid.NewGuid(),
                ShopId = shop.Id,
                AccountId = dto.AccountId,
                Role = dto.Role,
                CreatedAt = DateTime.UtcNow
            };

            // Update account role to Staff
            staffAccount.Role = "Staff";
            staffAccount.UpdatedAt = DateTime.UtcNow;

            _context.ShopStaff.Add(staff);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Staff added successfully.", StaffId = staff.Id });
        }

        /// <summary>
        /// Get all staff of current shop.
        /// </summary>
        [HttpGet("shops/me/staff")]
        [Authorize(Roles = "ShopOwner")]
        public async Task<IActionResult> GetStaff()
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var shop = await _context.Shops.FirstOrDefaultAsync(s => s.OwnerId == userId);
            if (shop == null)
                return NotFound(new { Message = "Shop not found." });

            var staff = await _context.ShopStaff
                .Include(ss => ss.Account)
                .Where(ss => ss.ShopId == shop.Id)
                .Select(ss => new StaffDto
                {
                    Id = ss.Id,
                    AccountId = ss.AccountId,
                    Username = ss.Account.Username,
                    Role = ss.Role,
                    CreatedAt = ss.CreatedAt
                })
                .ToListAsync();

            return Ok(staff);
        }

        /// <summary>
        /// Remove staff from shop.
        /// </summary>
        [HttpDelete("shops/me/staff/{staffId:guid}")]
        [Authorize(Roles = "ShopOwner")]
        public async Task<IActionResult> RemoveStaff(Guid staffId)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var shop = await _context.Shops.FirstOrDefaultAsync(s => s.OwnerId == userId);
            if (shop == null)
                return NotFound(new { Message = "Shop not found." });

            var staff = await _context.ShopStaff
                .FirstOrDefaultAsync(ss => ss.Id == staffId && ss.ShopId == shop.Id);

            if (staff == null)
                return NotFound(new { Message = "Staff not found." });

            // Restore account role to Customer
            var staffAccount = await _context.Accounts.FindAsync(staff.AccountId);
            if (staffAccount != null)
            {
                staffAccount.Role = "Customer";
                staffAccount.UpdatedAt = DateTime.UtcNow;
            }

            _context.ShopStaff.Remove(staff);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Staff removed successfully." });
        }

        // ===== Shop Products Management =====

        /// <summary>
        /// Get products of current shop (owner view).
        /// </summary>
        [HttpGet("shops/me/products")]
        [Authorize(Roles = "ShopOwner,Staff")]
        public async Task<IActionResult> GetMyProducts()
        {
            var shopId = await GetShopIdForUser();
            if (shopId == null) return NotFound(new { Message = "Shop not found." });

            var products = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.ProductTags).ThenInclude(pt => pt.Tag)
                .Where(p => p.ShopId == shopId)
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => new ProductDetailDto
                {
                    Id = p.Id,
                    ShopId = p.ShopId,
                    ShopName = p.Shop.ShopName,
                    CategoryId = p.CategoryId,
                    CategoryName = p.Category != null ? p.Category.Name : null,
                    Name = p.Name,
                    Price = p.Price,
                    Description = p.Description,
                    ImageUrl = p.ImageUrl,
                    Stock = p.Stock,
                    IsActive = p.IsActive,
                    AverageRating = p.AverageRating,
                    ReviewCount = p.ReviewCount,
                    Tags = p.ProductTags.Select(pt => pt.Tag.Name).ToList(),
                    CreatedAt = p.CreatedAt
                })
                .ToListAsync();

            return Ok(products);
        }

        /// <summary>
        /// Create product for current shop.
        /// </summary>
        [HttpPost("shops/me/products")]
        [Authorize(Roles = "ShopOwner,Staff")]
        public async Task<IActionResult> CreateProduct([FromBody] CreateProductExtendedDto dto)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var account = await _context.Accounts.Include(a => a.Shop).FirstOrDefaultAsync(a => a.Id == userId);
            if (account == null) return Unauthorized();

            Guid? shopId = await GetShopIdForUser();
            if (shopId == null) return NotFound(new { Message = "Shop not found." });

            if (!account.IsApproved)
                return StatusCode(403, new { Message = "Your shop has not been approved yet." });

            if (string.IsNullOrWhiteSpace(dto.Name))
                return BadRequest(new { Message = "Product name is required." });

            if (dto.Price <= 0)
                return BadRequest(new { Message = "Price must be greater than 0." });

            var product = new Product
            {
                Id = Guid.NewGuid(),
                ShopId = shopId.Value,
                CategoryId = dto.CategoryId,
                Name = dto.Name,
                Price = dto.Price,
                Description = dto.Description ?? string.Empty,
                ImageUrl = dto.ImageUrl ?? string.Empty,
                Stock = dto.Stock,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Products.Add(product);

            // Add tags
            if (dto.TagIds != null && dto.TagIds.Any())
            {
                foreach (var tagId in dto.TagIds)
                {
                    var tag = await _context.Tags.FindAsync(tagId);
                    if (tag != null)
                    {
                        _context.ProductTags.Add(new ProductTag
                        {
                            Id = Guid.NewGuid(),
                            ProductId = product.Id,
                            TagId = tagId
                        });
                    }
                }
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = "Product created successfully.",
                ProductId = product.Id,
                product.Name,
                product.Price,
                product.Stock
            });
        }

        /// <summary>
        /// Update product of current shop.
        /// </summary>
        [HttpPut("shops/me/products/{productId:guid}")]
        [Authorize(Roles = "ShopOwner,Staff")]
        public async Task<IActionResult> UpdateProduct(Guid productId, [FromBody] UpdateProductDto dto)
        {
            var shopId = await GetShopIdForUser();
            if (shopId == null) return NotFound(new { Message = "Shop not found." });

            var product = await _context.Products
                .Include(p => p.ProductTags)
                .FirstOrDefaultAsync(p => p.Id == productId && p.ShopId == shopId);

            if (product == null)
                return NotFound(new { Message = "Product not found." });

            if (dto.Name != null) product.Name = dto.Name;
            if (dto.Price.HasValue) product.Price = dto.Price.Value;
            if (dto.Description != null) product.Description = dto.Description;
            if (dto.ImageUrl != null) product.ImageUrl = dto.ImageUrl;
            if (dto.CategoryId.HasValue) product.CategoryId = dto.CategoryId;
            if (dto.Stock.HasValue) product.Stock = dto.Stock.Value;
            if (dto.IsActive.HasValue) product.IsActive = dto.IsActive.Value;
            product.UpdatedAt = DateTime.UtcNow;

            // Update tags if provided
            if (dto.TagIds != null)
            {
                _context.ProductTags.RemoveRange(product.ProductTags);
                foreach (var tagId in dto.TagIds)
                {
                    _context.ProductTags.Add(new ProductTag
                    {
                        Id = Guid.NewGuid(),
                        ProductId = product.Id,
                        TagId = tagId
                    });
                }
            }

            await _context.SaveChangesAsync();

            return Ok(new { Message = "Product updated successfully." });
        }

        /// <summary>
        /// Delete product of current shop.
        /// </summary>
        [HttpDelete("shops/me/products/{productId:guid}")]
        [Authorize(Roles = "ShopOwner,Staff")]
        public async Task<IActionResult> DeleteProduct(Guid productId)
        {
            var shopId = await GetShopIdForUser();
            if (shopId == null) return NotFound(new { Message = "Shop not found." });

            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == productId && p.ShopId == shopId);

            if (product == null)
                return NotFound(new { Message = "Product not found." });

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Product deleted successfully." });
        }

        /// <summary>
        /// Get products by shop ID (public).
        /// </summary>
        [HttpGet("shops/{shopId:guid}/products")]
        [AllowAnonymous]
        public async Task<IActionResult> GetProductsByShop(Guid shopId)
        {
            var shop = await _context.Shops.FindAsync(shopId);
            if (shop == null || !shop.IsActive)
                return NotFound(new { Message = "Shop not found." });

            var products = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.ProductTags).ThenInclude(pt => pt.Tag)
                .Where(p => p.ShopId == shopId && p.IsActive)
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => new ProductDetailDto
                {
                    Id = p.Id,
                    ShopId = p.ShopId,
                    ShopName = shop.ShopName,
                    CategoryId = p.CategoryId,
                    CategoryName = p.Category != null ? p.Category.Name : null,
                    Name = p.Name,
                    Price = p.Price,
                    Description = p.Description,
                    ImageUrl = p.ImageUrl,
                    Stock = p.Stock,
                    IsActive = p.IsActive,
                    AverageRating = p.AverageRating,
                    ReviewCount = p.ReviewCount,
                    Tags = p.ProductTags.Select(pt => pt.Tag.Name).ToList(),
                    CreatedAt = p.CreatedAt
                })
                .ToListAsync();

            return Ok(products);
        }

        private Guid? GetUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                return null;
            return userId;
        }

        private async Task<Guid?> GetShopIdForUser()
        {
            var userId = GetUserId();
            if (userId == null) return null;

            // Check if user is shop owner
            var shop = await _context.Shops.FirstOrDefaultAsync(s => s.OwnerId == userId);
            if (shop != null) return shop.Id;

            // Check if user is staff
            var staffRecord = await _context.ShopStaff.FirstOrDefaultAsync(ss => ss.AccountId == userId);
            if (staffRecord != null) return staffRecord.ShopId;

            return null;
        }
    }
}
