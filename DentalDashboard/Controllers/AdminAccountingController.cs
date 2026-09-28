using DentalDashboard.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DentalDashboard.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/admin/accounting")]
public sealed class AdminAccountingController(AdminAccountingReportService service) : ControllerBase
{
    [HttpGet("report")]
    public async Task<ActionResult<AdminAccountingReport>> Report(
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] string? search,
        CancellationToken cancellationToken)
    {
        if (fromDate.HasValue && toDate.HasValue && fromDate.Value.Date > toDate.Value.Date)
            return BadRequest(new { message = "تاریخ شروع نمی‌تواند بعد از تاریخ پایان باشد." });

        return Ok(await service.GetAsync(fromDate, toDate, search, cancellationToken));
    }

    [HttpGet("report/export")]
    public async Task<IActionResult> Export(
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] string? search,
        CancellationToken cancellationToken)
    {
        if (fromDate.HasValue && toDate.HasValue && fromDate.Value.Date > toDate.Value.Date)
            return BadRequest(new { message = "تاریخ شروع نمی‌تواند بعد از تاریخ پایان باشد." });

        var report = await service.GetAsync(fromDate, toDate, search, cancellationToken);
        return File(
            AdminAccountingReportService.ExportCsv(report),
            "text/csv; charset=utf-8",
            $"admin-accounting-{DateTime.UtcNow:yyyy-MM-dd}.csv");
    }
}
