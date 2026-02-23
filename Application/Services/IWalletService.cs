using Application.DTOs;

namespace Application.Services
{
    public interface IWalletService
    {
        Task<WalletDto> GetUserWalletAsync(Guid userId);
        Task<WalletDto?> GetShopWalletAsync(Guid ownerId);
        Task<List<WalletTransactionDto>> GetUserTransactionsAsync(Guid userId);
        Task<List<WalletTransactionDto>> GetShopTransactionsAsync(Guid ownerId);
        Task<object> DepositAsync(Guid userId, DepositWalletDto dto);
        Task<PaymentDto> CreatePaymentAsync(Guid userId, CreatePaymentDto dto);
    }
}
