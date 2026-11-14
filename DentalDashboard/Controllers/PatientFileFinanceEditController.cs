using System.Data;
using System.Security.Claims;
using System.Text.Json;
using DentalDashboard.Domain.Enums;
using DentalDashboard.Domain.Models;
using DentalDashboard.Domain.Secretary.Accountant.PatientFinance.Entities;
using DentalDashboard.Domain.Secretary.Accountant.PatientFinance.Enums;
using DentalDashboard.Infrastracture.Context;
using DentalDashboard.Utilities.Time;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.Controllers;

public sealed record EditPatientFileFinanceRequest(string ExpectedFinanceHash, CompletePatientFileRequest Finance);

public static class PatientFileFinanceEditRules
{
    // SQL datetime values have Unspecified Kind, but financial due dates are stored as UTC.
    public static DateOnly DueDay(DateTime value) => DateOnly.FromDateTime(
        IranTimeHelper.ToIranLocalTime(DateTime.SpecifyKind(value, DateTimeKind.Utc)));
    public static string? BlockReason(PatientFinancialCase c) =>
        c.IsDeleted || c.Status == PatientFinancialCaseStatus.Cancelled ? "صورت‌حساب لغوشده قابل ویرایش نیست." :
        c.Transactions.Any(x => !x.IsDeleted) || c.Debts.Any(x => !x.IsDeleted) ||
        c.Cheques.Any(x => !x.IsDeleted && (x.Status != PatientChequeStatus.Pending || x.IsGuarantee)) ||
        c.PromissoryNotes.Any(x => !x.IsDeleted && x.Status != PatientPromissoryNoteStatus.Pending) ||
        !string.IsNullOrWhiteSpace(c.GuaranteeDocument)
            ? "این صورت‌حساب دارای سابقه وصول، بدهی یا سند ضمانت است؛ اصلاح آن باید از حسابداری مدیر انجام شود." : null;

    public static string? Validate(PatientFinancialCase c, EditPatientFileFinanceRequest request)
    {
        if (BlockReason(c) is { } reason) return reason;
        if (request.ExpectedFinanceHash != PatientFileWorkflowRules.FinanceHash(c))
            return "اطلاعات مالی هم‌زمان تغییر کرده است؛ فرم را ببندید و دوباره باز کنید.";
        var r = request.Finance;
        if (r is null || r.PatientId != c.PatientId) return "هویت بیمار قابل تغییر نیست.";
        if (PatientCompletionValidation.Validate(r, new DateOnly(1900, 1, 1)) is { } error) return error;
        var chequeIds = r.Cheques.Where(x => x.Id.HasValue).Select(x => x.Id!.Value).ToArray();
        var noteIds = r.Notes.Where(x => x.Id.HasValue).Select(x => x.Id!.Value).ToArray();
        if (chequeIds.Distinct().Count() != chequeIds.Length || noteIds.Distinct().Count() != noteIds.Length ||
            chequeIds.Any(id => !c.Cheques.Any(x => x.Id == id && !x.IsDeleted)) ||
            noteIds.Any(id => !c.PromissoryNotes.Any(x => x.Id == id && !x.IsDeleted)))
            return "شناسه چک یا سفته معتبر نیست یا متعلق به این صورت‌حساب نیست.";
        return null;
    }

    public static void Apply(PatientFileWorkflow workflow, CompletePatientFileRequest r, DateOnly today)
    {
        var c = workflow.FinancialCase!;
        var now = DateTime.UtcNow;
        c.TotalAmount = r.TotalAmount;
        c.PrePaymentAmount = r.CashReceived;
        c.DepositAmount = 0;
        c.PaymentMethod = r.PaymentMode == "cash" ? "نقدی" : "اقساط";
        c.AgreementType = r.PaymentMode == "cash" ? PatientFinancialAgreementType.Deposit : PatientFinancialAgreementType.PrePayment;
        c.Status = r.PaymentMode == "cash" ? PatientFinancialCaseStatus.Completed : PatientFinancialCaseStatus.Active;
        c.Services = r.ServiceUnitCounts.Keys.Select(x => (DentalServiceType)x).ToList();
        c.ServiceUnitCountsJson = JsonSerializer.Serialize(r.ServiceUnitCounts);
        c.ToothUnitCount = r.ServiceUnitCounts.Values.Sum();
        c.UpdatedAt = now;
        foreach (var x in c.Cheques.Where(x => !x.IsDeleted && !r.Cheques.Any(y => y.Id == x.Id))) {
            x.IsDeleted = true; x.DeletedAt = now;
        }
        foreach (var x in c.PromissoryNotes.Where(x => !x.IsDeleted && !r.Notes.Any(y => y.Id == x.Id))) {
            x.IsDeleted = true; x.DeletedAt = now;
        }
        foreach (var draft in r.Cheques) {
            var x = draft.Id.HasValue ? c.Cheques.Single(x => x.Id == draft.Id) : new PatientCheque();
            if (!draft.Id.HasValue) c.Cheques.Add(x);
            x.Amount = draft.Amount; x.SayadNumber = draft.Number.Trim(); x.OwnerName = draft.OwnerName.Trim();
            x.DueDate = IranTimeHelper.GetIranDayRangeAsUtc(draft.Installment ? draft.DueDate!.Value : today).StartUtc;
            x.UpdatedAt = now;
        }
        foreach (var draft in r.Notes) {
            var x = draft.Id.HasValue ? c.PromissoryNotes.Single(x => x.Id == draft.Id) : new PatientPromissoryNote();
            if (!draft.Id.HasValue) c.PromissoryNotes.Add(x);
            x.Amount = draft.Amount; x.SerialNumber = draft.Number.Trim();
            x.DueDate = IranTimeHelper.GetIranDayRangeAsUtc(draft.DueDate!.Value).StartUtc;
            x.UpdatedAt = now;
        }
        workflow.FinanceConfirmedAtUtc = null; workflow.FinanceConfirmedByUserId = null;
        workflow.ProfileConfirmedAtUtc = null; workflow.ProfileConfirmedByUserId = null;
        workflow.ClearApproval();
    }
}

