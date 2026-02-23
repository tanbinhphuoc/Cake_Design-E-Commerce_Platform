using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;

namespace Application.Services
{
    public class WalletService : IWalletService
    {
        private readonly IUnitOfWork _uow;
        public WalletService(IUnitOfWork uow) { _uow = uow; }

        /// <summary>
        /// Get user's wallet (this is also the shop wallet if user is ShopOwner)
        /// </summary>
        public async Task<WalletDto> GetUserWalletAsync(Guid userId)
        {
            var account = await _uow.Accounts.GetByIdAsync(userId);
            if (account == null) throw new ArgumentException("User not found.");

            // Single wallet: Account.WalletBalance is used for everything
            var ownerType = account.Role == "ShopOwner" ? "ShopOwner" : "User";
            return new WalletDto { Balance = account.WalletBalance, OwnerType = ownerType };
        }

        /// <summary>
        /// Get shop wallet = owner's Account.WalletBalance (same wallet, unified)
        /// </summary>
        public async Task<WalletDto?> GetShopWalletAsync(Guid ownerId)
        {
            var account = await _uow.Accounts.GetByIdAsync(ownerId);
            if (account == null) return null;
            var shop = await _uow.Shops.GetByOwnerIdAsync(ownerId);
            if (shop == null) return null;

            // Shop wallet = owner's Account.WalletBalance
            return new WalletDto { Balance = account.WalletBalance, OwnerType = "ShopOwner" };
        }

        public async Task<List<WalletTransactionDto>> GetUserTransactionsAsync(Guid userId)
        {
            // Get all transactions: both User and Shop types for this owner
            var userTxns = await _uow.WalletTransactions.GetByOwnerAsync(userId, "User");
            var shopTxns = await _uow.WalletTransactions.GetByOwnerAsync(userId, "Shop");

            // Also check shop if exists — merge shop-level transactions
            var allShopTxns = new List<WalletTransaction>(shopTxns);
            var shop = await _uow.Shops.GetByOwnerIdAsync(userId);
            if (shop != null)
            {
                var shopIdTxns = await _uow.WalletTransactions.GetByOwnerAsync(shop.Id, "Shop");
                allShopTxns.AddRange(shopIdTxns);
                allShopTxns = allShopTxns.DistinctBy(t => t.Id).ToList();
            }

            var all = userTxns.Concat(allShopTxns).OrderByDescending(t => t.CreatedAt);
            return all.Select(t => new WalletTransactionDto
            { Id = t.Id, Amount = t.Amount, TransactionType = t.TransactionType, Description = t.Description, BalanceAfter = t.BalanceAfter, ReferenceId = t.ReferenceId, CreatedAt = t.CreatedAt }).ToList();
        }

        public async Task<List<WalletTransactionDto>> GetShopTransactionsAsync(Guid ownerId)
        {
            // Shop transactions are now stored with shop.Id as WalletOwnerId
            var shop = await _uow.Shops.GetByOwnerIdAsync(ownerId);
            if (shop == null) throw new ArgumentException("Shop not found.");
            var txns = await _uow.WalletTransactions.GetByOwnerAsync(shop.Id, "Shop");
            return txns.Select(t => new WalletTransactionDto
            { Id = t.Id, Amount = t.Amount, TransactionType = t.TransactionType, Description = t.Description, BalanceAfter = t.BalanceAfter, ReferenceId = t.ReferenceId, CreatedAt = t.CreatedAt }).ToList();
        }

        /// <summary>
        /// Deposit money into own wallet
        /// </summary>
        public async Task<object> DepositAsync(Guid userId, DepositWalletDto dto)
        {
            if (dto.Amount <= 0) throw new ArgumentException("Amount must be greater than 0.");
            var account = await _uow.Accounts.GetByIdAsync(userId);
            if (account == null) throw new ArgumentException("User not found.");

            account.WalletBalance += dto.Amount;
            account.UpdatedAt = DateTime.UtcNow;

            await _uow.WalletTransactions.AddAsync(new WalletTransaction
            {
                Id = Guid.NewGuid(), WalletOwnerId = account.Id, WalletType = "User",
                Amount = dto.Amount, TransactionType = "Deposit",
                Description = dto.Description ?? "Wallet deposit",
                BalanceAfter = account.WalletBalance, CreatedAt = DateTime.UtcNow
            });
            await _uow.SaveChangesAsync();
            return new { Message = "Deposit successful.", NewBalance = account.WalletBalance };
        }

