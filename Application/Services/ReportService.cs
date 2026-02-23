using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;

namespace Application.Services
{
    public class ReportService : IReportService
    {
        private readonly IUnitOfWork _uow;
        public ReportService(IUnitOfWork uow) { _uow = uow; }

        public async Task<Guid> CreateReportAsync(Guid userId, CreateReportDto dto)
        {
            if (dto.TargetType != "Product" && dto.TargetType != "Shop")
                throw new ArgumentException("TargetType must be 'Product' or 'Shop'.");
            if (string.IsNullOrWhiteSpace(dto.Reason))
                throw new ArgumentException("Reason is required.");

            if (dto.TargetType == "Product")
            { if (await _uow.Products.GetByIdAsync(dto.TargetId) == null) throw new ArgumentException("Product not found."); }
            else
            { if (await _uow.Shops.GetByIdAsync(dto.TargetId) == null) throw new ArgumentException("Shop not found."); }

            var report = new Report
            {
                Id = Guid.NewGuid(), ReporterId = userId, TargetType = dto.TargetType,
                TargetId = dto.TargetId, Reason = dto.Reason, Description = dto.Description ?? string.Empty,
                Status = "Pending", CreatedAt = DateTime.UtcNow
            };
            await _uow.Reports.AddAsync(report);
            await _uow.SaveChangesAsync();
            return report.Id;
        }

        public async Task<List<ReportDto>> GetReportsAsync(string? status)
        {
            var reports = await _uow.Reports.GetReportsWithReporterAsync(status);
            return reports.Select(r => new ReportDto
            {
                Id = r.Id, ReporterId = r.ReporterId, ReporterUsername = r.Reporter.Username,
                TargetType = r.TargetType, TargetId = r.TargetId, Reason = r.Reason,
                Description = r.Description, Status = r.Status, AdminNote = r.AdminNote,
                CreatedAt = r.CreatedAt, ResolvedAt = r.ResolvedAt
            }).ToList();
        }

        public async Task<string> UpdateReportAsync(Guid reportId, UpdateReportDto dto)
        {
            var report = await _uow.Reports.GetByIdAsync(reportId);
            if (report == null) throw new ArgumentException("Report not found.");
            var valid = new[] { "Reviewed", "Resolved", "Dismissed" };
            if (!valid.Contains(dto.Status)) throw new ArgumentException("Invalid status.");
            report.Status = dto.Status;
            if (dto.AdminNote != null) report.AdminNote = dto.AdminNote;
            if (dto.Status == "Resolved" || dto.Status == "Dismissed") report.ResolvedAt = DateTime.UtcNow;
            await _uow.SaveChangesAsync();
            return "Report updated.";
        }
    }
}
