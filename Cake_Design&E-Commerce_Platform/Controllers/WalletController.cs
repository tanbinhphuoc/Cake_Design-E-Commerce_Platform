using Application.DTOs;
using Domain.Entities;
using Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Cake_Design_E_Commerce_Platform.Controllers
{
    [ApiController]
    [Route("api")]
    [Authorize]
    public class WalletController : ControllerBase
    {
        private readonly AppDbContext _context;

        public WalletController(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Get current user's wallet balance.
        /// </summary>
        [HttpGet("wallet/me")]
        public async Task<IActionResult> GetMyWallet()
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var account = await _context.Accounts.FindAsync(userId);
            if (account == null) return NotFound();

            return Ok(new WalletDto
            {
                Balance = account.WalletBalance,
                OwnerType = "User"
            });
        }

        /// <summary>
        /// Get current shop's wallet balance.
        /// </summary>
        [HttpGet("shop/wallet")]
        [Authorize(Roles = "ShopOwner")]
        public async Task<IActionResult> GetShopWallet()
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var shop = await _context.Shops.FirstOrDefaultAsync(s => s.OwnerId == userId);
            if (shop == null)
                return NotFound(new { Message = "Shop not found." });

            return Ok(new WalletDto
            {
                Balance = shop.WalletBalance,
                OwnerType = "Shop"
            });
        }

        /// <summary>
        /// Get wallet transaction history for current user.
        /// </summary>
        [HttpGet("wallet/transactions")]
        public async Task<IActionResult> GetWalletTransactions()
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var transactions = await _context.WalletTransactions
                .Where(t => t.WalletOwnerId == userId && t.WalletType == "User")
                .OrderByDescending(t => t.CreatedAt)
                .Select(t => new WalletTransactionDto
                {
                    Id = t.Id,
                    Amount = t.Amount,
                    TransactionType = t.TransactionType,
                    Description = t.Description,
                    BalanceAfter = t.BalanceAfter,
                    ReferenceId = t.ReferenceId,
                    CreatedAt = t.CreatedAt
                })
                .ToListAsync();

            return Ok(transactions);
        }

        /// <summary>
        /// Get wallet transaction history for current shop.
        /// </summary>
        [HttpGet("shop/wallet/transactions")]
        [Authorize(Roles = "ShopOwner")]
        public async Task<IActionResult> GetShopWalletTransactions()
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var shop = await _context.Shops.FirstOrDefaultAsync(s => s.OwnerId == userId);
            if (shop == null)
                return NotFound(new { Message = "Shop not found." });

            var transactions = await _context.WalletTransactions
                .Where(t => t.WalletOwnerId == shop.Id && t.WalletType == "Shop")
                .OrderByDescending(t => t.CreatedAt)
                .Select(t => new WalletTransactionDto
                {
                    Id = t.Id,
                    Amount = t.Amount,
                    TransactionType = t.TransactionType,
                    Description = t.Description,
                    BalanceAfter = t.BalanceAfter,
                    ReferenceId = t.ReferenceId,
                    CreatedAt = t.CreatedAt
                })
                .ToListAsync();

            return Ok(transactions);
        }

        // ===== Payment =====

        /// <summary>
        /// Create a payment for an order (for non-wallet payments).
        /// </summary>
        [HttpPost("payments/create")]
        public async Task<IActionResult> CreatePayment([FromBody] CreatePaymentDto dto)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == dto.OrderId && o.UserId == userId);
            if (order == null)
                return NotFound(new { Message = "Order not found." });

            if (order.PaymentStatus == "Paid")
                return BadRequest(new { Message = "Order is already paid." });

            var existingPayment = await _context.Payments.FirstOrDefaultAsync(p => p.OrderId == dto.OrderId);
            if (existingPayment != null && existingPayment.Status == "Completed")
                return BadRequest(new { Message = "Payment already completed." });

            // Handle wallet payment
            if (dto.Method == "Wallet")
            {
                var account = await _context.Accounts.FindAsync(userId);
                if (account == null) return Unauthorized();

                if (account.WalletBalance < order.TotalAmount)
                    return BadRequest(new { Message = $"Insufficient balance. Required: {order.TotalAmount:F2}." });

                account.WalletBalance -= order.TotalAmount;

                _context.WalletTransactions.Add(new WalletTransaction
                {
                    Id = Guid.NewGuid(),
                    WalletOwnerId = account.Id,
                    WalletType = "User",
                    Amount = -order.TotalAmount,
                    TransactionType = "Purchase",
                    Description = $"Payment for order {order.Id}",
                    BalanceAfter = account.WalletBalance,
                    ReferenceId = order.Id,
                    CreatedAt = DateTime.UtcNow
                });

                var shop = await _context.Shops.FindAsync(order.ShopId);
                if (shop != null)
                {
                    shop.WalletBalance += order.TotalAmount;
                    _context.WalletTransactions.Add(new WalletTransaction
                    {
                        Id = Guid.NewGuid(),
                        WalletOwnerId = shop.Id,
                        WalletType = "Shop",
                        Amount = order.TotalAmount,
                        TransactionType = "Sale",
                        Description = $"Payment received for order {order.Id}",
                        BalanceAfter = shop.WalletBalance,
                        ReferenceId = order.Id,
                        CreatedAt = DateTime.UtcNow
                    });
                }

                order.PaymentStatus = "Paid";
                order.PaymentMethod = "Wallet";

                var payment = new Payment
                {
                    Id = Guid.NewGuid(),
                    OrderId = order.Id,
                    UserId = userId.Value,
                    Amount = order.TotalAmount,
                    Method = "Wallet",
                    Status = "Completed",
                    CreatedAt = DateTime.UtcNow,
                    CompletedAt = DateTime.UtcNow
                };

                if (existingPayment != null)
                    _context.Payments.Remove(existingPayment);

                _context.Payments.Add(payment);
                await _context.SaveChangesAsync();

                return Ok(new PaymentDto
                {
                    Id = payment.Id,
                    OrderId = payment.OrderId,
                    Amount = payment.Amount,
                    Method = payment.Method,
                    Status = payment.Status,
                    CreatedAt = payment.CreatedAt
                });
            }

            // For other payment methods (VNPay, MoMo, Stripe) - create pending payment
            var pendingPayment = new Payment
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                UserId = userId.Value,
                Amount = order.TotalAmount,
                Method = dto.Method,
                Status = "Pending",
                TransactionRef = $"{dto.Method.ToUpper()}_{Guid.NewGuid():N}",
                CreatedAt = DateTime.UtcNow
            };

            if (existingPayment != null)
                _context.Payments.Remove(existingPayment);

            _context.Payments.Add(pendingPayment);
            await _context.SaveChangesAsync();

            return Ok(new PaymentDto
            {
                Id = pendingPayment.Id,
                OrderId = pendingPayment.OrderId,
                Amount = pendingPayment.Amount,
                Method = pendingPayment.Method,
                Status = pendingPayment.Status,
                TransactionRef = pendingPayment.TransactionRef,
                CreatedAt = pendingPayment.CreatedAt
            });
        }

        /// <summary>
        /// Payment webhook (for VNPay/MoMo/Stripe callbacks).
        /// </summary>
        [HttpPost("payments/webhook")]
        [AllowAnonymous]
        public async Task<IActionResult> PaymentWebhook([FromBody] dynamic webhookData)
        {
            // Placeholder for future payment gateway integration
            // In production, validate signature, process callback, update payment & order status
            return Ok(new { Message = "Webhook received." });
        }

        private Guid? GetUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                return null;
            return userId;
        }
    }
}