[ApiController]
[Authorize(Roles = "Admin,Secretary")]
[Route("api/secretary/patient-files/{patientFileId:long}/finance-edit")]
public sealed class PatientFileFinanceEditController(DentalContext db, ILogger<PatientFileFinanceEditController> logger) : ControllerBase
{
    private Guid? Actor => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ??
        User.FindFirstValue("userId") ?? User.FindFirstValue("Id"), out var id) ? id : null;
    private Task<PatientFileWorkflow?> Load(long id, CancellationToken ct) => db.PatientFileWorkflows
        .Include(x => x.FinancialCase!).ThenInclude(x => x.Cheques)
        .Include(x => x.FinancialCase!).ThenInclude(x => x.PromissoryNotes)
        .Include(x => x.FinancialCase!).ThenInclude(x => x.Transactions)
        .Include(x => x.FinancialCase!).ThenInclude(x => x.Debts)
        .AsSplitQuery().SingleOrDefaultAsync(x => x.PatientFileId == id && !x.PatientFile.IsDeleted, ct);

    [HttpGet]
    public async Task<IActionResult> Get(long patientFileId, CancellationToken ct)
    {
        if (Actor is null) return Unauthorized();
        var workflow = await Load(patientFileId, ct);
        if (workflow?.FinancialCase is not { } c) return NotFound(new { message = "صورت‌حساب پرونده یافت نشد." });
        if (PatientFileFinanceEditRules.BlockReason(c) is { } reason) return Conflict(new { message = reason });
        var units = JsonSerializer.Deserialize<Dictionary<int, int>>(c.ServiceUnitCountsJson) ?? [];
        if (units.Count == 0) units = c.Services.Distinct().ToDictionary(x => (int)x, _ => 1);
        var finance = new CompletePatientFileRequest(c.PatientId,
            c.PaymentMethod == "نقدی" || (c.PaymentMethod is null && !c.Cheques.Any(x => !x.IsDeleted) && !c.PromissoryNotes.Any(x => !x.IsDeleted)) ? "cash" : "installments",
            c.TotalAmount, c.PrePaymentAmount + c.DepositAmount, units,
            c.Cheques.Where(x => !x.IsDeleted).Select(x => new CompletionCheque(x.Amount, x.SayadNumber, x.OwnerName,
                PatientFileFinanceEditRules.DueDay(x.DueDate), true, x.Id)).ToList(),
            c.PromissoryNotes.Where(x => !x.IsDeleted).Select(x => new CompletionNote(x.Amount, x.SerialNumber,
                PatientFileFinanceEditRules.DueDay(x.DueDate), x.Id)).ToList());
        return Ok(new EditPatientFileFinanceRequest(PatientFileWorkflowRules.FinanceHash(c), finance));
    }

    [HttpPut]
    public async Task<IActionResult> Update(long patientFileId, EditPatientFileFinanceRequest request, CancellationToken ct)
    {
        if (Actor is not { } actor) return Unauthorized();
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var workflow = await Load(patientFileId, ct);
        if (workflow?.FinancialCase is not { } c) return NotFound(new { message = "صورت‌حساب پرونده یافت نشد." });
        if (PatientFileFinanceEditRules.Validate(c, request) is { } error) return Conflict(new { message = error });
        PatientFileFinanceEditRules.Apply(workflow, request.Finance, IranTimeHelper.TodayInIran());
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        logger.LogInformation("Patient finance {CaseId} edited by {Actor} for file {FileId}; approvals cleared", c.Id, actor, patientFileId);
        return Ok(new { financialCaseId = c.Id });
    }
}
