using Application.DTOs;
using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Cake_Design_E_Commerce_Platform.Controllers
{
    [ApiController]
    [Route("api")]
    [Authorize]
    public class WalletController : ControllerBase
    {
        private readonly IWalletService _walletService;
        public WalletController(IWalletService walletService) { _walletService = walletService; }

        /// <summary>
        /// Get current user's wallet balance. If ShopOwner, this is also the shop wallet.
        /// </summary>
        [HttpGet("wallet/me")]
        public async Task<IActionResult> GetMyWallet()
        {
            var userId = GetUserId(); if (userId == null) return Unauthorized();
            try { return Ok(await _walletService.GetUserWalletAsync(userId.Value)); }
            catch (ArgumentException ex) { return NotFound(new { ex.Message }); }
        }

        /// <summary>
        /// Get shop wallet (= owner's wallet, unified). Returns the same balance as wallet/me for ShopOwner.
        /// </summary>
        [HttpGet("shop/wallet"), Authorize(Roles = "ShopOwner")]
        public async Task<IActionResult> GetShopWallet()
        {
            var userId = GetUserId(); if (userId == null) return Unauthorized();
            var wallet = await _walletService.GetShopWalletAsync(userId.Value);
            return wallet != null ? Ok(wallet) : NotFound(new { Message = "Shop not found." });
        }

        /// <summary>
        /// Deposit money into own wallet.
        /// </summary>
        [HttpPost("wallet/deposit")]
        public async Task<IActionResult> Deposit([FromBody] DepositWalletDto dto)
        {
            var userId = GetUserId(); if (userId == null) return Unauthorized();
            try { return Ok(await _walletService.DepositAsync(userId.Value, dto)); }
            catch (ArgumentException ex) { return BadRequest(new { ex.Message }); }
        }

        [HttpGet("wallet/transactions")]
        public async Task<IActionResult> GetWalletTransactions()
        {
            var userId = GetUserId(); if (userId == null) return Unauthorized();
            return Ok(await _walletService.GetUserTransactionsAsync(userId.Value));
        }

        [HttpGet("shop/wallet/transactions"), Authorize(Roles = "ShopOwner")]
        public async Task<IActionResult> GetShopWalletTransactions()
        {
            var userId = GetUserId(); if (userId == null) return Unauthorized();
            try { return Ok(await _walletService.GetShopTransactionsAsync(userId.Value)); }
            catch (ArgumentException ex) { return NotFound(new { ex.Message }); }
        }

        [HttpPost("payments/create")]
        public async Task<IActionResult> CreatePayment([FromBody] CreatePaymentDto dto)
        {
            var userId = GetUserId(); if (userId == null) return Unauthorized();
            try { return Ok(await _walletService.CreatePaymentAsync(userId.Value, dto)); }
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
