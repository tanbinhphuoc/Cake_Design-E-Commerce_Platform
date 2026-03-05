using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class SystemWalletRepository : GenericRepository<SystemWallet>, ISystemWalletRepository
    {
        public SystemWalletRepository(AppDbContext context) : base(context) { }

        public async Task<SystemWallet?> GetByTypeAsync(string walletType)
        {
            return await _dbSet.FirstOrDefaultAsync(w => w.WalletType == walletType);
        }
    }
}