        public async Task<PaymentDto> CreatePaymentAsync(Guid userId, CreatePaymentDto dto)
        {
            var order = await _uow.Orders.FirstOrDefaultAsync(o => o.Id == dto.OrderId && o.UserId == userId);
            if (order == null) throw new ArgumentException("Order not found.");
            if (order.PaymentStatus == "Paid") throw new InvalidOperationException("Order is already paid.");
            var existingPayment = await _uow.Payments.GetByOrderIdAsync(dto.OrderId);
            if (existingPayment != null && existingPayment.Status == "Completed")
                throw new InvalidOperationException("Payment already completed.");

            if (dto.Method == "Wallet")
            {
                var account = await _uow.Accounts.GetByIdAsync(userId);
                if (account == null) throw new UnauthorizedAccessException();
                if (account.WalletBalance < order.TotalAmount)
                    throw new InvalidOperationException($"Insufficient balance. Required: {order.TotalAmount:F2}, Available: {account.WalletBalance:F2}.");

                account.WalletBalance -= order.TotalAmount;
                await _uow.WalletTransactions.AddAsync(new WalletTransaction
                { Id = Guid.NewGuid(), WalletOwnerId = account.Id, WalletType = "User", Amount = -order.TotalAmount, TransactionType = "Purchase", Description = $"Payment for order {order.Id}", BalanceAfter = account.WalletBalance, ReferenceId = order.Id, CreatedAt = DateTime.UtcNow });

                // Credit shop OWNER's wallet (unified wallet)
                var shop = await _uow.Shops.GetByIdAsync(order.ShopId);
                if (shop != null)
                {
                    var shopOwner = await _uow.Accounts.GetByIdAsync(shop.OwnerId);
                    if (shopOwner != null)
                    {
                        shopOwner.WalletBalance += order.TotalAmount;
                        await _uow.WalletTransactions.AddAsync(new WalletTransaction
                        { Id = Guid.NewGuid(), WalletOwnerId = shop.Id, WalletType = "Shop", Amount = order.TotalAmount, TransactionType = "Sale", Description = $"Payment received for order {order.Id}", BalanceAfter = shopOwner.WalletBalance, ReferenceId = order.Id, CreatedAt = DateTime.UtcNow });
                    }
                }
                order.PaymentStatus = "Paid"; order.PaymentMethod = "Wallet";
                var payment = new Payment { Id = Guid.NewGuid(), OrderId = order.Id, UserId = userId, Amount = order.TotalAmount, Method = "Wallet", Status = "Completed", CreatedAt = DateTime.UtcNow, CompletedAt = DateTime.UtcNow };
                if (existingPayment != null) _uow.Payments.Remove(existingPayment);
                await _uow.Payments.AddAsync(payment);
                await _uow.SaveChangesAsync();
                return new PaymentDto { Id = payment.Id, OrderId = payment.OrderId, Amount = payment.Amount, Method = payment.Method, Status = payment.Status, CreatedAt = payment.CreatedAt };
            }

            var pendingPayment = new Payment { Id = Guid.NewGuid(), OrderId = order.Id, UserId = userId, Amount = order.TotalAmount, Method = dto.Method, Status = "Pending", TransactionRef = $"{dto.Method.ToUpper()}_{Guid.NewGuid():N}", CreatedAt = DateTime.UtcNow };
            if (existingPayment != null) _uow.Payments.Remove(existingPayment);
            await _uow.Payments.AddAsync(pendingPayment);
            await _uow.SaveChangesAsync();
            return new PaymentDto { Id = pendingPayment.Id, OrderId = pendingPayment.OrderId, Amount = pendingPayment.Amount, Method = pendingPayment.Method, Status = pendingPayment.Status, TransactionRef = pendingPayment.TransactionRef, CreatedAt = pendingPayment.CreatedAt };
        }
    }
}
