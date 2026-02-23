using Application.DTOs;

namespace Application.Services
{
    public interface IReportService
    {
        Task<Guid> CreateReportAsync(Guid userId, CreateReportDto dto);
        Task<List<ReportDto>> GetReportsAsync(string? status);
        Task<string> UpdateReportAsync(Guid reportId, UpdateReportDto dto);
    }
}
