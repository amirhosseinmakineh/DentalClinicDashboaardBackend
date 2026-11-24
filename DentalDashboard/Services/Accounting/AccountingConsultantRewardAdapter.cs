using DentalDashboard.ApplicationService.Contract.Accounting;

namespace DentalDashboard.Services.Accounting;

// An adapter to the existing versioned service-reward implementation; keeps
// rate snapshots, selected recipients and legacy rewards on their original flow.
public sealed class AccountingConsultantRewardAdapter(ConsultantRewardService rewards) : IAccountingConsultantRewards
{
    public async Task<AccountingOperationResponse> ReviewAsync(long reservationId, Guid actor, bool approved, string? reason, CancellationToken ct)
    {
        var result = await rewards.ReviewAsync(reservationId, actor, approved, ct, reason);
        if (!result.Success) throw new AccountingValidationException(result.Message);
        return new(result.Message);
    }
}
