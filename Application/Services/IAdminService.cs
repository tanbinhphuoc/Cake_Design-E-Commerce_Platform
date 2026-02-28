using Application.DTOs;

namespace Application.Services
{
    public interface IAdminService
    {
        Task<AdminStatsDto> GetStatsAsync();
        Task<object> UpdateWalletAsync(UpdateWalletDto dto);
        Task<List<object>> GetPendingShopsAsync();
        Task<List<object>> GetAllUsersAsync(string? role);
        Task<List<object>> GetAllShopsAsync();
        Task<string> ChangeUserRoleAsync(Guid userId, string newRole);
        
        // System Wallet
        Task<List<object>> GetSystemWalletsAsync();
        Task<List<object>> GetSystemWalletTransactionsAsync(string? walletType, int count = 50);
    }
}
