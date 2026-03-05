using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class SystemWalletTransactionRepository : GenericRepository<SystemWalletTransaction>, ISystemWalletTransactionRepository
    {
        public SystemWalletTransactionRepository(AppDbContext context) : base(context) { }

        public async Task<List<SystemWalletTransaction>> GetByWalletTypeAsync(string walletType)
        {
            return await _dbSet
                .Where(t => t.WalletType == walletType)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<SystemWalletTransaction>> GetByOrderIdAsync(Guid orderId)
        {
            return await _dbSet
                .Where(t => t.OrderId == orderId)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<SystemWalletTransaction>> GetRecentAsync(int count = 50)
        {
            return await _dbSet
                .OrderByDescending(t => t.CreatedAt)
                .Take(count)
                .ToListAsync();
        }
    }
}
