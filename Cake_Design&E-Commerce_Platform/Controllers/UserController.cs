using Application.DTOs;
using Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Cake_Design_E_Commerce_Platform.Controllers
{
    [ApiController]
    [Route("api/users")]
    [Authorize]
    public class UserController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UserController(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Get current user's profile.
        /// </summary>
        [HttpGet("me")]
        public async Task<IActionResult> GetProfile()
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var account = await _context.Accounts.FindAsync(userId);
            if (account == null) return NotFound(new { Message = "User not found." });

            return Ok(new UserProfileDto
            {
                Id = account.Id,
                Username = account.Username,
                FullName = account.FullName,
                Email = account.Email,
                Phone = account.Phone,
                AvatarUrl = account.AvatarUrl,
                Role = account.Role,
                WalletBalance = account.WalletBalance,
                DefaultAddressId = account.DefaultAddressId,
                CreatedAt = account.CreatedAt
            });
        }

        /// <summary>
        /// Update current user's profile (name, phone, email, avatar, default address).
        /// </summary>
        [HttpPut("me")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateUserProfileDto dto)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var account = await _context.Accounts.FindAsync(userId);
            if (account == null) return NotFound(new { Message = "User not found." });

            if (dto.FullName != null) account.FullName = dto.FullName;
            if (dto.Email != null) account.Email = dto.Email;
            if (dto.Phone != null) account.Phone = dto.Phone;
            if (dto.AvatarUrl != null) account.AvatarUrl = dto.AvatarUrl;
            if (dto.DefaultAddressId != null)
            {
                // Verify address belongs to user
                var address = await _context.Addresses
                    .FirstOrDefaultAsync(a => a.Id == dto.DefaultAddressId && a.UserId == userId);
                if (address == null)
                    return BadRequest(new { Message = "Address not found or does not belong to you." });
                account.DefaultAddressId = dto.DefaultAddressId;
            }

            account.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Profile updated successfully." });
        }

        // ===== Addresses =====

        /// <summary>
        /// Get all shipping addresses for current user.
        /// </summary>
        [HttpGet("addresses")]
        public async Task<IActionResult> GetAddresses()
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var addresses = await _context.Addresses
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.IsDefault)
                .ThenByDescending(a => a.CreatedAt)
                .Select(a => new AddressDto
                {
                    Id = a.Id,
                    ReceiverName = a.ReceiverName,
                    Phone = a.Phone,
                    Street = a.Street,
                    Ward = a.Ward,
                    District = a.District,
                    City = a.City,
                    IsDefault = a.IsDefault
                })
                .ToListAsync();

            return Ok(addresses);
        }

        /// <summary>
        /// Create a new shipping address.
        /// </summary>
        [HttpPost("addresses")]
        public async Task<IActionResult> CreateAddress([FromBody] CreateAddressDto dto)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            if (string.IsNullOrWhiteSpace(dto.ReceiverName) || string.IsNullOrWhiteSpace(dto.Phone) ||
                string.IsNullOrWhiteSpace(dto.Street) || string.IsNullOrWhiteSpace(dto.City))
            {
                return BadRequest(new { Message = "ReceiverName, Phone, Street, and City are required." });
            }

            // If this is the first address or set as default, unset others
            if (dto.IsDefault)
            {
                var existingDefaults = await _context.Addresses
                    .Where(a => a.UserId == userId && a.IsDefault)
                    .ToListAsync();
                foreach (var addr in existingDefaults)
                    addr.IsDefault = false;
            }

            var address = new Domain.Entities.Address
            {
                Id = Guid.NewGuid(),
                UserId = userId.Value,
                ReceiverName = dto.ReceiverName,
                Phone = dto.Phone,
                Street = dto.Street,
                Ward = dto.Ward,
                District = dto.District,
                City = dto.City,
                IsDefault = dto.IsDefault,
                CreatedAt = DateTime.UtcNow
            };

            _context.Addresses.Add(address);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Address created successfully.", AddressId = address.Id });
        }

        /// <summary>
        /// Update a shipping address.
        /// </summary>
        [HttpPut("addresses/{addressId:guid}")]
        public async Task<IActionResult> UpdateAddress(Guid addressId, [FromBody] UpdateAddressDto dto)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var address = await _context.Addresses
                .FirstOrDefaultAsync(a => a.Id == addressId && a.UserId == userId);

            if (address == null)
                return NotFound(new { Message = "Address not found." });

            if (dto.ReceiverName != null) address.ReceiverName = dto.ReceiverName;
            if (dto.Phone != null) address.Phone = dto.Phone;
            if (dto.Street != null) address.Street = dto.Street;
            if (dto.Ward != null) address.Ward = dto.Ward;
            if (dto.District != null) address.District = dto.District;
            if (dto.City != null) address.City = dto.City;

            if (dto.IsDefault == true)
            {
                var existingDefaults = await _context.Addresses
                    .Where(a => a.UserId == userId && a.IsDefault && a.Id != addressId)
                    .ToListAsync();
                foreach (var addr in existingDefaults)
                    addr.IsDefault = false;
                address.IsDefault = true;
            }
            else if (dto.IsDefault == false)
            {
                address.IsDefault = false;
            }

            await _context.SaveChangesAsync();

            return Ok(new { Message = "Address updated successfully." });
        }

        /// <summary>
        /// Delete a shipping address.
        /// </summary>
        [HttpDelete("addresses/{addressId:guid}")]
        public async Task<IActionResult> DeleteAddress(Guid addressId)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var address = await _context.Addresses
                .FirstOrDefaultAsync(a => a.Id == addressId && a.UserId == userId);

            if (address == null)
                return NotFound(new { Message = "Address not found." });

            _context.Addresses.Remove(address);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Address deleted successfully." });
        }

        private Guid? GetUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return null;
            }
            return userId;
        }
    }
}
