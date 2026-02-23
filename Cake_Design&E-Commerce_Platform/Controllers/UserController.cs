using Application.DTOs;
using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Cake_Design_E_Commerce_Platform.Controllers
{
    [ApiController]
    [Route("api/users")]
    [Authorize]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        public UserController(IUserService userService) { _userService = userService; }

        [HttpGet("me")]
        public async Task<IActionResult> GetProfile()
        {
            var userId = GetUserId(); if (userId == null) return Unauthorized();
            var profile = await _userService.GetProfileAsync(userId.Value);
            return profile != null ? Ok(profile) : NotFound(new { Message = "User not found." });
        }

        [HttpPut("me")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateUserProfileDto dto)
        {
            var userId = GetUserId(); if (userId == null) return Unauthorized();
            try { return Ok(new { Message = await _userService.UpdateProfileAsync(userId.Value, dto) }); }
            catch (ArgumentException ex) { return BadRequest(new { ex.Message }); }
        }

        [HttpGet("addresses")]
        public async Task<IActionResult> GetAddresses()
        {
            var userId = GetUserId(); if (userId == null) return Unauthorized();
            return Ok(await _userService.GetAddressesAsync(userId.Value));
        }

        [HttpPost("addresses")]
        public async Task<IActionResult> CreateAddress([FromBody] CreateAddressDto dto)
        {
            var userId = GetUserId(); if (userId == null) return Unauthorized();
            try { var id = await _userService.CreateAddressAsync(userId.Value, dto); return Ok(new { Message = "Address created.", AddressId = id }); }
            catch (ArgumentException ex) { return BadRequest(new { ex.Message }); }
        }

        [HttpPut("addresses/{addressId:guid}")]
        public async Task<IActionResult> UpdateAddress(Guid addressId, [FromBody] UpdateAddressDto dto)
        {
            var userId = GetUserId(); if (userId == null) return Unauthorized();
            try { return Ok(new { Message = await _userService.UpdateAddressAsync(userId.Value, addressId, dto) }); }
            catch (ArgumentException ex) { return NotFound(new { ex.Message }); }
        }

        [HttpDelete("addresses/{addressId:guid}")]
        public async Task<IActionResult> DeleteAddress(Guid addressId)
        {
            var userId = GetUserId(); if (userId == null) return Unauthorized();
            try { return Ok(new { Message = await _userService.DeleteAddressAsync(userId.Value, addressId) }); }
            catch (ArgumentException ex) { return NotFound(new { ex.Message }); }
        }

        [HttpPut("me/password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            var userId = GetUserId(); if (userId == null) return Unauthorized();
            try { return Ok(new { Message = await _userService.ChangePasswordAsync(userId.Value, dto) }); }
            catch (ArgumentException ex) { return BadRequest(new { ex.Message }); }
            catch (InvalidOperationException ex) { return BadRequest(new { ex.Message }); }
        }

        private Guid? GetUserId()
        {
            var c = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return !string.IsNullOrEmpty(c) && Guid.TryParse(c, out var id) ? id : null;
        }
    }
}
