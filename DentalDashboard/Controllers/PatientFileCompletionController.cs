using System.Data;
using System.Security.Claims;
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

public sealed record CompletionCheque(decimal Amount, string Number, string OwnerName, DateOnly? DueDate, bool Installment);
public sealed record CompletionNote(decimal Amount, string Number, DateOnly? DueDate);
public sealed record LinkPatientFinanceRequest(Guid FinancialCaseId);
public sealed record CompletePatientFileRequest(
    Guid PatientId, string PaymentMode, decimal TotalAmount, decimal CashReceived,
    Dictionary<int, int> ServiceUnitCounts, List<CompletionCheque> Cheques, List<CompletionNote> Notes);

public static class PatientCompletionValidation
{
    public static string? Validate(CompletePatientFileRequest r, DateOnly today)
    {
        if (r.PaymentMode is not ("cash" or "installments")) return "روش پرداخت معتبر نیست.";
        if (r.TotalAmount <= 0 || r.TotalAmount > 999_999_999_999m || decimal.Round(r.TotalAmount, 2) != r.TotalAmount ||
            r.CashReceived < 0 || r.CashReceived > r.TotalAmount || decimal.Round(r.CashReceived, 2) != r.CashReceived)
            return "مبلغ درمان یا مبلغ دریافتی معتبر نیست.";
        if (r.ServiceUnitCounts is not { Count: > 0 } || r.ServiceUnitCounts.Any(x => !Enum.IsDefined(typeof(DentalServiceType), x.Key) || x.Value is < 1 or > 100))
            return "خدمات و تعداد واحد آن‌ها را مشخص کنید.";
        if (r.Cheques is null || r.Notes is null || r.Cheques.Count + r.Notes.Count > 100)
            return "اطلاعات اسناد معتبر نیست.";
        if (r.PaymentMode == "cash")
            return r.CashReceived != r.TotalAmount || r.Cheques.Count != 0 || r.Notes.Count != 0
                ? "در پرداخت نقدی باید کل مبلغ درمان دریافت شده باشد و سند اقساط ثبت نشود." : null;
        if (r.Cheques.Count + r.Notes.Count == 0) return "حداقل یک چک یا سفته تحویل‌گرفته‌شده ثبت کنید.";
        if (r.Cheques.Any(x => x is null || x.Amount <= 0 || decimal.Round(x.Amount, 2) != x.Amount ||
            string.IsNullOrWhiteSpace(x.Number) || x.Number.Trim().Length > 32 ||
            string.IsNullOrWhiteSpace(x.OwnerName) || x.OwnerName.Trim().Length > 200 ||
            (x.Installment && (!x.DueDate.HasValue || x.DueDate < today))))
            return "شماره، مبلغ، نام صاحب چک و سررسید چک اقساط را بررسی کنید.";
        if (r.Notes.Any(x => x is null || x.Amount <= 0 || decimal.Round(x.Amount, 2) != x.Amount ||
            string.IsNullOrWhiteSpace(x.Number) || x.Number.Trim().Length > 64 || !x.DueDate.HasValue || x.DueDate < today))
            return "شماره، مبلغ و سررسید سفته را بررسی کنید.";
        if (r.Cheques.Select(x => x.Number.Trim()).Distinct().Count() != r.Cheques.Count ||
            r.Notes.Select(x => x.Number.Trim()).Distinct().Count() != r.Notes.Count)
            return "شماره چک یا سفته تکراری است.";
        return r.CashReceived + r.Cheques.Sum(x => x.Amount) + r.Notes.Sum(x => x.Amount) != r.TotalAmount
            ? "جمع مبلغ دریافتی و چک‌ها و سفته‌ها باید برابر هزینه درمان باشد." : null;
    }
}

