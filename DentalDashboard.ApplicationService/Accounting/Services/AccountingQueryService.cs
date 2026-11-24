using DentalDashboard.ApplicationService.Contract.Accounting;
using DentalDashboard.ApplicationService.Contract.Secretary.Accountant.PatientFinance.Queries;
using DentalDashboard.ApplicationService.Contract.Secretary.Accountant.Queries;
using DentalDashboard.Domain.Accounting;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;

namespace DentalDashboard.ApplicationService.Accounting.Services;

public static class AccountingFilterRules
{
    public static void Validate(AccountingFilter filter)
    {
        if (filter.Page < 1 || filter.Page > 100_000 || filter.PageSize is < 1 or > 100)
            throw new AccountingValidationException("صفحه و تعداد ردیف‌ها معتبر نیست.");
        if (filter.FromDate > filter.ToDate || filter.ToDate == DateOnly.MaxValue)
            throw new AccountingValidationException("بازه تاریخ معتبر نیست.");
        if (filter.Search?.Length > 200 || filter.Status?.Length > 60)
            throw new AccountingValidationException("متن فیلتر بیش از حد طولانی است.");
    }
}

public sealed class AccountingQueryService(IAccountingAccess access, IEnumerable<IAccountingReadRepository> repositories,
    IQueryDispatcher queries, IAccountingAccessRepository accessRepository)
{
    public async Task<AccountingPage> PageAsync(AccountingPageQuery query, CancellationToken ct)
    {
        await RequireAsync(query.Actor, query.Section == AccountingSection.Audit ? AccountingPermissions.ViewAudit : AccountingPermissions.View, ct);
        AccountingFilterRules.Validate(query.Filter);
        var repository = repositories.SingleOrDefault(x => x.Supports(query.Section))
            ?? throw new AccountingValidationException("بخش حسابداری معتبر نیست.");
        return await repository.ReadAsync(query.Section, query.Filter, ct);
    }


    public async Task<AccountingOverview> OverviewAsync(AccountingOverviewQuery query, CancellationToken ct)
    {
        await RequireAsync(query.Actor, AccountingPermissions.View, ct);
        AccountingFilterRules.Validate(query.Filter);
        var metrics = new List<AccountingMetric>();
        foreach (var repository in repositories) metrics.AddRange(await repository.MetricsAsync(query.Filter, ct));
        return new(metrics);
    }


    public async Task<object?> DetailsAsync(AccountingCaseDetailsQuery query, CancellationToken ct)
    {
        await RequireAsync(query.Actor, AccountingPermissions.View, ct);
        return await queries.DispatchAsync(new GetPatientFinancialCaseDetailsQuery(query.CaseId), ct);
    }


    public async Task<IReadOnlyList<AccountantUser>> UsersAsync(AccountingUsersQuery query, CancellationToken ct)
    {
        if ((await access.GetAsync(query.Actor.UserId, ct))?.IsAdmin != true) throw new AccountingForbiddenException();
        return await accessRepository.UsersAsync(ct);
    }


    public async Task<object?> TransactionAsync(AccountingTransactionQuery query, CancellationToken ct)
    {
        await RequireAsync(query.Actor, AccountingPermissions.View, ct);
        var result = await queries.DispatchAsync(new GetSecretaryFinancialTransactionDetailsQuery { Id = query.Id }, ct);
        return result?.IsSuccess == true ? result.Data : null;
    }


    private async Task RequireAsync(AccountingActor actor, string permission, CancellationToken ct)
    {
        if (!await access.AllowsAsync(actor.UserId, permission, ct)) throw new AccountingForbiddenException();
    }
}
