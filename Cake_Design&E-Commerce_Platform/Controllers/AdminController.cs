using Application.DTOs;
using Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Cake_Design_E_Commerce_Platform.Controllers
{
    [ApiController]
    [Route("api/admin")]
    [Authorize(Roles = "Admin,Staff")]
    public class AdminController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AdminController(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Get admin dashboard statistics.
        /// </summary>
        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            var today = DateTime.UtcNow.Date;

            var stats = new AdminStatsDto
            {
                TotalUsers = await _context.Accounts.CountAsync(a => a.Role == "Customer"),
                TotalShops = await _context.Shops.CountAsync(s => s.IsActive),
                TotalProducts = await _context.Products.CountAsync(p => p.IsActive),
                TotalOrders = await _context.Orders.CountAsync(),
                TotalRevenue = await _context.Orders
                    .Where(o => o.Status == "Completed")
                    .SumAsync(o => o.TotalAmount),
                NewUsersToday = await _context.Accounts
                    .CountAsync(a => a.CreatedAt >= today),
                OrdersToday = await _context.Orders
                    .CountAsync(o => o.CreatedAt >= today),
                RevenueToday = await _context.Orders
                    .Where(o => o.Status == "Completed" && o.CreatedAt >= today)
                    .SumAsync(o => o.TotalAmount),
                PendingReports = await _context.Reports
                    .CountAsync(r => r.Status == "Pending"),
                PendingShopRequests = await _context.Accounts
                    .CountAsync(a => a.Role == "ShopOwner" && !a.IsApproved)
            };

            return Ok(stats);
        }

        /// <summary>
        /// Update a user's wallet balance. Positive amount = add, negative = deduct.
        /// </summary>
        [HttpPost("wallet/update")]
        public async Task<IActionResult> UpdateWallet([FromBody] UpdateWalletDto dto)
        {
            var account = await _context.Accounts.FindAsync(dto.UserId);
            if (account == null)
                return BadRequest(new { Message = "User not found." });

            var newBalance = account.WalletBalance + dto.Amount;
            if (newBalance < 0)
                return BadRequest(new { Message = $"Insufficient balance. Current: {account.WalletBalance:F2}, Adjustment: {dto.Amount:F2}" });

            account.WalletBalance = newBalance;

            // Record transaction
            _context.WalletTransactions.Add(new Domain.Entities.WalletTransaction
            {
                Id = Guid.NewGuid(),
                WalletOwnerId = account.Id,
                WalletType = "User",
                Amount = dto.Amount,
                TransactionType = dto.Amount > 0 ? "Deposit" : "Withdrawal",
                Description = $"Admin wallet adjustment",
                BalanceAfter = newBalance,
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = "Wallet updated successfully.",
                UserId = account.Id,
                Username = account.Username,
                NewBalance = account.WalletBalance
            });
        }

        /// <summary>
        /// Get all pending shop requests.
        /// </summary>
        [HttpGet("shop/pending")]
        public async Task<IActionResult> GetPendingShops()
        {
            var pendingShops = await _context.Accounts
                .Include(a => a.Shop)
                .Where(a => a.Role == "ShopOwner" && !a.IsApproved)
                .Select(a => new
                {
                    a.Id,
                    a.Username,
                    a.FullName,
                    a.Role,
                    a.IsApproved,
                    ShopName = a.Shop != null ? a.Shop.ShopName : "",
                    ShopDescription = a.Shop != null ? a.Shop.Description : "",
                    a.CreatedAt
                })
                .ToListAsync();

            return Ok(pendingShops);
        }

        /// <summary>
        /// Get all users with optional role filter.
        /// </summary>
        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsers([FromQuery] string? role)
        {
            var query = _context.Accounts.AsQueryable();

            if (!string.IsNullOrWhiteSpace(role))
                query = query.Where(a => a.Role == role);

            var users = await query
                .OrderByDescending(a => a.CreatedAt)
                .Select(a => new
                {
                    a.Id,
                    a.Username,
                    a.FullName,
                    a.Email,
                    a.Phone,
                    a.Role,
                    a.IsApproved,
                    a.WalletBalance,
                    a.CreatedAt
                })
                .ToListAsync();

            return Ok(users);
        }

        /// <summary>
        /// Get all shops.
        /// </summary>
        [HttpGet("shops")]
        public async Task<IActionResult> GetAllShops()
        {
            var shops = await _context.Shops
                .Include(s => s.Owner)
                .OrderByDescending(s => s.CreatedAt)
                .Select(s => new
                {
                    s.Id,
                    s.OwnerId,
                    OwnerUsername = s.Owner.Username,
                    s.ShopName,
                    s.Description,
                    s.IsActive,
                    s.WalletBalance,
                    ProductCount = s.Products.Count,
                    s.CreatedAt
                })
                .ToListAsync();

            return Ok(shops);
        }

        /// <summary>
        /// Change a user's role.
        /// </summary>
        [HttpPut("users/{userId:guid}/role")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ChangeUserRole(Guid userId, [FromBody] dynamic roleDto)
        {
            var account = await _context.Accounts.FindAsync(userId);
            if (account == null)
                return NotFound(new { Message = "User not found." });

            string newRole = roleDto.role?.ToString() ?? roleDto.Role?.ToString();
            var validRoles = new[] { "Customer", "ShopOwner", "Admin", "Staff" };
            if (!validRoles.Contains(newRole))
                return BadRequest(new { Message = "Invalid role. Must be: Customer, ShopOwner, Admin, or Staff." });

            account.Role = newRole;
            account.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { Message = $"User role updated to '{newRole}'." });
        }
    }
}
