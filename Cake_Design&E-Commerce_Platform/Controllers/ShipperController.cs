using Application.DTOs;
using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Cake_Design_E_Commerce_Platform.Controllers
{
    /// <summary>
    /// Shipper - Quan ly giao hang
    /// </summary>
    [ApiController]
    [Route("api/shipper")]
    [Authorize(Roles = "Shipper")]
    public class ShipperController : ControllerBase
    {
        private readonly IShipperService _shipperService;
        public ShipperController(IShipperService shipperService) { _shipperService = shipperService; }

        /// <summary>
        /// Xem danh sach don hang cho shipper nhan (ReadyForPickup)
        /// </summary>
        [HttpGet("available-orders")]
        public async Task<IActionResult> GetAvailableOrders()
        {
            return Ok(await _shipperService.GetAvailableOrdersAsync());
        }

        /// <summary>
        /// Xem danh sach don hang dang giao cua shipper
        /// </summary>
        [HttpGet("my-orders")]
        public async Task<IActionResult> GetMyOrders()
        {
            var userId = GetUserId(); if (userId == null) return Unauthorized();
            return Ok(await _shipperService.GetMyOrdersAsync(userId.Value));
        }

        /// <summary>
        /// Shipper nhan don hang de giao (ReadyForPickup -> Shipping)
        /// </summary>
        [HttpPost("orders/{orderId:guid}/pickup")]
        public async Task<IActionResult> PickupOrder(Guid orderId)
        {
            var userId = GetUserId(); if (userId == null) return Unauthorized();
            try { return Ok(new { Message = await _shipperService.PickupOrderAsync(userId.Value, orderId) }); }
            catch (ArgumentException ex) { return NotFound(new { ex.Message }); }
            catch (InvalidOperationException ex) { return BadRequest(new { ex.Message }); }
        }

        /// <summary>
        /// Shipper xac nhan da giao hang (Shipping -> Delivered). Shipper nhan 50% phi ship.
        /// </summary>
        [HttpPost("orders/{orderId:guid}/deliver")]
        public async Task<IActionResult> DeliverOrder(Guid orderId)
        {
            var userId = GetUserId(); if (userId == null) return Unauthorized();
            try { return Ok(new { Message = await _shipperService.DeliverOrderAsync(userId.Value, orderId) }); }
            catch (ArgumentException ex) { return NotFound(new { ex.Message }); }
            catch (InvalidOperationException ex) { return BadRequest(new { ex.Message }); }
        }

        /// <summary>
        /// Xem thu nhap va lich su giao hang cua Shipper
        /// </summary>
        [HttpGet("earnings")]
        public async Task<IActionResult> GetEarnings()
        {
            var userId = GetUserId(); if (userId == null) return Unauthorized();
            try { return Ok(await _shipperService.GetEarningsAsync(userId.Value)); }
            catch (ArgumentException ex) { return NotFound(new { ex.Message }); }
        }

        private Guid? GetUserId()
        {
            var c = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return !string.IsNullOrEmpty(c) && Guid.TryParse(c, out var id) ? id : null;
        }
    }
}