[ApiController]
[Authorize(Roles = "Admin,Secretary")]
[Route("api/secretary/patient-files/{patientFileId:long}")]
public sealed class PatientFileCompletionController(DentalContext db) : ControllerBase
{
    private Guid? Actor => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ??
        User.FindFirstValue("userId") ?? User.FindFirstValue("Id"), out var id) ? id : null;

    [HttpPost("link-finance")]
    public async Task<IActionResult> LinkFinance(long patientFileId, LinkPatientFinanceRequest request, CancellationToken ct)
    {
        if (Actor is null) return Unauthorized();
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var file = await db.PatientFiles.SingleOrDefaultAsync(x => x.Id == patientFileId, ct);
        if (file is null) return NotFound();
        var workflow = await db.PatientFileWorkflows.Include(x => x.FinancialCase)
            .SingleOrDefaultAsync(x => x.PatientFileId == patientFileId, ct);
        if (workflow?.ConsultationCompleted != true)
            return BadRequest(new { message = "ابتدا پایان مشاوره را ثبت کنید." });
        if (PatientFileWorkflowRules.HasFinance(workflow))
            return Conflict(new { message = "پرونده مالی قبلاً متصل شده است." });
        var financialCase = await db.PatientFinancialCases.SingleOrDefaultAsync(x => x.Id == request.FinancialCaseId &&
            x.Patient.PhoneNumber == file.PhoneNumber && x.Status != PatientFinancialCaseStatus.Cancelled, ct);
        if (financialCase is null) return BadRequest(new { message = "صورت‌حساب معتبر مربوط به این بیمار یافت نشد." });
        if (await db.PatientFileWorkflows.AnyAsync(x => x.FinancialCaseId == request.FinancialCaseId && x.PatientFileId != patientFileId, ct))
            return Conflict(new { message = "این صورت‌حساب به پرونده دیگری متصل است." });
        workflow.FinancialCase = financialCase;
        workflow.ClearApproval();
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return Ok(new { financialCaseId = financialCase.Id });
    }

    [HttpPost("complete-finance")]
    public async Task<IActionResult> Complete(long patientFileId, CompletePatientFileRequest request, CancellationToken ct)
    {
        if (Actor is not { } actor) return Unauthorized();
        var today = IranTimeHelper.TodayInIran();
        if (PatientCompletionValidation.Validate(request, today) is { } error)
            return BadRequest(new { message = error });
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var file = await db.PatientFiles.SingleOrDefaultAsync(x => x.Id == patientFileId, ct);
        if (file is null) return NotFound(new { message = "پرونده بیمار یافت نشد." });
        var workflow = await db.PatientFileWorkflows.Include(x => x.FinancialCase)
            .SingleOrDefaultAsync(x => x.PatientFileId == patientFileId, ct);
        if (workflow?.ConsultationCompleted != true)
            return BadRequest(new { message = "ابتدا پایان مشاوره را در ویرایش پرونده ثبت کنید." });
        if (PatientFileWorkflowRules.HasFinance(workflow))
            return Conflict(new { message = "بخش مالی این پرونده قبلاً ثبت شده است؛ از صورت‌حساب بیمار برای بررسی و ویرایش استفاده کنید." });
        if (!await db.Users.AnyAsync(x => x.Id == request.PatientId && x.PhoneNumber == file.PhoneNumber &&
            !x.IsDeleted && x.PatientProfile != null && !x.PatientProfile.IsDeleted, ct))
            return BadRequest(new { message = "هویت مالی با پرونده بیمار مطابقت ندارد." });

        var financialCase = new PatientFinancialCase {
            Id = Guid.NewGuid(), PatientId = request.PatientId, CreatedByUserId = actor,
            TotalAmount = request.TotalAmount, PrePaymentAmount = request.CashReceived,
            PaymentMethod = request.PaymentMode == "cash" ? "نقدی" : "اقساط",
            AgreementType = request.PaymentMode == "cash" ? PatientFinancialAgreementType.Deposit : PatientFinancialAgreementType.PrePayment,
            Status = request.PaymentMode == "cash" ? PatientFinancialCaseStatus.Completed : PatientFinancialCaseStatus.Active,
            Services = request.ServiceUnitCounts.Keys.Select(x => (DentalServiceType)x).ToList(),
            ServiceUnitCountsJson = System.Text.Json.JsonSerializer.Serialize(request.ServiceUnitCounts),
            ToothUnitCount = request.ServiceUnitCounts.Values.Sum()
        };
        foreach (var cheque in request.Cheques)
            financialCase.Cheques.Add(new PatientCheque {
                Amount = cheque.Amount, SayadNumber = cheque.Number.Trim(), OwnerName = cheque.OwnerName.Trim(),
                DueDate = IranTimeHelper.GetIranDayRangeAsUtc(cheque.Installment ? cheque.DueDate!.Value : today).StartUtc,
                IsGuarantee = false, Status = PatientChequeStatus.Pending
            });
        foreach (var note in request.Notes)
            financialCase.PromissoryNotes.Add(new PatientPromissoryNote {
                Amount = note.Amount, SerialNumber = note.Number.Trim(),
                DueDate = IranTimeHelper.GetIranDayRangeAsUtc(note.DueDate!.Value).StartUtc,
                Status = PatientPromissoryNoteStatus.Pending
            });
        db.PatientFinancialCases.Add(financialCase);
        workflow.FinancialCase = financialCase;
        workflow.ClearApproval();
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return Ok(new { financialCaseId = financialCase.Id });
    }

    [HttpPost("approve-service")]
    public async Task<IActionResult> Approve(long patientFileId, CancellationToken ct)
    {
        if (Actor is not { } actor) return Unauthorized();
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        if (!await db.PatientFiles.AnyAsync(x => x.Id == patientFileId, ct)) return NotFound();
        var workflow = await db.PatientFileWorkflows
            .Include(x => x.FinancialCase!).ThenInclude(x => x.Cheques)
            .Include(x => x.FinancialCase!).ThenInclude(x => x.PromissoryNotes)
            .Include(x => x.FinancialCase!).ThenInclude(x => x.Transactions)
            .Include(x => x.FinancialCase!).ThenInclude(x => x.Debts)
            .AsSplitQuery().SingleOrDefaultAsync(x => x.PatientFileId == patientFileId, ct);
        if (workflow?.ConsultationCompleted != true || !PatientFileWorkflowRules.HasFinance(workflow))
            return BadRequest(new { message = "مشاوره و ثبت مالی باید قبل از اجازه انجام خدمات تکمیل شوند." });
        var c = workflow.FinancialCase!;
        if (!PatientFileWorkflowRules.CanApprove(workflow))
            return BadRequest(new { message = "مبالغ دریافت‌شده یا اسناد معتبر، هزینه درمان را پوشش نمی‌دهند؛ صورت‌حساب بیمار را بررسی کنید." });
        if (!PatientFileWorkflowRules.IsApproved(workflow)) {
            workflow.ApprovedAtUtc = DateTime.UtcNow;
            workflow.ApprovedByUserId = actor;
            workflow.ApprovedFinanceHash = PatientFileWorkflowRules.FinanceHash(c);
            await db.SaveChangesAsync(ct);
        }
        await transaction.CommitAsync(ct);
        return Ok(new { approvedAtUtc = workflow.ApprovedAtUtc });
    }
}
