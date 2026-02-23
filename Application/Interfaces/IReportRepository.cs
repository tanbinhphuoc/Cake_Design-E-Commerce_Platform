using Domain.Entities;

namespace Application.Interfaces
{
    public interface IReportRepository : IGenericRepository<Report>
    {
        Task<List<Report>> GetReportsWithReporterAsync(string? statusFilter = null);
    }
}
