using DentalDashboard.Services;
using DentalDashboard.Utilities.Time;
using DentalDashboard.Utilities.Convertor;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DentalDashboard.Domain.Enums;
using DentalDashboard.ApplicationService.Contract.Secretary.Accountant.PatientFinance.Commands;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;

namespace DentalDashboard.Controllers;

[Route("api/admin/reports")]
[ApiController]
public class AdminReportsController : ControllerBase
{
    private readonly LeadCallReportExportService leadCallReportExportService;
    private readonly UsersExportService usersExportService;
    private readonly LeadsExportService leadsExportService;
    private readonly ConsultantsExportService consultantsExportService;
    private readonly ConsultantsDailySummaryService consultantsDailySummaryService;
    private readonly ReservationsExportService reservationsExportService;
    private readonly DailyReservationsReportService dailyReservationsReportService;
    private readonly PatientFinanceAdminReportService patientFinanceAdminReportService;
    private readonly ICommandDispatcher commandDispatcher;

    public AdminReportsController(
        LeadCallReportExportService leadCallReportExportService,
        UsersExportService usersExportService,
        LeadsExportService leadsExportService,
        ConsultantsExportService consultantsExportService,
        ConsultantsDailySummaryService consultantsDailySummaryService,
        ReservationsExportService reservationsExportService,
        DailyReservationsReportService dailyReservationsReportService,
        PatientFinanceAdminReportService patientFinanceAdminReportService,
        ICommandDispatcher commandDispatcher)
    {
        this.leadCallReportExportService = leadCallReportExportService;
        this.usersExportService = usersExportService;
        this.leadsExportService = leadsExportService;
        this.consultantsExportService = consultantsExportService;
        this.consultantsDailySummaryService = consultantsDailySummaryService;
        this.reservationsExportService = reservationsExportService;
        this.dailyReservationsReportService = dailyReservationsReportService;
        this.patientFinanceAdminReportService = patientFinanceAdminReportService;
        this.commandDispatcher = commandDispatcher;
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("patient-finances/{id:guid}")]
    public async Task<IActionResult> UpdatePatientFinance(
        Guid id,
        UpdatePatientFinancialCaseCommand command,
        CancellationToken cancellationToken)
    {
        command.Id = id;
        var result = await commandDispatcher.DispatchAsync(command, cancellationToken);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("patient-finances/{id:guid}")]
    public async Task<IActionResult> DeletePatientFinance(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await commandDispatcher.DispatchAsync(
            new CancelPatientFinancialCaseCommand(id), cancellationToken);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("patient-finances")]
    public async Task<IActionResult> GetPatientFinances(
        [FromQuery] PatientFinanceAdminReportFilter filter,
        CancellationToken cancellationToken)
    {
        if (filter.FromDate.HasValue && filter.ToDate.HasValue && filter.FromDate > filter.ToDate)
            return BadRequest(new { message = "تاریخ شروع نمی‌تواند بعد از تاریخ پایان باشد." });

        return Ok(await patientFinanceAdminReportService.GetAsync(filter, cancellationToken));
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("patient-finances/export")]
    public async Task<IActionResult> ExportPatientFinances(
        [FromQuery] PatientFinanceAdminReportFilter filter,
        CancellationToken cancellationToken)
    {
        if (filter.FromDate.HasValue && filter.ToDate.HasValue && filter.FromDate > filter.ToDate)
            return BadRequest(new { message = "تاریخ شروع نمی‌تواند بعد از تاریخ پایان باشد." });

        var file = await patientFinanceAdminReportService.ExportExcelAsync(filter, cancellationToken);
        return File(
            file,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"patient-finance-report-{TodayPersianFileDate()}.xlsx");
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("daily-reservations")]
    public async Task<IActionResult> GetDailyReservations(
        [FromQuery] DateOnly? date,
        [FromQuery] ReservationOwnerType? reservationOwnerType,
        [FromQuery] long? consultantProfileId,
        [FromQuery] Guid? secretaryUserId,
        [FromQuery] DailyReservationRequestStatus? requestStatus,
        [FromQuery] bool includeAll,
        CancellationToken cancellationToken)
    {
        var validationError = ValidateOwnerFilters(reservationOwnerType, consultantProfileId, secretaryUserId, includeAll);
        if (validationError != null) return BadRequest(new { message = validationError });

        var report = await dailyReservationsReportService.GetAsync(date, reservationOwnerType,
            consultantProfileId, secretaryUserId, requestStatus, includeAll, cancellationToken);
        return Ok(report);
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("daily-reservations/export")]
    public async Task<IActionResult> ExportDailyReservations(
        [FromQuery] DateOnly? date,
        [FromQuery] ReservationOwnerType? reservationOwnerType,
        [FromQuery] long? consultantProfileId,
        [FromQuery] Guid? secretaryUserId,
        [FromQuery] DailyReservationRequestStatus? requestStatus,
        [FromQuery] bool includeAll,
        CancellationToken cancellationToken)
    {
        var validationError = ValidateOwnerFilters(reservationOwnerType, consultantProfileId, secretaryUserId, includeAll);
        if (validationError != null) return BadRequest(new { message = validationError });

        var reportDate = date ?? IranTimeHelper.TodayInIran();
        var file = await dailyReservationsReportService.ExportCsvAsync(
            reportDate, reservationOwnerType, consultantProfileId, secretaryUserId,
            requestStatus, includeAll, cancellationToken);
        var fileDate = includeAll ? "all" : PersianFileDate(reportDate);
        return File(file, "text/csv; charset=utf-8", $"daily-reservations-{fileDate}.csv");
    }

