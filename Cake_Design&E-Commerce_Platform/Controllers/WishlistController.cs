using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Cake_Design_E_Commerce_Platform.Controllers
{
    [ApiController]
    [Route("api/wishlist")]
    [Authorize]
    public class WishlistController : ControllerBase
    {
        private readonly IWishlistService _wishlistService;
        public WishlistController(IWishlistService wishlistService) { _wishlistService = wishlistService; }

        [HttpGet]
        public async Task<IActionResult> GetWishlist()
        {
            var userId = GetUserId(); if (userId == null) return Unauthorized();
            return Ok(await _wishlistService.GetWishlistAsync(userId.Value));
        }

        [HttpPost("{productId:guid}")]
        public async Task<IActionResult> AddToWishlist(Guid productId)
        {
            var userId = GetUserId(); if (userId == null) return Unauthorized();
            try { return Ok(new { Message = await _wishlistService.AddToWishlistAsync(userId.Value, productId) }); }
            catch (ArgumentException ex) { return NotFound(new { ex.Message }); }
            catch (InvalidOperationException ex) { return BadRequest(new { ex.Message }); }
        }

        [HttpDelete("{productId:guid}")]
        public async Task<IActionResult> RemoveFromWishlist(Guid productId)
        {
            var userId = GetUserId(); if (userId == null) return Unauthorized();
            try { return Ok(new { Message = await _wishlistService.RemoveFromWishlistAsync(userId.Value, productId) }); }
            catch (ArgumentException ex) { return NotFound(new { ex.Message }); }
        }

        private Guid? GetUserId()
        {
            var c = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return !string.IsNullOrEmpty(c) && Guid.TryParse(c, out var id) ? id : null;
        }
    }
}
