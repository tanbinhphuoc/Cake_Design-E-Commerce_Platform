using Domain.Entities;

namespace Application.Interfaces
{
    public interface ISystemWalletRepository : IGenericRepository<SystemWallet>
    {
        Task<SystemWallet?> GetByTypeAsync(string walletType);
    }
}
