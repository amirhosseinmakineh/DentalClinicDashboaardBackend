using System.Data;
using System.Security.Claims;
using DentalDashboard.Domain.Secretary.Accountant.PatientFinance.Entities;
using DentalDashboard.Domain.Secretary.Accountant.PatientFinance.Enums;
using DentalDashboard.Infrastracture.Context;
using DentalDashboard.Utilities.Time;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.Controllers;

public static class PatientGuaranteeCollectionRules
{
    public static bool CanCollect(decimal guarantee, decimal overdueDebt, decimal remaining) =>
        overdueDebt > 0 && overdueDebt <= guarantee && overdueDebt <= remaining;
}

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/admin/reports/patient-finances/cheques")]
public sealed class PatientGuaranteeCollectionController(DentalContext db) : ControllerBase
{
    [HttpPost("{id:long}/collect-guarantee")]
    public async Task<IActionResult> Collect(long id, CancellationToken ct)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ??
            User.FindFirstValue("userId") ?? User.FindFirstValue("Id"), out var actor)) return Unauthorized();
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var cheque = await db.PatientCheques.Include(x => x.FinancialCase)
            .SingleOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);
        if (cheque is null || cheque.FinancialCase.IsDeleted) return NotFound();
        if (!cheque.IsGuarantee || cheque.Status != PatientChequeStatus.Pending ||
            cheque.FinancialCase.Status != PatientFinancialCaseStatus.Active)
            return Conflict(new { message = "فقط چک ضمانت نگهداری‌شده در پرونده فعال قابل وصول است." });
        var (_, end) = IranTimeHelper.GetIranDayRangeAsUtc(IranTimeHelper.TodayInIran());
        if (cheque.DueDate > end)
            return Conflict(new { message = "تاریخ چک ضمانت هنوز نرسیده است." });
        var debts = await db.PatientDebts.Where(x => x.PatientFinancialCaseId == cheque.PatientFinancialCaseId &&
            !x.IsDeleted && x.Status == PatientDebtStatus.Unpaid && x.DueDate <= end).ToListAsync(ct);
        var amount = debts.Sum(x => x.Amount);
        var paid = await db.PatientFinancialTransactions.Where(x => !x.IsDeleted &&
            x.PatientFinancialCaseId == cheque.PatientFinancialCaseId).SumAsync(x => (decimal?)x.Amount, ct) ?? 0;
        var remaining = cheque.FinancialCase.TotalAmount - cheque.FinancialCase.PrePaymentAmount -
            cheque.FinancialCase.DepositAmount - paid;
        if (!PatientGuaranteeCollectionRules.CanCollect(cheque.Amount, amount, remaining))
            return Conflict(new { message = "باید بدهی سررسیدشده بابت اقساط وصول‌نشده ثبت شده باشد؛ مجموع بدهی نباید از مانده درمان یا مبلغ چک ضمانت بیشتر باشد." });
        // Settle the original failed commitments as well, so they cannot be collected a second time.
        var chequeIds = debts.Where(x => x.SourceType == PatientDebtSourceType.Cheque).Select(x => x.SourceId).ToArray();
        var noteIds = debts.Where(x => x.SourceType == PatientDebtSourceType.PromissoryNote).Select(x => x.SourceId).ToArray();
        var sources = await db.PatientCheques.Where(x => chequeIds.Contains(x.Id) &&
            x.PatientFinancialCaseId == cheque.PatientFinancialCaseId && !x.IsDeleted && !x.IsGuarantee &&
            x.Status == PatientChequeStatus.Unpaid).ToListAsync(ct);
        var notes = await db.PatientPromissoryNotes.Where(x => noteIds.Contains(x.Id) &&
            x.PatientFinancialCaseId == cheque.PatientFinancialCaseId && !x.IsDeleted && !x.IsGuarantee &&
            x.Status == PatientPromissoryNoteStatus.Unpaid).ToListAsync(ct);
        if (sources.Count != chequeIds.Length || notes.Count != noteIds.Length)
            return Conflict(new { message = "اسناد بدهی با وضعیت فعلی پرونده تطابق ندارند؛ ابتدا بدهی‌ها بررسی شوند." });
        foreach (var debt in debts) debt.Status = PatientDebtStatus.Paid;
        foreach (var source in sources) source.Status = PatientChequeStatus.Paid;
        foreach (var note in notes) note.Status = PatientPromissoryNoteStatus.Paid;
        db.PatientFinancialTransactions.Add(new PatientFinancialTransaction {
            PatientFinancialCaseId = cheque.PatientFinancialCaseId, Amount = amount,
            SourceType = PatientFinancialTransactionSourceType.Cheque, SourceId = cheque.Id, CreatedByUserId = actor
        });
        cheque.Status = PatientChequeStatus.Paid;
        if (amount == remaining) cheque.FinancialCase.Status = PatientFinancialCaseStatus.Completed;
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return Ok(new { amount, message = "وصول ضمانت فقط به میزان بدهی سررسیدشده و تسویه اقساط مربوط ثبت شد." });
    }
}
