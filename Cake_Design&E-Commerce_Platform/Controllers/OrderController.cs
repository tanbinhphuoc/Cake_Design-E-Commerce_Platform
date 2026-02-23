using Application.DTOs;
using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Cake_Design_E_Commerce_Platform.Controllers
{
    [ApiController]
    [Route("api")]
    public class OrderController : ControllerBase
    {
        private readonly IOrderService _orderService;
        private readonly IShopService _shopService;
        public OrderController(IOrderService orderService, IShopService shopService) { _orderService = orderService; _shopService = shopService; }

        [HttpPost("orders/create"), Authorize]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderDto dto)
        {
            var userId = GetUserId(); if (userId == null) return Unauthorized();
            try
            {
                var ip = HttpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString() ?? "127.0.0.1";
                var result = await _orderService.CreateOrderAsync(userId.Value, dto, ip);
                if (result.RequiresPaymentRedirect)
                    return Ok(new { Message = "Orders created. Redirect to VNPay.", result.Orders, result.TotalAmount, result.PaymentUrl });
                return Ok(new { Message = "Orders created successfully.", result.Orders, result.TotalAmount, result.RemainingBalance });
            }
            catch (ArgumentException ex) { return BadRequest(new { ex.Message }); }
            catch (InvalidOperationException ex) { return BadRequest(new { ex.Message }); }
        }

        [HttpGet("orders"), Authorize]
        public async Task<IActionResult> GetOrders()
        {
            var userId = GetUserId(); if (userId == null) return Unauthorized();
            return Ok(await _orderService.GetOrdersAsync(userId.Value));
        }

        [HttpGet("orders/{id:guid}"), Authorize]
        public async Task<IActionResult> GetOrderById(Guid id)
        {
            var userId = GetUserId(); if (userId == null) return Unauthorized();
            var order = await _orderService.GetOrderByIdAsync(userId.Value, id);
            return order != null ? Ok(order) : NotFound(new { Message = "Order not found." });
        }

        [HttpPost("orders/{id:guid}/cancel"), Authorize]
        public async Task<IActionResult> CancelOrder(Guid id)
        {
            var userId = GetUserId(); if (userId == null) return Unauthorized();
            try { return Ok(new { Message = await _orderService.CancelOrderAsync(userId.Value, id) }); }
            catch (ArgumentException ex) { return NotFound(new { ex.Message }); }
            catch (InvalidOperationException ex) { return BadRequest(new { ex.Message }); }
        }

        [HttpPost("orders/{id:guid}/confirm-received"), Authorize]
        public async Task<IActionResult> ConfirmReceived(Guid id)
        {
            var userId = GetUserId(); if (userId == null) return Unauthorized();
            try { return Ok(new { Message = await _orderService.ConfirmReceivedAsync(userId.Value, id) }); }
            catch (ArgumentException ex) { return NotFound(new { ex.Message }); }
            catch (InvalidOperationException ex) { return BadRequest(new { ex.Message }); }
        }

        // VNPay
        [HttpGet("orders/vnpay/ipn"), AllowAnonymous]
        public async Task<IActionResult> VnPayIpn()
        {
            var data = Request.Query.Keys.ToDictionary(k => k, k => Request.Query[k].ToString());
            var result = await _orderService.ProcessVnPayIpnAsync(data);
            return Ok(result);
        }

        [HttpGet("orders/vnpay/return"), AllowAnonymous]
        public IActionResult VnPayReturn()
        {
            var data = Request.Query.Keys.ToDictionary(k => k, k => Request.Query[k].ToString());
            return Ok(_orderService.ProcessVnPayReturn(data));
        }

        // Shop Orders
        [HttpGet("shop/orders"), Authorize(Roles = "ShopOwner,Staff")]
        public async Task<IActionResult> GetShopOrders()
        {
            var userId = GetUserId(); if (userId == null) return Unauthorized();
            var shopId = await _shopService.GetShopIdForUserAsync(userId.Value);
            if (shopId == null) return NotFound(new { Message = "Shop not found." });
            return Ok(await _orderService.GetShopOrdersAsync(shopId.Value));
        }

        [HttpGet("shop/orders/{id:guid}"), Authorize(Roles = "ShopOwner,Staff")]
        public async Task<IActionResult> GetShopOrderById(Guid id)
        {
            var userId = GetUserId(); if (userId == null) return Unauthorized();
            var shopId = await _shopService.GetShopIdForUserAsync(userId.Value);
            if (shopId == null) return NotFound(new { Message = "Shop not found." });
            var order = await _orderService.GetShopOrderByIdAsync(shopId.Value, id);
            return order != null ? Ok(order) : NotFound(new { Message = "Order not found." });
        }

        [HttpPut("shop/orders/{id:guid}/status"), Authorize(Roles = "ShopOwner,Staff")]
        public async Task<IActionResult> UpdateOrderStatus(Guid id, [FromBody] UpdateOrderStatusDto dto)
        {
            var userId = GetUserId(); if (userId == null) return Unauthorized();
            var shopId = await _shopService.GetShopIdForUserAsync(userId.Value);
            if (shopId == null) return NotFound(new { Message = "Shop not found." });
            try { return Ok(new { Message = await _orderService.UpdateOrderStatusAsync(shopId.Value, id, dto) }); }
            catch (ArgumentException ex) { return NotFound(new { ex.Message }); }
            catch (InvalidOperationException ex) { return BadRequest(new { ex.Message }); }
        }

        [HttpGet("admin/orders"), Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllOrders() => Ok(await _orderService.GetAllOrdersAsync());

        private Guid? GetUserId()
        {
            var c = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return !string.IsNullOrEmpty(c) && Guid.TryParse(c, out var id) ? id : null;
        }
    }
}
