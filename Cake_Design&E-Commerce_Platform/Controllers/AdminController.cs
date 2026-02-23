using Application.DTOs;
using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cake_Design_E_Commerce_Platform.Controllers
{
    [ApiController]
    [Route("api/admin")]
    [Authorize(Roles = "Admin,Staff")]
    public class AdminController : ControllerBase
    {
        private readonly IAdminService _adminService;
        public AdminController(IAdminService adminService) { _adminService = adminService; }

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
    }
}
