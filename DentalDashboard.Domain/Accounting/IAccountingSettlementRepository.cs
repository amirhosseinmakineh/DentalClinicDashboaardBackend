using DentalDashboard.Domain.PatientReferrals;
using DentalDashboard.Domain.Secretary.Accountant.PatientFinance.Entities;

namespace DentalDashboard.Domain.Accounting;

public interface IAccountingSettlementRepository
{
    Task<PatientReferral?> ReferralAsync(long id, CancellationToken ct);
    Task<bool> ReservationServedAsync(long id, CancellationToken ct);
    Task<bool> ReferralRewardExistsAsync(long id, CancellationToken ct);
    Task<PatientWallet> PatientWalletAsync(Guid userId, CancellationToken ct);
    void AddReferralReward(PatientWalletTransaction transaction);
    Task<PatientFinancialCase?> CaseForInstallmentAsync(long id, CancellationToken ct);
    Task<PatientFinancialCase?> CaseForGuaranteeAsync(long id, CancellationToken ct);
    void AddPayment(PatientFinancialTransaction transaction);
}