    private static string? ValidateOwnerFilters(
        ReservationOwnerType? ownerType,
        long? consultantProfileId,
        Guid? secretaryUserId,
        bool includeAll)
    {
        if (includeAll) return null;
        if (ownerType == ReservationOwnerType.Consultant && secretaryUserId.HasValue)
            return "secretaryUserId cannot be used with Consultant reservationOwnerType.";
        if (ownerType == ReservationOwnerType.Secretary && consultantProfileId.HasValue)
            return "consultantProfileId cannot be used with Secretary reservationOwnerType.";
        return null;
    }

    [HttpGet("users/export")]
    public async Task<IActionResult> ExportUsers(CancellationToken cancellationToken)
    {
        var file = await usersExportService.ExportCsvAsync(cancellationToken);
        return File(file, "text/csv; charset=utf-8", $"users-report-{TodayPersianFileDate()}.csv");
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("leads")]
    public async Task<IActionResult> GetLeads(
        [FromQuery] LeadReportFilter filter, CancellationToken cancellationToken)
    {
        if (filter.From.HasValue && filter.To.HasValue && filter.From > filter.To)
            return BadRequest(new { message = "from cannot be after to." });

        return Ok(await leadsExportService.GetAsync(filter, cancellationToken));
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("leads/export")]
    public async Task<IActionResult> ExportLeads(
        [FromQuery] LeadReportFilter filter, CancellationToken cancellationToken)
    {
        if (filter.From.HasValue && filter.To.HasValue && filter.From > filter.To)
            return BadRequest(new { message = "from cannot be after to." });

        var file = await leadsExportService.ExportCsvAsync(filter, cancellationToken);
        return File(file, "text/csv; charset=utf-8", $"leads-report-{TodayPersianFileDate()}.csv");
    }

    [HttpGet("consultants/export")]
    public async Task<IActionResult> ExportConsultants(CancellationToken cancellationToken)
    {
        var file = await consultantsExportService.ExportCsvAsync(cancellationToken);
        return File(file, "text/csv; charset=utf-8", $"consultants-report-{TodayPersianFileDate()}.csv");
    }

    [HttpGet("consultants/daily-summary")]
    public async Task<IActionResult> GetConsultantsDailySummary(CancellationToken cancellationToken)
    {
        var items = await consultantsDailySummaryService.GetTodaySummaryAsync(cancellationToken);
        return Ok(new
        {
            date = IranTimeHelper.TodayInIran().ToPersianDate(),
            items
        });
    }

    [HttpGet("lead-call-reports/export")]
    public async Task<IActionResult> ExportLeadCallReports([FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken cancellationToken)
    {
        var toExclusive = to?.Date.AddDays(1) ?? DateTime.Today.AddDays(1);
        var fromInclusive = from?.Date ?? toExclusive.AddDays(-1);
        var file = await leadCallReportExportService.ExportCsvAsync(fromInclusive, toExclusive, cancellationToken);
        return File(file, "text/csv; charset=utf-8", $"lead-call-reports-{PersianFileDate(DateOnly.FromDateTime(fromInclusive))}-{PersianFileDate(DateOnly.FromDateTime(toExclusive.AddDays(-1)))}.csv");
    }

    [HttpGet("reservations/export")]
    public async Task<IActionResult> ExportReservations(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] long? consultantProfileId,
        CancellationToken cancellationToken)
    {
        var file = await reservationsExportService.ExportReservationsCsvAsync(from, to, consultantProfileId, cancellationToken);
        return File(file, "text/csv; charset=utf-8", $"reservations-report-{TodayPersianFileDate()}.csv");
    }

    [HttpGet("consultant-attendance-confirmations/export")]
    public async Task<IActionResult> ExportConsultantAttendanceConfirmations(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] long? consultantProfileId,
        CancellationToken cancellationToken)
    {
        var file = await reservationsExportService.ExportConsultantAttendanceConfirmationsCsvAsync(from, to, consultantProfileId, cancellationToken);
        return File(file, "text/csv; charset=utf-8", $"consultant-attendance-confirmations-{TodayPersianFileDate()}.csv");
    }

    private static string TodayPersianFileDate() => PersianFileDate(IranTimeHelper.TodayInIran());

    private static string PersianFileDate(DateOnly date) => date.ToPersianDate().Replace("/", string.Empty);
}
