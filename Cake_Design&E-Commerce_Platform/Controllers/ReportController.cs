using Application.DTOs;
using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Cake_Design_E_Commerce_Platform.Controllers
{
    [ApiController]
    [Route("api")]
    public class ReportController : ControllerBase
    {
        private readonly IReportService _reportService;
        public ReportController(IReportService reportService) { _reportService = reportService; }

        [HttpPost("report"), Authorize]
        public async Task<IActionResult> CreateReport([FromBody] CreateReportDto dto)
        {
            var userId = GetUserId(); if (userId == null) return Unauthorized();
            try { var id = await _reportService.CreateReportAsync(userId.Value, dto); return Ok(new { Message = "Report submitted.", ReportId = id }); }
            catch (ArgumentException ex) { return BadRequest(new { ex.Message }); }
        }

        [HttpGet("admin/reports"), Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetReports([FromQuery] string? status) => Ok(await _reportService.GetReportsAsync(status));

        [HttpPut("admin/reports/{id:guid}"), Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateReport(Guid id, [FromBody] UpdateReportDto dto)
        {
            try { return Ok(new { Message = await _reportService.UpdateReportAsync(id, dto) }); }
            catch (ArgumentException ex) { return NotFound(new { ex.Message }); }
        }

        private Guid? GetUserId()
        {
            var c = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return !string.IsNullOrEmpty(c) && Guid.TryParse(c, out var id) ? id : null;
        }
    }
}
