using Domain.Entities;

namespace Application.Interfaces
{
    public interface ISystemWalletTransactionRepository : IGenericRepository<SystemWalletTransaction>
    {
        Task<List<SystemWalletTransaction>> GetByWalletTypeAsync(string walletType);
        Task<List<SystemWalletTransaction>> GetByOrderIdAsync(Guid orderId);
        Task<List<SystemWalletTransaction>> GetRecentAsync(int count = 50);
    }
}
