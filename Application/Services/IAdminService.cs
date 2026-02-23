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
    }
}
