using DentalDashboard.Services;
using Microsoft.AspNetCore.Mvc;

namespace DentalDashboard.Controllers;

[Route("api/admin/reports")]
[ApiController]
public class AdminReportsController : ControllerBase
{
    private readonly LeadCallReportExportService leadCallReportExportService;
    private readonly UsersExportService usersExportService;
    private readonly LeadsExportService leadsExportService;
    private readonly ConsultantsExportService consultantsExportService;
    private readonly ReservationsExportService reservationsExportService;

    public AdminReportsController(
        LeadCallReportExportService leadCallReportExportService,
        UsersExportService usersExportService,
        LeadsExportService leadsExportService,
        ConsultantsExportService consultantsExportService,
        ReservationsExportService reservationsExportService)
    {
        this.leadCallReportExportService = leadCallReportExportService;
        this.usersExportService = usersExportService;
        this.leadsExportService = leadsExportService;
        this.consultantsExportService = consultantsExportService;
        this.reservationsExportService = reservationsExportService;
    }

    [HttpGet("users/export")]
    public async Task<IActionResult> ExportUsers(CancellationToken cancellationToken)
    {
        var file = await usersExportService.ExportCsvAsync(cancellationToken);
        return File(file, "text/csv; charset=utf-8", $"users-report-{DateTime.Today:yyyyMMdd}.csv");
    }

    [HttpGet("leads/export")]
    public async Task<IActionResult> ExportLeads(CancellationToken cancellationToken)
    {
        var file = await leadsExportService.ExportCsvAsync(cancellationToken);
        return File(file, "text/csv; charset=utf-8", $"leads-report-{DateTime.Today:yyyyMMdd}.csv");
    }

    [HttpGet("consultants/export")]
    public async Task<IActionResult> ExportConsultants(CancellationToken cancellationToken)
    {
        var file = await consultantsExportService.ExportCsvAsync(cancellationToken);
        return File(file, "text/csv; charset=utf-8", $"consultants-report-{DateTime.Today:yyyyMMdd}.csv");
    }

    [HttpGet("lead-call-reports/export")]
    public async Task<IActionResult> ExportLeadCallReports([FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken cancellationToken)
    {
        var toExclusive = to?.Date.AddDays(1) ?? DateTime.Today.AddDays(1);
        var fromInclusive = from?.Date ?? toExclusive.AddDays(-1);
        var file = await leadCallReportExportService.ExportCsvAsync(fromInclusive, toExclusive, cancellationToken);
        return File(file, "text/csv; charset=utf-8", $"lead-call-reports-{fromInclusive:yyyyMMdd}-{toExclusive.AddDays(-1):yyyyMMdd}.csv");
    }

    [HttpGet("reservations/export")]
    public async Task<IActionResult> ExportReservations(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] long? consultantProfileId,
        CancellationToken cancellationToken)
    {
        var file = await reservationsExportService.ExportReservationsCsvAsync(from, to, consultantProfileId, cancellationToken);
        return File(file, "text/csv; charset=utf-8", $"reservations-report-{DateTime.Today:yyyyMMdd}.csv");
    }

    [HttpGet("consultant-attendance-confirmations/export")]
    public async Task<IActionResult> ExportConsultantAttendanceConfirmations(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] long? consultantProfileId,
        CancellationToken cancellationToken)
    {
        var file = await reservationsExportService.ExportConsultantAttendanceConfirmationsCsvAsync(from, to, consultantProfileId, cancellationToken);
        return File(file, "text/csv; charset=utf-8", $"consultant-attendance-confirmations-{DateTime.Today:yyyyMMdd}.csv");
    }
}
