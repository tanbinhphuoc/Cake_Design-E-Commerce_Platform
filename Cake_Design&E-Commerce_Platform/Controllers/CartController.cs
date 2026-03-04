using Application.DTOs;
using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Cake_Design_E_Commerce_Platform.Controllers
{
    /// <summary>
    /// Quản lý giỏ hàng của người dùng
    /// </summary>
    [ApiController]
    [Route("api/cart")]
    [Authorize]
    public class CartController : ControllerBase
    {
        private readonly ICartService _cartService;
        public CartController(ICartService cartService) { _cartService = cartService; }

        /// <summary>
        /// Lấy thông tin giỏ hàng hiện tại của người dùng
        /// </summary>
        /// <response code="200">Trả về chi tiết giỏ hàng và danh sách sản phẩm</response>
        /// <response code="401">Không có quyền truy cập</response>
        [HttpGet]
        public async Task<IActionResult> GetCart()
        {
            var userId = GetUserId(); if (userId == null) return Unauthorized();
            return Ok(await _cartService.GetCartAsync(userId.Value));
        }

        /// <summary>
        /// Thêm sản phẩm vào giỏ hàng
        /// </summary>
        /// <param name="dto">Thông tin sản phẩm và số lượng cần thêm</param>
        /// <response code="200">Thêm thành công</response>
        /// <response code="400">Lỗi dữ liệu đầu vào hoặc hết hàng</response>
        [HttpPost("add")]
        public async Task<IActionResult> AddToCart([FromBody] AddToCartDto dto)
        {
            var userId = GetUserId(); if (userId == null) return Unauthorized();
            try { return Ok(new { Message = await _cartService.AddToCartAsync(userId.Value, dto) }); }
            catch (ArgumentException ex) { return BadRequest(new { ex.Message }); }
        }

        /// <summary>
        /// Cập nhật số lượng sản phẩm trong giỏ hàng
        /// </summary>
        /// <param name="dto">Thông tin cập nhật số lượng</param>
        /// <response code="200">Cập nhật thành công</response>
        /// <response code="400">Lỗi số lượng không hợp lệ</response>
        [HttpPut("update-item")]
        public async Task<IActionResult> UpdateCartItem([FromBody] UpdateCartItemDto dto)
        {
            var userId = GetUserId(); if (userId == null) return Unauthorized();
            try { return Ok(new { Message = await _cartService.UpdateCartItemAsync(userId.Value, dto) }); }
            catch (ArgumentException ex) { return BadRequest(new { ex.Message }); }
        }

        /// <summary>
        /// Xóa một sản phẩm khỏi giỏ hàng
        /// </summary>
        /// <param name="productId">ID của sản phẩm cần xóa</param>
        /// <response code="200">Xóa thành công</response>
        [HttpDelete("remove-item/{productId:guid}")]
        public async Task<IActionResult> RemoveCartItem(Guid productId)
        {
            var userId = GetUserId(); if (userId == null) return Unauthorized();
            try { return Ok(new { Message = await _cartService.RemoveCartItemAsync(userId.Value, productId) }); }
            catch (ArgumentException ex) { return BadRequest(new { ex.Message }); }
        }

        /// <summary>
        /// Xóa toàn bộ sản phẩm trong giỏ hàng (Clear Cart)
        /// </summary>
        /// <response code="200">Xóa thành công</response>
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
