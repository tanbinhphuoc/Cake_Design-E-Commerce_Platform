using Application.DTOs;
using Application.Services;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Cake_Design_E_Commerce_Platform.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CouponController : ControllerBase
    {
        private readonly ICouponService _couponService;
        public CouponController(ICouponService couponService) { _couponService = couponService; }

        private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        private string GetRole() => User.FindFirstValue(ClaimTypes.Role) ?? "";

        // ===== Shop Owner =====

        [HttpPost("shop")]
        public async Task<IActionResult> CreateShopCoupon([FromBody] CreateCouponDto dto)
        {
            if (GetRole() != "ShopOwner") return Forbid();
            try
            {
                var result = await _couponService.CreateShopCouponAsync(GetUserId(), dto);
                return Ok(result);
            }
            catch (Exception ex) { return BadRequest(new { ex.Message }); }
        }

        [HttpGet("shop")]
        public async Task<IActionResult> GetShopCoupons()
        {
            if (GetRole() != "ShopOwner") return Forbid();
            try
            {
                var coupons = await _couponService.GetShopCouponsAsync(GetUserId());
                return Ok(coupons);
            }
            catch (Exception ex) { return BadRequest(new { ex.Message }); }
        }

        [HttpPut("shop/{couponId}")]
        public async Task<IActionResult> UpdateShopCoupon(Guid couponId, [FromBody] UpdateCouponDto dto)
        {
            if (GetRole() != "ShopOwner") return Forbid();
            try
            {
                var result = await _couponService.UpdateCouponAsync(GetUserId(), couponId, dto);
                return Ok(new { Message = result });
            }
            catch (Exception ex) { return BadRequest(new { ex.Message }); }
        }

        [HttpDelete("shop/{couponId}")]
        public async Task<IActionResult> DeactivateShopCoupon(Guid couponId)
        {
            if (GetRole() != "ShopOwner") return Forbid();
            try
            {
                var result = await _couponService.DeactivateCouponAsync(GetUserId(), couponId);
                return Ok(new { Message = result });
            }
            catch (Exception ex) { return BadRequest(new { ex.Message }); }
        }

        // ===== Admin =====

        [HttpPost("system")]
        public async Task<IActionResult> CreateSystemCoupon([FromBody] CreateCouponDto dto)
        {
            if (GetRole() != "Admin") return Forbid();
            try
            {
                var result = await _couponService.CreateSystemCouponAsync(dto);
                return Ok(result);
            }
            catch (Exception ex) { return BadRequest(new { ex.Message }); }
        }

        [HttpGet("system")]
        public async Task<IActionResult> GetSystemCoupons()
        {
            if (GetRole() != "Admin") return Forbid();
            try
            {
                var coupons = await _couponService.GetSystemCouponsAsync();
                return Ok(coupons);
            }
            catch (Exception ex) { return BadRequest(new { ex.Message }); }
        }

        // ===== Customer =====

        [HttpPost("validate")]
        public async Task<IActionResult> ValidateCoupon([FromBody] ValidateCouponRequestDto dto)
        {
            try
            {
                var result = await _couponService.ValidateCouponAsync(dto.Code, GetUserId(), dto.OrderAmount, dto.ShopId);
                return Ok(result);
            }
            catch (Exception ex) { return BadRequest(new { ex.Message }); }
        }
    }

    // DTO for validate request
    public class ValidateCouponRequestDto
    {
        public string Code { get; set; } = string.Empty;
        public decimal OrderAmount { get; set; }
        public Guid? ShopId { get; set; }
    }
}
