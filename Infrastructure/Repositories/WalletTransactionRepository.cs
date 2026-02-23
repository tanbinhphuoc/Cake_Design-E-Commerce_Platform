using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class WalletTransactionRepository : GenericRepository<WalletTransaction>, IWalletTransactionRepository
    {
        public WalletTransactionRepository(AppDbContext context) : base(context) { }

        public async Task<List<WalletTransaction>> GetByOwnerAsync(Guid ownerId, string walletType)
        {
            return await _dbSet
                .Where(t => t.WalletOwnerId == ownerId && t.WalletType == walletType)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }
    }
}
