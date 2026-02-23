using Application.DTOs;
using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Cake_Design_E_Commerce_Platform.Controllers
{
    [ApiController]
    [Route("api/cart")]
    [Authorize]
    public class CartController : ControllerBase
    {
        private readonly ICartService _cartService;
        public CartController(ICartService cartService) { _cartService = cartService; }

        [HttpGet]
        public async Task<IActionResult> GetCart()
        {
            var userId = GetUserId(); if (userId == null) return Unauthorized();
            return Ok(await _cartService.GetCartAsync(userId.Value));
        }

        [HttpPost("add")]
        public async Task<IActionResult> AddToCart([FromBody] AddToCartDto dto)
        {
            var userId = GetUserId(); if (userId == null) return Unauthorized();
            try { return Ok(new { Message = await _cartService.AddToCartAsync(userId.Value, dto) }); }
            catch (ArgumentException ex) { return BadRequest(new { ex.Message }); }
        }

        [HttpPut("update-item")]
        public async Task<IActionResult> UpdateCartItem([FromBody] UpdateCartItemDto dto)
        {
            var userId = GetUserId(); if (userId == null) return Unauthorized();
            try { return Ok(new { Message = await _cartService.UpdateCartItemAsync(userId.Value, dto) }); }
            catch (ArgumentException ex) { return BadRequest(new { ex.Message }); }
        }

        [HttpDelete("remove-item/{productId:guid}")]
        public async Task<IActionResult> RemoveCartItem(Guid productId)
        {
            var userId = GetUserId(); if (userId == null) return Unauthorized();
            try { return Ok(new { Message = await _cartService.RemoveCartItemAsync(userId.Value, productId) }); }
            catch (ArgumentException ex) { return BadRequest(new { ex.Message }); }
        }

        [HttpDelete("clear")]
        public async Task<IActionResult> ClearCart()
        {
            var userId = GetUserId(); if (userId == null) return Unauthorized();
            try { return Ok(new { Message = await _cartService.ClearCartAsync(userId.Value) }); }
            catch (ArgumentException ex) { return BadRequest(new { ex.Message }); }
        }

        private Guid? GetUserId()
        {
            var c = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return !string.IsNullOrEmpty(c) && Guid.TryParse(c, out var id) ? id : null;
        }
    }
}
