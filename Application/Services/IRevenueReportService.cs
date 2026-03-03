using Application.DTOs;

namespace Application.Services
{
    public interface IRevenueReportService
    {
        Task<ShopRevenueDto> GetShopRevenueAsync(Guid ownerId);
        Task<SystemRevenueDto> GetSystemRevenueAsync();
    }
}
