using DentalDashboard.ApplicationService.Contract.Accounting;
using DentalDashboard.Domain.Accounting;

namespace DentalDashboard.ApplicationService.Accounting.Services;

public sealed class AccountingAccessService(IAccountingAccessRepository repository) : IAccountingAccess
{
    public Task<AccountingAccess?> GetAsync(Guid userId, CancellationToken ct = default) => repository.GetAsync(userId, ct);
    public async Task<bool> AllowsAsync(Guid userId, string permission, CancellationToken ct = default)
    {
        var access = await GetAsync(userId, ct);
        return access is not null && (access.IsAdmin || access.Permissions.Contains(permission));
    }
}
