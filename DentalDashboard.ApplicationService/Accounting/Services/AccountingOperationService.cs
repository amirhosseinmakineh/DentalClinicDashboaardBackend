using DentalDashboard.ApplicationService.Contract.Accounting;
using DentalDashboard.Domain.Accounting;

namespace DentalDashboard.ApplicationService.Accounting.Services;

public sealed class AccountingOperationService(IAccountingAccess access, IEnumerable<IAccountingOperationExecutor> executors,
    IAccountingAuditScope audit)
{
    public async Task<AccountingOperationResponse> ExecuteAsync(AccountingOperationCommand command, CancellationToken ct)
    {
        var executor = executors.SingleOrDefault(x => x.Permissions.ContainsKey(command.Operation));
        if (executor is null) throw new AccountingValidationException("عملیات حسابداری معتبر نیست.");
        if (!await access.AllowsAsync(command.Actor.UserId, executor.Permissions[command.Operation], ct)) throw new AccountingForbiddenException();
        audit.ActorUserId = command.Actor.UserId;
        audit.Operation = command.Operation;
        try {
            return await executor.ExecuteAsync(command.Operation, command.Request.TargetId, command.Request.Data, command.Actor.UserId, ct);
        } finally { audit.ActorUserId = null; audit.Operation = ""; }
    }
}
public sealed class AccountingPermissionService(IAccountingAccess access, IAccountingAccessRepository repository, IAccountingAuditScope audit)
{
    public async Task<AccountingOperationResponse> SetAsync(SetAccountantPermissionsCommand command, CancellationToken ct)
    {
        if ((await access.GetAsync(command.Actor.UserId, ct))?.IsAdmin != true) throw new AccountingForbiddenException();
        if (command.Permissions is null || command.Permissions.Count > AccountingPermissions.All.Count ||
            command.Permissions.Any(x => !AccountingPermissions.All.Contains(x))) throw new AccountingValidationException("مجوز حسابداری معتبر نیست.");
        audit.ActorUserId = command.Actor.UserId; audit.Operation = "SetPermissions";
        try {
            if (!await repository.ReplaceAsync(command.UserId, command.Actor.UserId, command.Permissions.Distinct().ToArray(), ct))
                throw new AccountingValidationException("کاربر حسابدار فعال پیدا نشد.");
            return new("دسترسی حسابدار ذخیره شد.");
        } finally { audit.ActorUserId = null; audit.Operation = ""; }
    }
}
