using DentalDashboard.Services;
using Microsoft.AspNetCore.Mvc;

namespace DentalDashboard.Controllers;

[Route("api/admin/reports")]
[ApiController]
public class AdminReportsController : ControllerBase
{
    private readonly LeadCallReportExportService exportService;
    public AdminReportsController(LeadCallReportExportService exportService) => this.exportService = exportService;

    [HttpGet("lead-call-reports/export")]
    public async Task<IActionResult> ExportLeadCallReports([FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken cancellationToken)
    {
        var toExclusive = to?.Date.AddDays(1) ?? DateTime.Today.AddDays(1);
        var fromInclusive = from?.Date ?? toExclusive.AddDays(-1);
        var file = await exportService.ExportCsvAsync(fromInclusive, toExclusive, cancellationToken);
        return File(file, "text/csv; charset=utf-8", $"lead-call-reports-{fromInclusive:yyyyMMdd}-{toExclusive.AddDays(-1):yyyyMMdd}.csv");
    }
}
