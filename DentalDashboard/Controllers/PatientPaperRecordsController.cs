using System.Text.Json;
using DentalDashboard.Domain.Models;
using DentalDashboard.Infrastracture.Context;
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
    [HttpGet]
    public async Task<IActionResult> Get(long patientFileId, CancellationToken ct)
    {
        if (!await db.PatientFiles.AsNoTracking().AnyAsync(x => x.Id == patientFileId, ct))
            return NotFound(new { message = "پرونده بیمار یافت نشد" });

        var record = await db.PatientPaperRecords.AsNoTracking()
            .SingleOrDefaultAsync(x => x.PatientFileId == patientFileId, ct);
        if (record is null)
            return Ok(new PatientPaperRecordRequest(new PatientPaperIntake(), [], JsonDocument.Parse("{}").RootElement.Clone()));

        return Ok(new PatientPaperRecordRequest(
            JsonSerializer.Deserialize<PatientPaperIntake>(record.IntakeJson) ?? new(),
            JsonSerializer.Deserialize<List<PatientPaperTreatment>>(record.TreatmentsJson) ?? [],
            JsonSerializer.Deserialize<JsonElement>(record.DentalChartJson)));
    }

    [HttpPut]
    public async Task<IActionResult> Save(long patientFileId, [FromBody] PatientPaperRecordRequest request, CancellationToken ct)
    {
        if (!await db.PatientFiles.AsNoTracking().AnyAsync(x => x.Id == patientFileId, ct))
            return NotFound(new { message = "پرونده بیمار یافت نشد" });

        if (request.Intake is null || request.Treatments is null)
            return BadRequest(new { message = "اطلاعات پرونده معتبر نیست" });
        if (request.Treatments.Count > 500)
            return BadRequest(new { message = "تعداد ردیف‌های درمان بیش از حد مجاز است" });

        foreach (var row in request.Treatments)
        {
            if (row.Cost < 0 || row.PaidAmount < 0 || row.PaidAmount > row.Cost ||
                string.IsNullOrWhiteSpace(row.DentistName) || row.DentistName.Length > 200 ||
                row.Description?.Length > 2000)
                return BadRequest(new { message = "اطلاعات ردیف درمان معتبر نیست" });
        }

        if (request.Intake.PatientSignature?.Length > 100_000)
            return BadRequest(new { message = "اندازه امضای بیمار بیش از حد مجاز است" });

        var chartJson = request.DentalChart.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null
            ? "{}" : request.DentalChart.GetRawText();
        if (chartJson.Length > 200_000)
            return BadRequest(new { message = "حجم چارت دندان بیش از حد مجاز است" });

        var record = await db.PatientPaperRecords.SingleOrDefaultAsync(x => x.PatientFileId == patientFileId, ct);
        if (record is null)
        {
            record = new PatientPaperRecord { PatientFileId = patientFileId };
            db.PatientPaperRecords.Add(record);
        }

        record.IntakeJson = JsonSerializer.Serialize(request.Intake);
        record.TreatmentsJson = JsonSerializer.Serialize(request.Treatments);
        record.DentalChartJson = chartJson;
        record.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Ok(new { patientFileId, record.UpdatedAtUtc });
    }
}

public sealed record PatientPaperRecordRequest(
    PatientPaperIntake Intake,
    List<PatientPaperTreatment> Treatments,
    JsonElement DentalChart);

public sealed record PatientPaperTreatment(
    DateOnly TreatmentDate,
    string? Description,
    bool HasFollowUp,
    decimal Cost,
    decimal PaidAmount,
    string DentistName)
{
    public decimal RemainingAmount => Cost - PaidAmount;
}
