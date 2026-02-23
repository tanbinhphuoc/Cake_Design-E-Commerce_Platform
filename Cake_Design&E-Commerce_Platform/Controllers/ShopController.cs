using Application.DTOs;
using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Cake_Design_E_Commerce_Platform.Controllers
{
    [ApiController]
    [Route("api")]
    public class ShopController : ControllerBase
    {
        private readonly IShopService _shopService;
        public ShopController(IShopService shopService) { _shopService = shopService; }

        [HttpPost("shop/request"), Authorize(Roles = "Customer")]
        public async Task<IActionResult> RequestShop([FromBody] UpdateShopDto dto)
        {
            var userId = GetUserId(); if (userId == null) return Unauthorized();
            try { return Ok(await _shopService.RequestShopAsync(userId.Value, dto)); }
            catch (InvalidOperationException ex) { return BadRequest(new { ex.Message }); }
        }

        [HttpPost("admin/shop/approve/{userId:guid}"), Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> ApproveShop(Guid userId)
        {
            try { return Ok(new { Message = await _shopService.ApproveShopAsync(userId) }); }
            catch (Exception ex) { return BadRequest(new { ex.Message }); }
        }

        [HttpGet("shops/me"), Authorize(Roles = "ShopOwner")]
        public async Task<IActionResult> GetMyShop()
        {
            var userId = GetUserId(); if (userId == null) return Unauthorized();
            var shop = await _shopService.GetMyShopAsync(userId.Value);
            return shop != null ? Ok(shop) : NotFound(new { Message = "Shop not found." });
        }

        [HttpPut("shops/me"), Authorize(Roles = "ShopOwner")]
        public async Task<IActionResult> UpdateMyShop([FromBody] UpdateShopDto dto)
        {
            var userId = GetUserId(); if (userId == null) return Unauthorized();
            try { return Ok(new { Message = await _shopService.UpdateMyShopAsync(userId.Value, dto) }); }
            catch (ArgumentException ex) { return NotFound(new { ex.Message }); }
        }

        [HttpGet("shops/{shopId:guid}"), AllowAnonymous]
        public async Task<IActionResult> GetShopById(Guid shopId)
        {
            var shop = await _shopService.GetShopByIdAsync(shopId);
            return shop != null ? Ok(shop) : NotFound(new { Message = "Shop not found." });
        }

        // Staff
        [HttpPost("shops/me/staff"), Authorize(Roles = "ShopOwner")]
        public async Task<IActionResult> AddStaff([FromBody] AddStaffDto dto)
        {
            var userId = GetUserId(); if (userId == null) return Unauthorized();
            try { var id = await _shopService.AddStaffAsync(userId.Value, dto); return Ok(new { Message = "Staff added.", StaffId = id }); }
            catch (Exception ex) { return BadRequest(new { ex.Message }); }
        }

        [HttpGet("shops/me/staff"), Authorize(Roles = "ShopOwner")]
        public async Task<IActionResult> GetStaff()
        {
            var userId = GetUserId(); if (userId == null) return Unauthorized();
            try { return Ok(await _shopService.GetStaffAsync(userId.Value)); }
            catch (ArgumentException ex) { return NotFound(new { ex.Message }); }
        }

        [HttpDelete("shops/me/staff/{staffId:guid}"), Authorize(Roles = "ShopOwner")]
        public async Task<IActionResult> RemoveStaff(Guid staffId)
        {
            var userId = GetUserId(); if (userId == null) return Unauthorized();
            try { return Ok(new { Message = await _shopService.RemoveStaffAsync(userId.Value, staffId) }); }
            catch (ArgumentException ex) { return NotFound(new { ex.Message }); }
        }

        // Products
        [HttpGet("shops/me/products"), Authorize(Roles = "ShopOwner,Staff")]
        public async Task<IActionResult> GetMyProducts()
        {
            var userId = GetUserId(); if (userId == null) return Unauthorized();
            try { return Ok(await _shopService.GetMyProductsAsync(userId.Value)); }
            catch (ArgumentException ex) { return NotFound(new { ex.Message }); }
        }

        [HttpPost("shops/me/products"), Authorize(Roles = "ShopOwner,Staff")]
        public async Task<IActionResult> CreateProduct([FromBody] CreateProductExtendedDto dto)
        {
            var userId = GetUserId(); if (userId == null) return Unauthorized();
            try { return Ok(await _shopService.CreateProductAsync(userId.Value, dto)); }
            catch (ArgumentException ex) { return BadRequest(new { ex.Message }); }
            catch (InvalidOperationException ex) { return StatusCode(403, new { ex.Message }); }
        }

        [HttpPut("shops/me/products/{productId:guid}"), Authorize(Roles = "ShopOwner,Staff")]
        public async Task<IActionResult> UpdateProduct(Guid productId, [FromBody] UpdateProductDto dto)
        {
            var userId = GetUserId(); if (userId == null) return Unauthorized();
            try { return Ok(new { Message = await _shopService.UpdateProductAsync(userId.Value, productId, dto) }); }
            catch (ArgumentException ex) { return NotFound(new { ex.Message }); }
        }

        [HttpDelete("shops/me/products/{productId:guid}"), Authorize(Roles = "ShopOwner,Staff")]
        public async Task<IActionResult> DeleteProduct(Guid productId)
        {
            var userId = GetUserId(); if (userId == null) return Unauthorized();
            try { return Ok(new { Message = await _shopService.DeleteProductAsync(userId.Value, productId) }); }
            catch (ArgumentException ex) { return NotFound(new { ex.Message }); }
        }

        [HttpGet("shops/{shopId:guid}/products"), AllowAnonymous]
        public async Task<IActionResult> GetProductsByShop(Guid shopId)
        {
            try { return Ok(await _shopService.GetProductsByShopAsync(shopId)); }
            catch (ArgumentException ex) { return NotFound(new { ex.Message }); }
        }

        private Guid? GetUserId()
        {
            var c = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return !string.IsNullOrEmpty(c) && Guid.TryParse(c, out var id) ? id : null;
        }
    }
}
