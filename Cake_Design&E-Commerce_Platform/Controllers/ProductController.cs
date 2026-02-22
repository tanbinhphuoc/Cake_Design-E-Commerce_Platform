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
    public class ProductController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ProductController(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Get all active products (public).
        /// </summary>
        [HttpGet("products")]
        [AllowAnonymous]
        public async Task<IActionResult> GetAllProducts()
        {
            var products = await _context.Products
                .Include(p => p.Shop)
                .Include(p => p.Category)
                .Include(p => p.ProductTags).ThenInclude(pt => pt.Tag)
                .Where(p => p.IsActive)
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
        /// Get product detail by ID (public).
        /// </summary>
        [HttpGet("products/{id:guid}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetProductById(Guid id)
        {
            var product = await _context.Products
                .Include(p => p.Shop)
                .Include(p => p.Category)
                .Include(p => p.ProductTags).ThenInclude(pt => pt.Tag)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
                return NotFound(new { Message = "Product not found." });

            return Ok(new ProductDetailDto
            {
                Id = product.Id,
                ShopId = product.ShopId,
                ShopName = product.Shop.ShopName,
                CategoryId = product.CategoryId,
                CategoryName = product.Category?.Name,
                Name = product.Name,
                Price = product.Price,
                Description = product.Description,
                ImageUrl = product.ImageUrl,
                Stock = product.Stock,
                IsActive = product.IsActive,
                AverageRating = product.AverageRating,
                ReviewCount = product.ReviewCount,
                Tags = product.ProductTags.Select(pt => pt.Tag.Name).ToList(),
                CreatedAt = product.CreatedAt
            });
        }

        /// <summary>
        /// Search products with filters and pagination.
        /// GET /api/products/search?keyword=&categoryId=&minPrice=&maxPrice=&sort=&shopId=&tagId=&page=&pageSize=
        /// </summary>
        [HttpGet("products/search")]
        [AllowAnonymous]
        public async Task<IActionResult> SearchProducts([FromQuery] ProductSearchDto search)
        {
            var query = _context.Products
                .Include(p => p.Shop)
                .Include(p => p.Category)
                .Include(p => p.ProductTags).ThenInclude(pt => pt.Tag)
                .Where(p => p.IsActive)
                .AsQueryable();

            // Keyword search
            if (!string.IsNullOrWhiteSpace(search.Keyword))
            {
                var keyword = search.Keyword.ToLower();
                query = query.Where(p => p.Name.ToLower().Contains(keyword) ||
                                         p.Description.ToLower().Contains(keyword));
            }

            // Category filter
            if (search.CategoryId.HasValue)
                query = query.Where(p => p.CategoryId == search.CategoryId);

            // Shop filter
            if (search.ShopId.HasValue)
                query = query.Where(p => p.ShopId == search.ShopId);

            // Tag filter
            if (search.TagId.HasValue)
                query = query.Where(p => p.ProductTags.Any(pt => pt.TagId == search.TagId));

            // Price range filter
            if (search.MinPrice.HasValue)
                query = query.Where(p => p.Price >= search.MinPrice);

            if (search.MaxPrice.HasValue)
                query = query.Where(p => p.Price <= search.MaxPrice);

            // Sort
            query = search.Sort?.ToLower() switch
            {
                "price_asc" => query.OrderBy(p => p.Price),
                "price_desc" => query.OrderByDescending(p => p.Price),
                "rating" => query.OrderByDescending(p => p.AverageRating),
                "popular" => query.OrderByDescending(p => p.ReviewCount),
                _ => query.OrderByDescending(p => p.CreatedAt) // "newest" or default
            };

            // Pagination
            var totalCount = await query.CountAsync();
            var page = Math.Max(1, search.Page);
            var pageSize = Math.Clamp(search.PageSize, 1, 100);

            var products = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
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

            return Ok(new PaginatedResultDto<ProductDetailDto>
            {
                Items = products,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            });
        }

        // ===== Reviews =====

        /// <summary>
        /// Get reviews for a product.
        /// </summary>
        [HttpGet("products/{id:guid}/reviews")]
        [AllowAnonymous]
        public async Task<IActionResult> GetReviews(Guid id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
                return NotFound(new { Message = "Product not found." });

            var reviews = await _context.Reviews
                .Include(r => r.User)
                .Where(r => r.ProductId == id)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new ReviewDto
                {
                    Id = r.Id,
                    UserId = r.UserId,
                    Username = r.User.Username,
                    AvatarUrl = r.User.AvatarUrl,
                    Rating = r.Rating,
                    Comment = r.Comment,
                    CreatedAt = r.CreatedAt
                })
                .ToListAsync();

            return Ok(reviews);
        }

        /// <summary>
        /// Create a review for a product (only users who purchased).
        /// </summary>
        [HttpPost("products/{id:guid}/reviews")]
        [Authorize]
        public async Task<IActionResult> CreateReview(Guid id, [FromBody] CreateReviewDto dto)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var product = await _context.Products.FindAsync(id);
            if (product == null)
                return NotFound(new { Message = "Product not found." });

            // Check if user already reviewed
            var existingReview = await _context.Reviews
                .FirstOrDefaultAsync(r => r.ProductId == id && r.UserId == userId);

            if (existingReview != null)
                return BadRequest(new { Message = "You have already reviewed this product." });

            if (dto.Rating < 1 || dto.Rating > 5)
                return BadRequest(new { Message = "Rating must be between 1 and 5." });

            var review = new Review
            {
                Id = Guid.NewGuid(),
                ProductId = id,
                UserId = userId.Value,
                Rating = dto.Rating,
                Comment = dto.Comment ?? string.Empty,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Reviews.Add(review);

            // Update product average rating
            var allRatings = await _context.Reviews
                .Where(r => r.ProductId == id)
                .Select(r => r.Rating)
                .ToListAsync();

            allRatings.Add(dto.Rating);
            product.AverageRating = allRatings.Average();
            product.ReviewCount = allRatings.Count;
            product.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { Message = "Review created successfully.", ReviewId = review.Id });
        }

        private Guid? GetUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                return null;
            return userId;
        }
    }
}
