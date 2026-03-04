using Application.DTOs;
using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cake_Design_E_Commerce_Platform.Controllers
{
    /// <summary>
    /// Quản trị hệ thống (Admin and SystemStaff)
    /// </summary>
    [ApiController]
    [Route("api/admin")]
    [Authorize(Roles = "Admin,SystemStaff")]
    public class AdminController : ControllerBase
    {
        private readonly IAdminService _adminService;
        private readonly IOrderService _orderService;
        private readonly IRevenueReportService _revenueService;
        public AdminController(IAdminService adminService, IOrderService orderService, IRevenueReportService revenueService)
        {
            _adminService = adminService;
            _orderService = orderService;
            _revenueService = revenueService;
        }

        /// <summary>
        /// Xem thống kê tổng quan toàn hệ thống (Users, Shops, Revenue)
        /// </summary>
        [HttpGet("stats")]
        public async Task<IActionResult> GetStats() => Ok(await _adminService.GetStatsAsync());

        /// <summary>
        /// Cập nhật số dư ví (Nạp tiền / Trừ tiền thủ công)
        /// </summary>
        /// <param name="dto">Thông tin cập nhật ví</param>
        [HttpPost("wallet/update")]
        public async Task<IActionResult> UpdateWallet([FromBody] UpdateWalletDto dto)
        {
            try { var result = await _adminService.UpdateWalletAsync(dto); return Ok(new { Message = "Wallet updated.", Data = result }); }
            catch (ArgumentException ex) { return BadRequest(new { ex.Message }); }
            catch (InvalidOperationException ex) { return BadRequest(new { ex.Message }); }
        }

        /// <summary>
        /// Lấy danh sách các cửa hàng đang chờ duyệt
        /// </summary>
        [HttpGet("shop/pending")]
        public async Task<IActionResult> GetPendingShops() => Ok(await _adminService.GetPendingShopsAsync());

        /// <summary>
        /// Lấy danh sách toàn bộ người dùng trong hệ thống
        /// </summary>
        /// <param name="role">Lọc theo Role (Optional)</param>
        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsers([FromQuery] string? role) => Ok(await _adminService.GetAllUsersAsync(role));

        /// <summary>
        /// Lấy danh sách toàn bộ Cửa hàng
        /// </summary>
        [HttpGet("shops")]
        public async Task<IActionResult> GetAllShops() => Ok(await _adminService.GetAllShopsAsync());

        /// <summary>
        /// Đổi Role cho một người dùng (Phân quyền)
        /// </summary>
        [HttpPut("users/{userId:guid}/role"), Authorize(Roles = "Admin")]
        public async Task<IActionResult> ChangeUserRole(Guid userId, [FromBody] ChangeRoleDto dto)
        {
            try { return Ok(new { Message = await _adminService.ChangeUserRoleAsync(userId, dto.Role) }); }
            catch (ArgumentException ex) { return BadRequest(new { ex.Message }); }
        }

        /// <summary>
        /// Tạo tài khoản mới (Shipper, SystemStaff, Staff, v.v.)
        /// </summary>
        [HttpPost("accounts"), Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateAccount([FromBody] AdminCreateAccountDto dto)
        {
            try { return StatusCode(StatusCodes.Status201Created, await _adminService.CreateAccountAsync(dto)); }
            catch (ArgumentException ex) { return BadRequest(new { ex.Message }); }
            catch (InvalidOperationException ex) { return BadRequest(new { ex.Message }); }
        }

        // === Commission Management ===

        /// <summary>
        /// Set mức hoa hồng áp dụng cho một cửa hàng (Admin only)
        /// </summary>
        [HttpPut("shops/{shopId:guid}/commission"), Authorize(Roles = "Admin")]
        public async Task<IActionResult> SetShopCommission(Guid shopId, [FromBody] SetCommissionDto dto)
        {
            try { return Ok(new { Message = await _adminService.SetShopCommissionAsync(shopId, dto.CommissionRate) }); }
            catch (ArgumentException ex) { return BadRequest(new { ex.Message }); }
        }

        // === System Revenue Report ===

        /// <summary>
        /// Xem báo cáo doanh thu từ hoa hồng của hệ thống (Admin only)
        /// </summary>
        [HttpGet("revenue"), Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetSystemRevenue()
        {
            return Ok(await _revenueService.GetSystemRevenueAsync());
        }

        // === System Wallet ===

        /// <summary>
        /// Xem các ví tổng của hệ thống (Ví Escrow, Ví trung gian)
        /// </summary>
        [HttpGet("system/wallets"), Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetSystemWallets()
        {
            return Ok(await _adminService.GetSystemWalletsAsync());
        }

        /// <summary>
        /// Lịch sử giao dịch dòng tiền của ví hệ thống
        /// </summary>
        [HttpGet("system/wallets/transactions"), Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetSystemWalletTransactions([FromQuery] string? walletType, [FromQuery] int count = 50)
        {
            return Ok(await _adminService.GetSystemWalletTransactionsAsync(walletType, count));
        }

        // === Refund Management ===

        /// <summary>
        /// Xem danh sách yêu cầu hoàn tiền đang chờ xử lý
        /// </summary>
        [HttpGet("refunds/pending")]
        public async Task<IActionResult> GetPendingRefunds()
        {
            return Ok(await _orderService.GetPendingRefundsAsync());
        }

        /// <summary>
        /// Xem chi tiết một yêu cầu hoàn tiền
        /// </summary>
        [HttpGet("refunds/{refundId:guid}")]
        public async Task<IActionResult> GetRefundById(Guid refundId)
        {
            var refund = await _orderService.GetRefundByIdAsync(refundId);
            return refund != null ? Ok(refund) : NotFound(new { Message = "Refund request not found." });
        }

        /// <summary>
        /// Chấp nhận hoặc Từ chối yêu cầu hoàn tiền của khách hàng
        /// </summary>
        [HttpPost("refunds/{refundId:guid}/resolve")]
        public async Task<IActionResult> ResolveRefund(Guid refundId, [FromBody] ResolveRefundDto dto)
        {
            var staffId = GetUserId(); if (staffId == null) return Unauthorized();
            try { return Ok(new { Message = await _orderService.ResolveRefundAsync(staffId.Value, refundId, dto) }); }
            catch (ArgumentException ex) { return NotFound(new { ex.Message }); }
            catch (InvalidOperationException ex) { return BadRequest(new { ex.Message }); }
        }

        private Guid? GetUserId()
        {
            var c = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            return !string.IsNullOrEmpty(c) && Guid.TryParse(c, out var id) ? id : null;
        }
    }
}
