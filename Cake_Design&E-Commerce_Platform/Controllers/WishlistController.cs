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
    [Route("api/wishlist")]
    [Authorize]
    public class WishlistController : ControllerBase
    {
        private readonly AppDbContext _context;

        public WishlistController(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Get current user's wishlist.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetWishlist()
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var items = await _context.WishlistItems
                .Include(w => w.Product)
                .Where(w => w.UserId == userId)
                .OrderByDescending(w => w.CreatedAt)
                .Select(w => new WishlistItemDto
                {
                    ProductId = w.ProductId,
                    ProductName = w.Product.Name,
                    Price = w.Product.Price,
                    ImageUrl = w.Product.ImageUrl,
                    AddedAt = w.CreatedAt
                })
                .ToListAsync();

            return Ok(items);
        }

        /// <summary>
        /// Add product to wishlist.
        /// </summary>
        [HttpPost("{productId:guid}")]
        public async Task<IActionResult> AddToWishlist(Guid productId)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var product = await _context.Products.FindAsync(productId);
            if (product == null)
                return NotFound(new { Message = "Product not found." });

            var existing = await _context.WishlistItems
                .FirstOrDefaultAsync(w => w.UserId == userId && w.ProductId == productId);

            if (existing != null)
                return BadRequest(new { Message = "Product already in wishlist." });

            var item = new WishlistItem
            {
                Id = Guid.NewGuid(),
                UserId = userId.Value,
                ProductId = productId,
                CreatedAt = DateTime.UtcNow
            };

            _context.WishlistItems.Add(item);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Product added to wishlist." });
        }

        /// <summary>
        /// Remove product from wishlist.
        /// </summary>
        [HttpDelete("{productId:guid}")]
        public async Task<IActionResult> RemoveFromWishlist(Guid productId)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var item = await _context.WishlistItems
                .FirstOrDefaultAsync(w => w.UserId == userId && w.ProductId == productId);

            if (item == null)
                return NotFound(new { Message = "Product not in wishlist." });

            _context.WishlistItems.Remove(item);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Product removed from wishlist." });
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
