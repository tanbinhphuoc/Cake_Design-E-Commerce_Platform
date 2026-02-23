using Domain.Entities;

namespace Application.Interfaces
{
    public interface IWalletTransactionRepository : IGenericRepository<WalletTransaction>
    {
        Task<List<WalletTransaction>> GetByOwnerAsync(Guid ownerId, string walletType);
    }
}
