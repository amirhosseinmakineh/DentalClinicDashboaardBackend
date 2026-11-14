using System.Data;
using System.Security.Claims;
using DentalDashboard.Domain.Secretary.Accountant.PatientFinance.Entities;
using DentalDashboard.Domain.Secretary.Accountant.PatientFinance.Enums;
using DentalDashboard.Infrastracture.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.Controllers;

public sealed record InstallmentInput(decimal Amount, DateTime DueDate);
public sealed record ReplaceInstallmentsRequest(List<InstallmentInput> Installments);

[ApiController]
[Authorize(Roles = "Admin,Secretary")]
[Route("api/secretary/patient-financial-cases/{caseId:guid}/installments")]
public sealed class PatientInstallmentsController(DentalContext db) : ControllerBase
{
    private Guid? Actor => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ??
        User.FindFirstValue("userId") ?? User.FindFirstValue("Id"), out var id) ? id : null;

    [HttpGet]
    public async Task<IActionResult> List(Guid caseId, CancellationToken ct)
    {
        if (!await db.PatientFinancialCases.AnyAsync(x => x.Id == caseId, ct)) return NotFound();
        var rows = await db.PatientInstallments.AsNoTracking().Where(x => x.PatientFinancialCaseId == caseId)
            .OrderBy(x => x.DueDate).ThenBy(x => x.Id)
            .Select(x => new { x.Id, x.Amount, x.DueDate, x.PaidAtUtc, x.PaidByUserId }).ToListAsync(ct);
        return Ok(rows);
    }

    [HttpPut]
    public async Task<IActionResult> Replace(Guid caseId, ReplaceInstallmentsRequest request, CancellationToken ct)
    {
        if (Actor is null) return Unauthorized();
        if (request.Installments is null || request.Installments.Count > 120 ||
            request.Installments.Any(x => x.Amount <= 0 || x.Amount > 999999999999m ||
                decimal.Round(x.Amount, 2) != x.Amount || x.DueDate == default))
            return BadRequest(new { message = "تاریخ یا مبلغ اقساط معتبر نیست." });
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var financialCase = await db.PatientFinancialCases.SingleOrDefaultAsync(x => x.Id == caseId, ct);
        if (financialCase is null) return NotFound();
        var existing = await db.PatientInstallments.Where(x => x.PatientFinancialCaseId == caseId).ToListAsync(ct);
        if (existing.Any(x => x.PaidAtUtc.HasValue))
            return Conflict(new { message = "اقساط پرداخت‌شده قابل جایگزینی نیستند." });
        var otherCommitments = await db.PatientCheques.Where(x => x.PatientFinancialCaseId == caseId && !x.IsGuarantee).SumAsync(x => (decimal?)x.Amount, ct) ?? 0m;
        otherCommitments += await db.PatientPromissoryNotes.Where(x => x.PatientFinancialCaseId == caseId && !x.IsGuarantee).SumAsync(x => (decimal?)x.Amount, ct) ?? 0m;
        if (request.Installments.Sum(x => x.Amount) + otherCommitments + financialCase.PrePaymentAmount > financialCase.TotalAmount)
            return BadRequest(new { message = "مجموع اقساط و اسناد پرداخت از مبلغ قابل دریافت بیشتر است." });
        db.PatientInstallments.RemoveRange(existing);
        db.PatientInstallments.AddRange(request.Installments.Select(x => new PatientInstallment {
            PatientFinancialCaseId = caseId, Amount = x.Amount, DueDate = x.DueDate
        }));
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return Ok(new { message = "جدول اقساط ذخیره شد." });
    }

    [HttpPost("{id:long}/pay")]
    public async Task<IActionResult> Pay(Guid caseId, long id, CancellationToken ct)
    {
        if (Actor is not { } actor) return Unauthorized();
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var row = await db.PatientInstallments.SingleOrDefaultAsync(x => x.Id == id && x.PatientFinancialCaseId == caseId, ct);
        if (row is null) return NotFound();
        if (row.PaidAtUtc.HasValue) return Conflict(new { message = "این قسط قبلاً پرداخت شده است." });
        row.PaidAtUtc = DateTime.UtcNow;
        row.PaidByUserId = actor;
        db.PatientFinancialTransactions.Add(new PatientFinancialTransaction {
            PatientFinancialCaseId = caseId, Amount = row.Amount,
            Type = PatientFinancialTransactionType.Payment,
            SourceType = PatientFinancialTransactionSourceType.Installment,
            SourceId = row.Id, CreatedByUserId = actor
        });
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return Ok(new { row.Id, row.PaidAtUtc, row.PaidByUserId });
    }
}
