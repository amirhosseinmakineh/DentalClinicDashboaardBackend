using System.Security.Claims;
using System.Text.Json;
using DentalDashboard.Domain.Models;
using DentalDashboard.Infrastracture.Context;
using DentalDashboard.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.Controllers;

/// <summary>
/// Paper-form supplement to the existing patient file. Never allocates or edits FileNumber.
/// Existing patient-file create, update, finance and import endpoints remain unchanged.
/// </summary>
[ApiController]
[Authorize(Roles = "Admin,Secretary")]
[Route("api/secretary/patient-files/{patientFileId:long}/paper-record")]
public sealed class PatientPaperRecordsController(DentalContext db) : ControllerBase
{
    private bool TryGetScope(out Guid? secretaryId, out bool isAdmin)
    {
        isAdmin = User.IsInRole("Admin");
        secretaryId = null;
        if (isAdmin) return true;

        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier) ??
                    User.FindFirstValue("userId") ??
                    User.FindFirstValue("Id");
        if (!Guid.TryParse(claim, out var userId)) return false;

        secretaryId = userId;
        return true;
    }

    [HttpGet]
    public async Task<IActionResult> Get(long patientFileId, CancellationToken ct)
    {
        if (!TryGetScope(out var secretaryId, out var isAdmin)) return Unauthorized();
        if (!await db.PatientFiles.AsNoTracking().AnyAsync(x => x.Id == patientFileId, ct))
            return NotFound(new { message = "پرونده بیمار یافت نشد" });

        var record = await db.PatientPaperRecords.AsNoTracking()
            .SingleOrDefaultAsync(x => x.PatientFileId == patientFileId, ct);
        var completed = await db.PatientFileWorkflows.AsNoTracking()
            .Where(x => x.PatientFileId == patientFileId).Select(x => x.ConsultationCompleted).FirstOrDefaultAsync(ct);
        if (record is null)
            return Ok(new PatientPaperRecordRequest(new PatientPaperIntake(), [], JsonSerializer.SerializeToElement(new { }), completed));

        return Ok(new PatientPaperRecordRequest(
            JsonSerializer.Deserialize<PatientPaperIntake>(record.IntakeJson) ?? new(),
            JsonSerializer.Deserialize<List<PatientPaperTreatment>>(record.TreatmentsJson) ?? [],
            JsonSerializer.Deserialize<JsonElement>(record.DentalChartJson), completed));
    }

    [HttpPut]
    public async Task<IActionResult> Save(long patientFileId, [FromBody] PatientPaperRecordRequest request, CancellationToken ct)
    {
        if (!TryGetScope(out _, out _)) return Unauthorized();
        if (!await db.PatientFiles.AsNoTracking().AnyAsync(x => x.Id == patientFileId, ct))
            return NotFound(new { message = "پرونده بیمار یافت نشد" });

        if (request.Intake is null || request.Treatments is null)
            return BadRequest(new { message = "اطلاعات پرونده معتبر نیست" });
        if (request.Treatments.Count > 500)
            return BadRequest(new { message = "تعداد ردیف‌های درمان بیش از حد مجاز است" });

        foreach (var row in request.Treatments)
        {
            if (row is null || row.Cost < 0 || row.PaidAmount < 0 || (row.PaidAmount ?? 0) > (row.Cost ?? 0) ||
                row.DentistName?.Length > 200 ||
                row.Description?.Length > 2000)
                return BadRequest(new { message = "اطلاعات ردیف درمان معتبر نیست" });
        }

        if (request.Intake.PatientSignature?.Length > 100_000)
            return BadRequest(new { message = "اندازه امضای بیمار بیش از حد مجاز است" });

        // A secretary must explicitly confirm a meaningful treatment plan before finance opens.
        // A mere consultation checkbox with an empty treatment plan is not sufficient.
        if (request.ConsultationCompleted == true &&
            (request.Intake.ConsentAccepted != true ||
             request.Treatments.Count == 0 ||
             request.Treatments.All(x => string.IsNullOrWhiteSpace(x.Description)) ||
             request.Treatments.Any(x => string.IsNullOrWhiteSpace(x.Description) || !x.Cost.HasValue || x.Cost <= 0)))
            return BadRequest(new { message = "برای تأیید طرح درمان، رضایت بیمار و حداقل یک خدمت با شرح و هزینه معتبر الزامی است." });

        var chartJson = request.DentalChart.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null
            ? "{}" : request.DentalChart.GetRawText();
        if (chartJson.Length > 200_000)
            return BadRequest(new { message = "حجم چارت دندان بیش از حد مجاز است" });

        await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
        var record = await db.PatientPaperRecords.SingleOrDefaultAsync(x => x.PatientFileId == patientFileId, ct);
        if (record is null)
        {
            record = new PatientPaperRecord { PatientFileId = patientFileId };
            db.PatientPaperRecords.Add(record);
        }

        var intakeJson = JsonSerializer.Serialize(request.Intake);
        var treatmentsJson = JsonSerializer.Serialize(request.Treatments);
        var workflow = await db.PatientFileWorkflows.Include(x => x.FinancialCase).SingleOrDefaultAsync(x => x.PatientFileId == patientFileId, ct);
        if (workflow is null)
        {
            workflow = new PatientFileWorkflow { PatientFileId = patientFileId };
            db.PatientFileWorkflows.Add(workflow);
        }
        var chartChanged = record.DentalChartJson != chartJson;
        var changed = record.TreatmentsJson != treatmentsJson || chartChanged || intakeJson != record.IntakeJson ||
            (request.ConsultationCompleted.HasValue && request.ConsultationCompleted != workflow.ConsultationCompleted);
        if (PatientChartFinanceRules.RevisePlan(workflow, chartJson, chartChanged, changed) is { } revisionError)
            return Conflict(new { message = revisionError });
        if (request.ConsultationCompleted.HasValue)
        {
            workflow.ConsultationCompleted = request.ConsultationCompleted.Value;
            if (!request.ConsultationCompleted.Value) {
                workflow.TreatmentConfirmedAtUtc = null;
                workflow.TreatmentConfirmedByUserId = null;
            }
            else if (changed || workflow.TreatmentConfirmedAtUtc is null)
            {
                var actorClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ??
                    User.FindFirstValue("userId") ?? User.FindFirstValue("Id");
                if (!Guid.TryParse(actorClaim, out var actor)) return Unauthorized();
                workflow.TreatmentConfirmedAtUtc = DateTime.UtcNow;
                workflow.TreatmentConfirmedByUserId = actor;
            }
        }
        record.IntakeJson = intakeJson;
        record.TreatmentsJson = treatmentsJson;
        record.DentalChartJson = chartJson;
        record.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return Ok(new { patientFileId, record.UpdatedAtUtc });
    }
}

public sealed record PatientPaperRecordRequest(
    PatientPaperIntake Intake,
    List<PatientPaperTreatment> Treatments,
    JsonElement DentalChart,
    bool? ConsultationCompleted = null);

public sealed record PatientPaperTreatment(
    DateOnly? TreatmentDate,
    string? Description,
    bool HasFollowUp,
    decimal? Cost,
    decimal? PaidAmount,
    string? DentistName)
{
    public decimal RemainingAmount => (Cost ?? 0) - (PaidAmount ?? 0);
}
