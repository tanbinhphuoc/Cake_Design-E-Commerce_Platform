using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;

namespace Application.Services
{
    public class AdminService : IAdminService
    {
        private readonly IUnitOfWork _uow;
        public AdminService(IUnitOfWork uow) { _uow = uow; }

        public async Task<AdminStatsDto> GetStatsAsync()
        {
            var today = DateTime.UtcNow.Date;
            return new AdminStatsDto
            {
                TotalUsers = await _uow.Accounts.CountAsync(a => a.Role == "Customer"),
                TotalShops = await _uow.Shops.CountAsync(s => s.IsActive),
                TotalProducts = await _uow.Products.CountAsync(p => p.IsActive),
                TotalOrders = await _uow.Orders.CountAsync(),
                TotalRevenue = await _uow.Orders.GetTotalRevenueAsync(),
                NewUsersToday = await _uow.Accounts.CountAsync(a => a.CreatedAt >= today),
                OrdersToday = await _uow.Orders.CountAsync(o => o.CreatedAt >= today),
                RevenueToday = await _uow.Orders.GetTodayRevenueAsync(),
                PendingReports = await _uow.Reports.CountAsync(r => r.Status == "Pending"),
                PendingShopRequests = await _uow.Accounts.CountAsync(a => a.Role == "ShopOwner" && !a.IsApproved)
            };
        }

        public async Task<object> UpdateWalletAsync(UpdateWalletDto dto)
        {
            var account = await _uow.Accounts.GetByIdAsync(dto.UserId);
            if (account == null) throw new ArgumentException("User not found.");
            var newBal = account.WalletBalance + dto.Amount;
            if (newBal < 0) throw new InvalidOperationException($"Insufficient balance. Current: {account.WalletBalance:F2}");
            account.WalletBalance = newBal;
            await _uow.WalletTransactions.AddAsync(new WalletTransaction
            {
                Id = Guid.NewGuid(), WalletOwnerId = account.Id, WalletType = "User", Amount = dto.Amount,
                TransactionType = dto.Amount > 0 ? "Deposit" : "Withdrawal",
                Description = "Admin wallet adjustment", BalanceAfter = newBal, CreatedAt = DateTime.UtcNow
            });
            await _uow.SaveChangesAsync();
            return new { UserId = account.Id, account.Username, NewBalance = account.WalletBalance };
        }

        public async Task<List<object>> GetPendingShopsAsync()
        {
            var pending = await _uow.Accounts.FindAsync(a => a.Role == "ShopOwner" && !a.IsApproved);
            var result = new List<object>();
            foreach (var a in pending)
            {
                var acc = await _uow.Accounts.GetByIdWithShopAsync(a.Id);
                result.Add(new { a.Id, a.Username, a.FullName, a.Role, a.IsApproved,
                    ShopName = acc?.Shop?.ShopName ?? "", ShopDescription = acc?.Shop?.Description ?? "", a.CreatedAt });
            }
            return result;
        }

        public async Task<List<object>> GetAllUsersAsync(string? role)
        {
            IEnumerable<Account> users = !string.IsNullOrWhiteSpace(role)
                ? await _uow.Accounts.FindAsync(a => a.Role == role) : await _uow.Accounts.GetAllAsync();
            return users.OrderByDescending(a => a.CreatedAt).Select(a => (object)new
            { a.Id, a.Username, a.FullName, a.Email, a.Phone, a.Role, a.IsApproved, a.WalletBalance, a.CreatedAt }).ToList();
        }

        public async Task<List<object>> GetAllShopsAsync()
        {
            var shops = await _uow.Shops.GetAllAsync();
            var result = new List<object>();
            foreach (var s in shops.OrderByDescending(s => s.CreatedAt))
            {
                var sp = await _uow.Shops.GetByIdWithProductsAsync(s.Id);
                var owner = await _uow.Accounts.GetByIdAsync(s.OwnerId);
                result.Add(new { s.Id, s.OwnerId, OwnerUsername = owner?.Username ?? "", s.ShopName, s.Description,
                    s.IsActive, WalletBalance = owner?.WalletBalance ?? 0, ProductCount = sp?.Products.Count ?? 0, s.CreatedAt });
            }
            return result;
        }

        public async Task<string> ChangeUserRoleAsync(Guid userId, string newRole)
        {
            var account = await _uow.Accounts.GetByIdAsync(userId);
            if (account == null) throw new ArgumentException("User not found.");
            var valid = new[] { "Customer", "ShopOwner", "Admin", "Staff", "SystemStaff", "Shipper" };
            if (!valid.Contains(newRole)) throw new ArgumentException("Invalid role.");
            account.Role = newRole;
            account.UpdatedAt = DateTime.UtcNow;
            await _uow.SaveChangesAsync();
            return $"User role updated to '{newRole}'.";
        }

        // === System Wallet ===

        public async Task<List<object>> GetSystemWalletsAsync()
        {
            var wallets = await _uow.SystemWallets.GetAllAsync();
            return wallets.Select(w => (object)new
            {
                w.Id,
                w.WalletType,
                w.Balance,
                w.Description,
                w.CreatedAt,
                w.UpdatedAt
            }).ToList();
        }

        public async Task<List<object>> GetSystemWalletTransactionsAsync(string? walletType, int count = 50)
        {
            List<SystemWalletTransaction> transactions;
            
            if (!string.IsNullOrEmpty(walletType))
                transactions = await _uow.SystemWalletTransactions.GetByWalletTypeAsync(walletType);
            else
                transactions = await _uow.SystemWalletTransactions.GetRecentAsync(count);

            return transactions.Select(t => (object)new
            {
                t.Id,
                t.WalletType,
                t.Amount,
                t.TransactionType,
                t.BalanceAfter,
                t.OrderId,
                t.RelatedUserId,
                t.Description,
                t.CreatedAt
            }).ToList();
        }
    }
}
