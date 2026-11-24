using System.Data;
using DentalDashboard.ApplicationService.Contract.Accounting;
using DentalDashboard.Domain.Accounting;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Domain.Models;
using DentalDashboard.Domain.PatientReferrals;
using DentalDashboard.Domain.Secretary.Accountant.PatientFinance.Entities;
using DentalDashboard.Domain.Secretary.Accountant.PatientFinance.Enums;
using DentalDashboard.Utilities.Time;

namespace DentalDashboard.ApplicationService.Accounting.Services;

public sealed class AccountingSettlementService(IAccountingSettlementRepository repository, IUnitOfWork unitOfWork)
{
    private async Task<AccountingOperationResponse> InTransaction(Func<Task<AccountingOperationResponse>> action, CancellationToken ct)
    {
        await unitOfWork.BeginTransactionAsync(ct, IsolationLevel.Serializable);
        try {
            var response = await action();
            await unitOfWork.CommitAsync(ct);
            return response;
        } catch {
            await unitOfWork.RollbackAsync(CancellationToken.None);
            throw;
        }
    }
    public Task<AccountingOperationResponse> ReviewReferralAsync(long id, Guid actor, bool approved, string? reason, CancellationToken ct) => InTransaction(async () => {
        var referral = await repository.ReferralAsync(id, ct) ?? throw new AccountingValidationException("معرفی یافت نشد.");
        if (referral.Status != PatientReferralStatus.ReservedPendingAdminApproval) throw new AccountingValidationException("این معرفی قبلاً بررسی شده یا قابل بررسی نیست.");
        var now = DateTime.UtcNow;
        if (approved) {
            if (referral.ReservationId is not { } reservationId || !await repository.ReservationServedAsync(reservationId, ct))
                throw new AccountingValidationException("ابتدا انجام خدمت بیمار باید توسط منشی تأیید شود.");
            if (await repository.ReferralRewardExistsAsync(id, ct)) throw new AccountingValidationException("پاداش این معرفی قبلاً ثبت شده است.");
            var wallet = await repository.PatientWalletAsync(referral.ReferrerPatientUserId, ct);
            wallet.Balance += referral.RewardAmount; wallet.UpdatedAt = now;
            repository.AddReferralReward(new PatientWalletTransaction { Wallet = wallet, PatientUserId = referral.ReferrerPatientUserId,
                PatientReferralId = referral.Id, Amount = referral.RewardAmount, TransactionType = PatientWalletTransactionType.ReferralReward,
                Description = $"پاداش معرفی بیمار {referral.ReferredFirstName} {referral.ReferredLastName}" });
            referral.Status = PatientReferralStatus.ApprovedRewarded;
        } else {
            if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length > 500) throw new AccountingValidationException("دلیل رد الزامی و حداکثر ۵۰۰ نویسه است.");
            referral.Status = PatientReferralStatus.Rejected; referral.RejectionReason = reason.Trim();
        }
        referral.ReviewedByAdminId = actor; referral.ReviewedAt = referral.UpdatedAt = now;
        return new(approved ? "معرفی تأیید و پاداش ثبت شد." : "معرفی رد شد.");
    }, ct);

    public Task<AccountingOperationResponse> PayInstallmentAsync(long id, Guid actor, CancellationToken ct) => InTransaction(async () => {
        var financialCase = await repository.CaseForInstallmentAsync(id, ct) ?? throw new AccountingValidationException("قسط یافت نشد.");
        var row = financialCase.Installments.Single(x => x.Id == id);
        var paid = financialCase.Transactions.Sum(x => x.Amount);
        if (PatientInstallmentRules.PaymentError(financialCase, row, paid) is { } error) throw new AccountingValidationException(error);
        if (row.PaidAtUtc.HasValue || financialCase.Transactions.Any(x => x.SourceType == PatientFinancialTransactionSourceType.Installment && x.SourceId == id))
            throw new AccountingValidationException("این قسط قبلاً پرداخت شده است.");
        row.PaidAtUtc = DateTime.UtcNow; row.PaidByUserId = actor;
        repository.AddPayment(new PatientFinancialTransaction { PatientFinancialCaseId = financialCase.Id, Amount = row.Amount,
            SourceType = PatientFinancialTransactionSourceType.Installment, SourceId = id, CreatedByUserId = actor });
        if (financialCase.PrePaymentAmount + financialCase.DepositAmount + paid + row.Amount == financialCase.TotalAmount)
            financialCase.Status = PatientFinancialCaseStatus.Completed;
        return new("پرداخت قسط ثبت شد.");
    }, ct);

    public Task<AccountingOperationResponse> CollectGuaranteeAsync(long id, Guid actor, CancellationToken ct) => InTransaction(async () => {
        var financialCase = await repository.CaseForGuaranteeAsync(id, ct) ?? throw new AccountingValidationException("چک ضمانت یافت نشد.");
        var cheque = financialCase.Cheques.Single(x => x.Id == id);
        if (!cheque.IsGuarantee || cheque.Status != PatientChequeStatus.Pending || financialCase.Status != PatientFinancialCaseStatus.Active)
            throw new AccountingValidationException("فقط چک ضمانت نگهداری‌شده در پرونده فعال قابل وصول است.");
        var (_, end) = IranTimeHelper.GetIranDayRangeAsUtc(IranTimeHelper.TodayInIran());
        if (cheque.DueDate > end) throw new AccountingValidationException("تاریخ چک ضمانت هنوز نرسیده است.");
        var debts = financialCase.Debts.Where(x => x.Status == PatientDebtStatus.Unpaid && x.DueDate <= end).ToList();
        var amount = debts.Sum(x => x.Amount);
        var remaining = financialCase.TotalAmount - financialCase.PrePaymentAmount - financialCase.DepositAmount - financialCase.Transactions.Sum(x => x.Amount);
        if (!GuaranteeCollectionRules.CanCollect(cheque.Amount, amount, remaining))
            throw new AccountingValidationException("بدهی سررسیدشده باید مثبت و حداکثر برابر مانده درمان و مبلغ ضمانت باشد.");
        var chequeIds = debts.Where(x => x.SourceType == PatientDebtSourceType.Cheque).Select(x => x.SourceId).ToArray();
        var noteIds = debts.Where(x => x.SourceType == PatientDebtSourceType.PromissoryNote).Select(x => x.SourceId).ToArray();
        var sources = financialCase.Cheques.Where(x => chequeIds.Contains(x.Id) && !x.IsGuarantee && x.Status == PatientChequeStatus.Unpaid).ToList();
        var notes = financialCase.PromissoryNotes.Where(x => noteIds.Contains(x.Id) && !x.IsGuarantee && x.Status == PatientPromissoryNoteStatus.Unpaid).ToList();
        if (sources.Count != chequeIds.Length || notes.Count != noteIds.Length) throw new AccountingValidationException("اسناد بدهی با پرونده تطابق ندارند.");
        foreach (var debt in debts) debt.Status = PatientDebtStatus.Paid;
        foreach (var source in sources) source.Status = PatientChequeStatus.Paid;
        foreach (var note in notes) note.Status = PatientPromissoryNoteStatus.Paid;
        cheque.Status = PatientChequeStatus.Paid;
        repository.AddPayment(new PatientFinancialTransaction { PatientFinancialCaseId = financialCase.Id, Amount = amount,
            SourceType = PatientFinancialTransactionSourceType.Cheque, SourceId = id, CreatedByUserId = actor });
        if (amount == remaining) financialCase.Status = PatientFinancialCaseStatus.Completed;
        return new("وصول ضمانت فقط به میزان بدهی ثبت شد.", new AccountingCollectedAmount(amount));
    }, ct);
}
