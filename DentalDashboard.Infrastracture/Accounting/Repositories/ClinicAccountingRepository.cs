using DentalDashboard.Domain.Accounting;
using DentalDashboard.Infrastracture.Context;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.Infrastracture.Accounting.Repositories;

public sealed class ClinicAccountingRepository(DentalContext db) : AccountingReadRepository
{
    public override bool Supports(AccountingSection section) => section is AccountingSection.Transactions or AccountingSection.Categories or AccountingSection.Audit;
    protected override IQueryable<AccountingRow> Rows(AccountingSection section) => section switch {
        AccountingSection.Transactions => db.FinancialTransactions.AsNoTracking().Where(x => !x.IsDeleted).Select(x => new AccountingRow {
            Id = x.Id.ToString(), Person = x.CounterpartyName ?? "", Description = x.Subject ?? "", Amount = x.Amount,
            Status = ((int)x.Type).ToString(), Date = x.TransactionDate, Owner = x.Description, UserId = x.CreatedByUserId ,
                CaseId = null, IsGuarantee = false, Number = null, PaidAmount = null, DetailsJson = null }),
        AccountingSection.Categories => db.ExpenseCategories.AsNoTracking().Where(x => !x.IsDeleted).Select(x => new AccountingRow {
            Id = x.Id.ToString(), Description = x.Title, Status = x.IsActive ? "Active" : "Inactive", Date = x.CreatedAt ,
                Person = "", Amount = 0, CaseId = null, UserId = null, IsGuarantee = false, Number = null, Owner = null, PaidAmount = null, DetailsJson = null }),
        AccountingSection.Audit => db.Set<AccountingAuditEntry>().AsNoTracking().Select(x => new AccountingRow {
            Id = x.Id.ToString(), Person = db.Users.Where(u => u.Id == x.ActorUserId).Select(u => u.FirstName + " " + u.LastName).FirstOrDefault() ?? x.ActorUserId.ToString(),
            Description = x.Operation + " / " + x.EntityType, Status = "Recorded", Date = x.OccurredAtUtc,
            Number = x.EntityId, DetailsJson = x.ChangesJson, UserId = x.ActorUserId ,
                Amount = 0, CaseId = null, IsGuarantee = false, Owner = null, PaidAmount = null }),
        _ => throw new ArgumentOutOfRangeException(nameof(section))
    };
    public override async Task<IReadOnlyList<AccountingMetric>> MetricsAsync(AccountingFilter filter, CancellationToken ct) => [
        await Metric(AccountingSection.Transactions, new() { FromDate = filter.FromDate, ToDate = filter.ToDate, Status = "1" }, "Income", ct),
        await Metric(AccountingSection.Transactions, new() { FromDate = filter.FromDate, ToDate = filter.ToDate, Status = "2" }, "Expense", ct)
    ];
}
