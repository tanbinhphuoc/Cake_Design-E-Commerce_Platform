using Application.DTOs;
using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Cake_Design_E_Commerce_Platform.Controllers
{
    [ApiController]
    [Route("api/shipper")]
    [Authorize(Roles = "Shipper")]
    public class ShipperController : ControllerBase
    {
        private readonly IShipperService _shipperService;
        public ShipperController(IShipperService shipperService) { _shipperService = shipperService; }

        /// <summary>
        /// L?y danh sách ??n hàng ch? shipper nh?n (ReadyForPickup)
        /// </summary>
        [HttpGet("orders/available")]
        public async Task<IActionResult> GetAvailableOrders()
        {
            return Ok(await _shipperService.GetAvailableOrdersAsync());
        }

        /// <summary>
        /// L?y danh sách ??n hàng shipper ?ang giao
        /// </summary>
        [HttpGet("orders/my")]
        public async Task<IActionResult> GetMyOrders()
        {
            var shipperId = GetUserId(); if (shipperId == null) return Unauthorized();
            return Ok(await _shipperService.GetMyOrdersAsync(shipperId.Value));
        }

        /// <summary>
        /// Shipper nh?n ??n hàng (ReadyForPickup ? Shipping)
        /// </summary>
        [HttpPost("orders/{orderId:guid}/pickup")]
        public async Task<IActionResult> PickupOrder(Guid orderId)
        {
            var shipperId = GetUserId(); if (shipperId == null) return Unauthorized();
            try { return Ok(new { Message = await _shipperService.PickupOrderAsync(shipperId.Value, orderId) }); }
            catch (ArgumentException ex) { return NotFound(new { ex.Message }); }
            catch (InvalidOperationException ex) { return BadRequest(new { ex.Message }); }
        }

        /// <summary>
        /// Shipper xác nh?n ?ã giao hàng (Shipping ? Delivered)
        /// </summary>
        [HttpPost("orders/{orderId:guid}/deliver")]
        public async Task<IActionResult> DeliverOrder(Guid orderId)
        {
            var shipperId = GetUserId(); if (shipperId == null) return Unauthorized();
            try { return Ok(new { Message = await _shipperService.DeliverOrderAsync(shipperId.Value, orderId) }); }
            catch (ArgumentException ex) { return NotFound(new { ex.Message }); }
            catch (InvalidOperationException ex) { return BadRequest(new { ex.Message }); }
        }

        private Guid? GetUserId()
        {
            var c = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return !string.IsNullOrEmpty(c) && Guid.TryParse(c, out var id) ? id : null;
        }
    }
}
