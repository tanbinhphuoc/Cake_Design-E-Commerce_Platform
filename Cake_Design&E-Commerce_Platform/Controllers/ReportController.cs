using Application.DTOs;
using Domain.Entities;
using Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Cake_Design_E_Commerce_Platform.Controllers
{
    [ApiController]
    [Route("api")]
    public class ReportController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ReportController(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// User reports a product or shop.
        /// </summary>
        [HttpPost("report")]
        [Authorize]
        public async Task<IActionResult> CreateReport([FromBody] CreateReportDto dto)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            if (string.IsNullOrWhiteSpace(dto.TargetType) ||
                (dto.TargetType != "Product" && dto.TargetType != "Shop"))
                return BadRequest(new { Message = "TargetType must be 'Product' or 'Shop'." });

            if (string.IsNullOrWhiteSpace(dto.Reason))
                return BadRequest(new { Message = "Reason is required." });

            // Verify target exists
            if (dto.TargetType == "Product")
            {
                var product = await _context.Products.FindAsync(dto.TargetId);
                if (product == null)
                    return BadRequest(new { Message = "Product not found." });
            }
            else
            {
                var shop = await _context.Shops.FindAsync(dto.TargetId);
                if (shop == null)
                    return BadRequest(new { Message = "Shop not found." });
            }

            var report = new Report
            {
                Id = Guid.NewGuid(),
                ReporterId = userId.Value,
                TargetType = dto.TargetType,
                TargetId = dto.TargetId,
                Reason = dto.Reason,
                Description = dto.Description ?? string.Empty,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };

            _context.Reports.Add(report);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Report submitted successfully.", ReportId = report.Id });
        }

        // ===== Admin Report Management =====

        /// <summary>
        /// Admin gets all reports.
        /// </summary>
        [HttpGet("admin/reports")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetReports([FromQuery] string? status)
        {
            var query = _context.Reports
                .Include(r => r.Reporter)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(r => r.Status == status);

            var reports = await query
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new ReportDto
                {
                    Id = r.Id,
                    ReporterId = r.ReporterId,
                    ReporterUsername = r.Reporter.Username,
                    TargetType = r.TargetType,
                    TargetId = r.TargetId,
                    Reason = r.Reason,
                    Description = r.Description,
                    Status = r.Status,
                    AdminNote = r.AdminNote,
                    CreatedAt = r.CreatedAt,
                    ResolvedAt = r.ResolvedAt
                })
                .ToListAsync();

            return Ok(reports);
        }

        /// <summary>
        /// Admin updates report status.
        /// </summary>
        [HttpPut("admin/reports/{id:guid}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateReport(Guid id, [FromBody] UpdateReportDto dto)
        {
            var report = await _context.Reports.FindAsync(id);
            if (report == null)
                return NotFound(new { Message = "Report not found." });

            var validStatuses = new[] { "Reviewed", "Resolved", "Dismissed" };
            if (!validStatuses.Contains(dto.Status))
                return BadRequest(new { Message = "Invalid status. Must be: Reviewed, Resolved, or Dismissed." });

            report.Status = dto.Status;
            if (dto.AdminNote != null)
                report.AdminNote = dto.AdminNote;

            if (dto.Status == "Resolved" || dto.Status == "Dismissed")
                report.ResolvedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { Message = "Report updated." });
        }

        private Guid? GetUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                return null;
            return userId;
        }
    }
}
