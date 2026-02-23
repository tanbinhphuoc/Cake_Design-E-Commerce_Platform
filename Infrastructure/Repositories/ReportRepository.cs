using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class ReportRepository : GenericRepository<Report>, IReportRepository
    {
        public ReportRepository(AppDbContext context) : base(context) { }

        public async Task<List<Report>> GetReportsWithReporterAsync(string? statusFilter = null)
        {
            var query = _dbSet.Include(r => r.Reporter).AsQueryable();

            if (!string.IsNullOrWhiteSpace(statusFilter))
                query = query.Where(r => r.Status == statusFilter);

            return await query
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }
    }
}
