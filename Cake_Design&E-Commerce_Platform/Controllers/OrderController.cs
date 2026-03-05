using Application.DTOs;
using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Cake_Design_E_Commerce_Platform.Controllers
{
    /// <summary>
    /// Quản lý Đơn hàng và Thanh toán
    /// </summary>
    [ApiController]
    [Route("api")]
    public class OrderController : ControllerBase
    {
        private readonly IOrderService _orderService;
        private readonly IShopService _shopService;
        public OrderController(IOrderService orderService, IShopService shopService) { _orderService = orderService; _shopService = shopService; }

        /// <summary>
        /// Tạo đơn hàng mới từ giỏ hàng hiện tại, khởi tạo thanh toán
        /// </summary>
        /// <param name="dto">Thông tin tạo đơn hàng (Địa chỉ, Mã giảm giá...)</param>
        /// <response code="200">Tạo đơn hàng thành công, trả về URL thanh toán VNPay nếu cần</response>
        /// <response code="400">Giỏ hàng trống hoặc thông tin không hợp lệ</response>
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

        /// <summary>
        /// Lấy danh sách đơn hàng của người dùng hiện tại
        /// </summary>
        /// <response code="200">Danh sách đơn hàng của User</response>
        [HttpGet("orders"), Authorize]
        public async Task<IActionResult> GetOrders()
        {
            var userId = GetUserId(); if (userId == null) return Unauthorized();
            return Ok(await _orderService.GetOrdersAsync(userId.Value));
        }

        /// <summary>
        /// Lấy chi tiết một đơn hàng theo ID
        /// </summary>
        /// <param name="id">ID đơn hàng</param>
        /// <response code="200">Trả về thông tin chi tiết đơn hàng</response>
        /// <response code="404">Không tìm thấy đơn hàng</response>
        [HttpGet("orders/{id:guid}"), Authorize]
        public async Task<IActionResult> GetOrderById(Guid id)
        {
            var userId = GetUserId(); if (userId == null) return Unauthorized();
            var order = await _orderService.GetOrderByIdAsync(userId.Value, id);
            return order != null ? Ok(order) : NotFound(new { Message = "Order not found." });
        }

        /// <summary>
        /// Khách hàng hủy đơn hàng (chỉ khi đang ở trạng thái Pending/Chờ xử lý)
        /// </summary>
        /// <param name="id">ID đơn hàng</param>
        [HttpPost("orders/{id:guid}/cancel"), Authorize]
        public async Task<IActionResult> CancelOrder(Guid id)
        {
            var userId = GetUserId(); if (userId == null) return Unauthorized();
            try { return Ok(new { Message = await _orderService.CancelOrderAsync(userId.Value, id) }); }
            catch (ArgumentException ex) { return NotFound(new { ex.Message }); }
            catch (InvalidOperationException ex) { return BadRequest(new { ex.Message }); }
        }

        /// <summary>
        /// Khách hàng xác nhận đã nhận hàng (Release tiền cho Shop)
        /// </summary>
        /// <param name="id">ID đơn hàng</param>
        [HttpPost("orders/{id:guid}/confirm-received"), Authorize]
        public async Task<IActionResult> ConfirmReceived(Guid id)
        {
            var userId = GetUserId(); if (userId == null) return Unauthorized();
            try { return Ok(new { Message = await _orderService.ConfirmReceivedAsync(userId.Value, id) }); }
            catch (ArgumentException ex) { return NotFound(new { ex.Message }); }
            catch (InvalidOperationException ex) { return BadRequest(new { ex.Message }); }
        }

        /// <summary>
        /// Khách hàng yêu cầu hoàn tiền cho đơn hàng có vấn đề
        /// </summary>
        /// <param name="id">ID đơn hàng</param>
        /// <param name="dto">Lý do và hình ảnh chứng minh</param>
        [HttpPost("orders/{id:guid}/refund"), Authorize]
        public async Task<IActionResult> RequestRefund(Guid id, [FromBody] CreateRefundRequestDto dto)
        {
            var userId = GetUserId(); if (userId == null) return Unauthorized();
            try { return Ok(new { Message = await _orderService.RequestRefundAsync(userId.Value, id, dto) }); }
            catch (ArgumentException ex) { return BadRequest(new { ex.Message }); }
            catch (InvalidOperationException ex) { return BadRequest(new { ex.Message }); }
        }

        // VNPay
        /// <summary>
        /// VNPay Webhook (IPN) - Nhận thông báo kết quả giao dịch thanh toán từ VNPay
        /// </summary>
        /// <response code="200">Xác nhận đã nhận IPN</response>
        [HttpGet("orders/vnpay/ipn"), AllowAnonymous]
        public async Task<IActionResult> VnPayIpn()
        {
            var data = Request.Query.Keys.ToDictionary(k => k, k => Request.Query[k].ToString());
            var result = await _orderService.ProcessVnPayIpnAsync(data);
            return Ok(result);
        }

        /// <summary>
        /// Dẫn hướng (Return URL) của VNPay sau khi thanh toán xong để quay lại ứng dụng
        /// </summary>
        [HttpGet("orders/vnpay/return"), AllowAnonymous]
        public async Task<IActionResult> VnPayReturn()
        {
            var data = Request.Query.Keys.ToDictionary(k => k, k => Request.Query[k].ToString());
            var result = await _orderService.ProcessVnPayReturnAsync(data);
            
            // Redirect v? frontend v?i k?t qu?
            var frontendUrl = result.Success 
                ? $"http://localhost:3000/order-success?status=success&orderId={result.OrderId}"
                : $"http://localhost:3000/order-success?status=failed&message={result.Message}";
            
            return Redirect(frontendUrl);
        }

        // Shop Orders
        /// <summary>
        /// Xem danh sách các đơn hàng của Cửa hàng (Dành cho ShopOwner/Staff)
        /// </summary>
        [HttpGet("shop/orders"), Authorize(Roles = "ShopOwner,Staff")]
        public async Task<IActionResult> GetShopOrders()
        {
            var userId = GetUserId(); if (userId == null) return Unauthorized();
            var shopId = await _shopService.GetShopIdForUserAsync(userId.Value);
            if (shopId == null) return NotFound(new { Message = "Shop not found." });
            return Ok(await _orderService.GetShopOrdersAsync(shopId.Value));
        }

        /// <summary>
        /// Xem chi tiết một đơn hàng của Cửa hàng (Dành cho ShopOwner/Staff)
        /// </summary>
        [HttpGet("shop/orders/{id:guid}"), Authorize(Roles = "ShopOwner,Staff")]
        public async Task<IActionResult> GetShopOrderById(Guid id)
        {
            var userId = GetUserId(); if (userId == null) return Unauthorized();
            var shopId = await _shopService.GetShopIdForUserAsync(userId.Value);
            if (shopId == null) return NotFound(new { Message = "Shop not found." });
            var order = await _orderService.GetShopOrderByIdAsync(shopId.Value, id);
            return order != null ? Ok(order) : NotFound(new { Message = "Order not found." });
        }

        /// <summary>
        /// Cập nhật trạng thái đơn hàng của Cửa hàng (Dành cho ShopOwner/Staff)
        /// </summary>
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

        /// <summary>
        /// Quản trị viên xem tất cả các đơn hàng trên toàn hệ thống
        /// </summary>
        [HttpGet("admin/orders"), Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllOrders() => Ok(await _orderService.GetAllOrdersAsync());

        private Guid? GetUserId()
        {
            var c = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return !string.IsNullOrEmpty(c) && Guid.TryParse(c, out var id) ? id : null;
        }
    }
}
