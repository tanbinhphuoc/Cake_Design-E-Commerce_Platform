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
    [Route("api/cart")]
    [Authorize]
    public class CartController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CartController(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Get the current user's cart with items.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetCart()
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var cart = await _context.Carts
                .Include(c => c.Items)
                    .ThenInclude(ci => ci.Product)
                        .ThenInclude(p => p.Shop)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null)
            {
                return Ok(new { Items = new List<object>(), TotalPrice = 0m });
            }

            var cartResponse = new
            {
                CartId = cart.Id,
                Items = cart.Items.Select(ci => new
                {
                    ci.Id,
                    ci.ProductId,
                    ProductName = ci.Product.Name,
                    ProductPrice = ci.Product.Price,
                    ProductImageUrl = ci.Product.ImageUrl,
                    ShopId = ci.Product.ShopId,
                    ShopName = ci.Product.Shop.ShopName,
                    ci.Quantity,
                    Subtotal = ci.Product.Price * ci.Quantity
                }),
                TotalPrice = cart.Items.Sum(ci => ci.Product.Price * ci.Quantity)
            };

            return Ok(cartResponse);
        }

        /// <summary>
        /// Add product to cart (or increase quantity if already in cart).
        /// </summary>
        [HttpPost("add")]
        public async Task<IActionResult> AddToCart([FromBody] AddToCartDto dto)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            // Validate product exists
            var product = await _context.Products.FindAsync(dto.ProductId);
            if (product == null)
                return BadRequest(new { Message = "Product not found." });

            if (!product.IsActive)
                return BadRequest(new { Message = "Product is not available." });

            if (dto.Quantity <= 0)
                return BadRequest(new { Message = "Quantity must be greater than 0." });

            if (product.Stock < dto.Quantity)
                return BadRequest(new { Message = $"Insufficient stock. Available: {product.Stock}" });

            // Get or create cart
            var cart = await _context.Carts
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null)
            {
                cart = new Cart
                {
                    Id = Guid.NewGuid(),
                    UserId = userId.Value
                };
                _context.Carts.Add(cart);
                await _context.SaveChangesAsync();
            }

            // Check if item already in cart
            var existingItem = cart.Items.FirstOrDefault(ci => ci.ProductId == dto.ProductId);
            if (existingItem != null)
            {
                existingItem.Quantity += dto.Quantity;
            }
            else
            {
                var cartItem = new CartItem
                {
                    Id = Guid.NewGuid(),
                    CartId = cart.Id,
                    ProductId = dto.ProductId,
                    Quantity = dto.Quantity
                };
                _context.CartItems.Add(cartItem);
            }

            await _context.SaveChangesAsync();

            return Ok(new { Message = "Product added to cart successfully." });
        }

        /// <summary>
        /// Update cart item quantity.
        /// </summary>
        [HttpPut("update-item")]
        public async Task<IActionResult> UpdateCartItem([FromBody] UpdateCartItemDto dto)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var cart = await _context.Carts
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null)
                return BadRequest(new { Message = "Cart not found." });

            var cartItem = cart.Items.FirstOrDefault(ci => ci.ProductId == dto.ProductId);
            if (cartItem == null)
                return BadRequest(new { Message = "Product not in cart." });

            if (dto.Quantity <= 0)
            {
                _context.CartItems.Remove(cartItem);
            }
            else
            {
                var product = await _context.Products.FindAsync(dto.ProductId);
                if (product != null && product.Stock < dto.Quantity)
                    return BadRequest(new { Message = $"Insufficient stock. Available: {product.Stock}" });

                cartItem.Quantity = dto.Quantity;
            }

            await _context.SaveChangesAsync();

            return Ok(new { Message = "Cart item updated." });
        }

        /// <summary>
        /// Remove item from cart.
        /// </summary>
        [HttpDelete("remove-item/{productId:guid}")]
        public async Task<IActionResult> RemoveCartItem(Guid productId)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var cart = await _context.Carts
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null)
                return BadRequest(new { Message = "Cart not found." });

            var cartItem = cart.Items.FirstOrDefault(ci => ci.ProductId == productId);
            if (cartItem == null)
                return BadRequest(new { Message = "Product not in cart." });

            _context.CartItems.Remove(cartItem);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Item removed from cart." });
        }

        /// <summary>
        /// Clear entire cart.
        /// </summary>
        [HttpDelete("clear")]
        public async Task<IActionResult> ClearCart()
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var cart = await _context.Carts
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null)
                return BadRequest(new { Message = "Cart not found." });

            _context.CartItems.RemoveRange(cart.Items);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Cart cleared." });
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
