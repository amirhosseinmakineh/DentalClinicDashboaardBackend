using DentalDashboard.Domain.Accounting;
using DentalDashboard.Utilities.Time;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.Infrastracture.Accounting.Repositories;

public abstract class AccountingReadRepository : IAccountingReadRepository
{
    public abstract bool Supports(AccountingSection section);
    protected abstract IQueryable<AccountingRow> Rows(AccountingSection section);
    public virtual Task<IReadOnlyList<AccountingMetric>> MetricsAsync(AccountingFilter filter, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<AccountingMetric>>([]);

    public static IQueryable<AccountingRow> Filter(IQueryable<AccountingRow> query, AccountingFilter filter)
    {
        if (filter.FromDate is { } from) {
            var (start, _) = IranTimeHelper.GetIranDayRangeAsUtc(from);
            query = query.Where(x => x.Date >= start);
        }
        if (filter.ToDate is { } to) {
            var (_, end) = IranTimeHelper.GetIranDayRangeAsUtc(to);
            query = query.Where(x => x.Date < end);
        }
        if (!string.IsNullOrWhiteSpace(filter.Search)) {
            var term = filter.Search.Trim();
            query = query.Where(x => x.Person.Contains(term) || x.Description.Contains(term) ||
                (x.Number != null && x.Number.Contains(term)) || (x.Owner != null && x.Owner.Contains(term)));
        }
        if (!string.IsNullOrWhiteSpace(filter.Status)) query = query.Where(x => x.Status == filter.Status);
        if (filter.UserId is { } userId) query = query.Where(x => x.UserId == userId);
        if (filter.CaseId is { } caseId) query = query.Where(x => x.CaseId == caseId);
        return query;
    }
    protected virtual void Enrich(IReadOnlyList<AccountingRow> items) { }
    public async Task<AccountingPage> ReadAsync(AccountingSection section, AccountingFilter filter, CancellationToken ct)
    {
        var query = Filter(Rows(section), filter);
        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(x => x.Date).ThenByDescending(x => x.Id)
            .Skip((filter.Page - 1) * filter.PageSize).Take(filter.PageSize).ToListAsync(ct);
        foreach (var item in items) {
            // DateTime.Kind is lost by SQL datetime2; dates represent UTC instants.
            if (item.Date.HasValue) item.Date = DateTime.SpecifyKind(item.Date.Value, DateTimeKind.Utc);
        }
        Enrich(items);
        return new(items, total, filter.Page, filter.PageSize);
    }
    protected async Task<AccountingMetric> Metric(AccountingSection section, AccountingFilter filter, string key, CancellationToken ct)
    {
        var query = Filter(Rows(section), filter);
        return new(key, await query.SumAsync(x => (decimal?)x.Amount, ct) ?? 0, await query.CountAsync(ct));
    }
}
