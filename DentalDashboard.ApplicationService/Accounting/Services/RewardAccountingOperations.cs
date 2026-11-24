using System.Text.Json;
using DentalDashboard.ApplicationService.Contract.Accounting;
using DentalDashboard.ApplicationService.Contract.Secretary.Accountant.SecretarySales.Commands;
using DentalDashboard.Domain.Accounting;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;

namespace DentalDashboard.ApplicationService.Accounting.Services;

public sealed record AccountingReviewRequest(bool Approved, string? Reason);
public sealed class RewardAccountingOperations(ICommandDispatcher commands, IAccountingConsultantRewards consultantRewards,
    AccountingSettlementService settlements) : IAccountingOperationExecutor
{
    public IReadOnlyDictionary<string, string> Permissions { get; } = new Dictionary<string, string> {
        ["ReviewSecretary"] = AccountingPermissions.ApproveSecretary,
        ["ReviewConsultant"] = AccountingPermissions.ApproveConsultant,
        ["ReviewReferral"] = AccountingPermissions.ApproveReferral
    };
    public async Task<AccountingOperationResponse> ExecuteAsync(string operation, string? targetId, JsonElement data, Guid actorUserId, CancellationToken ct)
    {
        if (data.ValueKind != JsonValueKind.Object || !data.TryGetProperty("approved", out var approved) || approved.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
            throw new AccountingValidationException("وضعیت تأیید یا رد را مشخص کنید.");
        var request = AccountingPayload.Read<AccountingReviewRequest>(data);
        var id = AccountingPayload.LongId(targetId);
        return operation switch {
            "ReviewSecretary" => request.Approved
                ? AccountingPayload.Result(await commands.DispatchAsync(new ApproveSecretarySaleCommand { SaleId = id, AdminUserId = actorUserId }, ct))
                : AccountingPayload.Result(await commands.DispatchAsync(new RejectSecretarySaleCommand { SaleId = id, AdminUserId = actorUserId }, ct)),
            "ReviewConsultant" => await consultantRewards.ReviewAsync(id, actorUserId, request.Approved, request.Reason, ct),
            "ReviewReferral" => await settlements.ReviewReferralAsync(id, actorUserId, request.Approved, request.Reason, ct),
            _ => throw new AccountingValidationException("عملیات پاداش معتبر نیست.")
        };
    }
}
