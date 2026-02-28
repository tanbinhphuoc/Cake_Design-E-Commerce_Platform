using Application.DTOs;
using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cake_Design_E_Commerce_Platform.Controllers
{
    [ApiController]
    [Route("api/admin")]
    [Authorize(Roles = "Admin,SystemStaff")]
    public class AdminController : ControllerBase
    {
        private readonly IAdminService _adminService;
        private readonly IOrderService _orderService;
        public AdminController(IAdminService adminService, IOrderService orderService) 
        { 
            _adminService = adminService; 
            _orderService = orderService; 
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetStats() => Ok(await _adminService.GetStatsAsync());

        [HttpPost("wallet/update")]
        public async Task<IActionResult> UpdateWallet([FromBody] UpdateWalletDto dto)
        {
            try { var result = await _adminService.UpdateWalletAsync(dto); return Ok(new { Message = "Wallet updated.", Data = result }); }
            catch (ArgumentException ex) { return BadRequest(new { ex.Message }); }
            catch (InvalidOperationException ex) { return BadRequest(new { ex.Message }); }
        }

        [HttpGet("shop/pending")]
        public async Task<IActionResult> GetPendingShops() => Ok(await _adminService.GetPendingShopsAsync());

        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsers([FromQuery] string? role) => Ok(await _adminService.GetAllUsersAsync(role));

        [HttpGet("shops")]
        public async Task<IActionResult> GetAllShops() => Ok(await _adminService.GetAllShopsAsync());

        [HttpPut("users/{userId:guid}/role"), Authorize(Roles = "Admin")]
        public async Task<IActionResult> ChangeUserRole(Guid userId, [FromBody] ChangeRoleDto dto)
        {
            try { return Ok(new { Message = await _adminService.ChangeUserRoleAsync(userId, dto.Role) }); }
            catch (ArgumentException ex) { return BadRequest(new { ex.Message }); }
        }

        // === System Wallet ===

        /// <summary>
        /// L?y danh sách ví h? th?ng (Escrow, Revenue, etc.)
        /// </summary>
        [HttpGet("system/wallets"), Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetSystemWallets()
        {
            return Ok(await _adminService.GetSystemWalletsAsync());
        }

        /// <summary>
        /// L?y l?ch s? giao d?ch ví h? th?ng
        /// </summary>
        [HttpGet("system/wallets/transactions"), Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetSystemWalletTransactions([FromQuery] string? walletType, [FromQuery] int count = 50)
        {
            return Ok(await _adminService.GetSystemWalletTransactionsAsync(walletType, count));
        }

        // === Refund Management ===

        /// <summary>
        /// L?y danh sách yêu c?u hoàn ti?n ch? duy?t
        /// </summary>
        [HttpGet("refunds/pending")]
        public async Task<IActionResult> GetPendingRefunds()
        {
            return Ok(await _orderService.GetPendingRefundsAsync());
        }

        /// <summary>
        /// Xem chi ti?t yêu c?u hoàn ti?n
        /// </summary>
        [HttpGet("refunds/{refundId:guid}")]
        public async Task<IActionResult> GetRefundById(Guid refundId)
        {
            var refund = await _orderService.GetRefundByIdAsync(refundId);
            return refund != null ? Ok(refund) : NotFound(new { Message = "Refund request not found." });
        }

        /// <summary>
        /// Duy?t ho?c t? ch?i yêu c?u hoàn ti?n
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
