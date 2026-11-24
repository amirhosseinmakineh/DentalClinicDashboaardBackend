using DentalDashboard.Domain.Accounting;
using DentalDashboard.Domain.PatientReferrals;
using DentalDashboard.Domain.Secretary.Accountant.PatientFinance.Entities;
using DentalDashboard.Infrastracture.Context;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.Infrastracture.Accounting.Repositories;

public sealed class AccountingSettlementRepository(DentalContext db) : IAccountingSettlementRepository
{
    public Task<PatientReferral?> ReferralAsync(long id, CancellationToken ct) => db.PatientReferrals.SingleOrDefaultAsync(x => x.Id == id, ct);
    public Task<bool> ReservationServedAsync(long id, CancellationToken ct) => db.Reservations.AnyAsync(x => x.Id == id &&
        !x.IsDeleted && !x.IsCanceled && x.PatientReceivedService == true && x.SecretaryApprovedConsultantConfirmation == true, ct);
    public Task<bool> ReferralRewardExistsAsync(long id, CancellationToken ct) => db.PatientWalletTransactions.AnyAsync(x =>
        x.PatientReferralId == id && x.TransactionType == PatientWalletTransactionType.ReferralReward, ct);
    public async Task<PatientWallet> PatientWalletAsync(Guid userId, CancellationToken ct)
    {
        var wallet = await db.PatientWallets.SingleOrDefaultAsync(x => x.PatientUserId == userId, ct);
        if (wallet is not null) return wallet;
        wallet = new PatientWallet { PatientUserId = userId };
        db.Add(wallet);
        return wallet;
    }
    private IQueryable<PatientFinancialCase> Cases => db.PatientFinancialCases.Include(x => x.Cheques)
        .Include(x => x.PromissoryNotes).Include(x => x.Debts).Include(x => x.Transactions).Include(x => x.Installments).AsSplitQuery();
    public Task<PatientFinancialCase?> CaseForInstallmentAsync(long id, CancellationToken ct) => Cases.SingleOrDefaultAsync(x =>
        x.Installments.Any(i => i.Id == id && !i.IsDeleted), ct);
    public Task<PatientFinancialCase?> CaseForGuaranteeAsync(long id, CancellationToken ct) => Cases.SingleOrDefaultAsync(x =>
        x.Cheques.Any(i => i.Id == id && !i.IsDeleted), ct);
    public void AddReferralReward(PatientWalletTransaction transaction) => db.Add(transaction);
    public void AddPayment(PatientFinancialTransaction transaction) => db.Add(transaction);
}
