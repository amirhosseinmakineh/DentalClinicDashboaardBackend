namespace DentalDashboard.Domain.Accounting;

public enum AccountingSection { Patients, Cheques, Notes, Installments, Debts, PatientPayments, Transactions,
    Categories, SecretarySales, ConsultantWallets, ConsultantRewards, Referrals, SecretaryWallets, PatientWallets,
    SecretaryWalletTransactions, PatientWalletTransactions, Audit }

public sealed class AccountingFilter
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? Search { get; set; }
    public string? Status { get; set; }
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
    public Guid? UserId { get; set; }
    public Guid? CaseId { get; set; }
}

// Property initializers in repository projections keep filtering and paging on SQL Server.
public sealed class AccountingRow
{
    public string Id { get; init; } = "";
    public string Person { get; init; } = "";
    public string Description { get; init; } = "";
    public decimal Amount { get; set; }
    public string Status { get; init; } = "";
    public DateTime? Date { get; set; }
    public Guid? CaseId { get; init; }
    public Guid? UserId { get; init; }
    public bool IsGuarantee { get; init; }
    public string? Number { get; init; }
    public string? Owner { get; init; }
    public decimal? PaidAmount { get; init; }
    [System.Text.Json.Serialization.JsonIgnore]
    public decimal LegacyRewardAmount { get; init; }
    public string? DetailsJson { get; init; }
}
public sealed record AccountingPage(IReadOnlyList<AccountingRow> Items, int TotalCount, int Page, int PageSize);
public sealed record AccountingMetric(string Key, decimal Amount, int Count);
public sealed record AccountingOverview(IReadOnlyList<AccountingMetric> Metrics);
public sealed record AccountingAccess(bool IsAdmin, IReadOnlyList<string> Permissions);
public sealed record AccountantUser(Guid Id, string Name, string PhoneNumber, IReadOnlyList<string> Permissions);

public interface IAccountingReadRepository
{
    bool Supports(AccountingSection section);
    Task<AccountingPage> ReadAsync(AccountingSection section, AccountingFilter filter, CancellationToken ct);
    Task<IReadOnlyList<AccountingMetric>> MetricsAsync(AccountingFilter filter, CancellationToken ct);
}
public interface IAccountingAccessRepository
{
    Task<AccountingAccess?> GetAsync(Guid userId, CancellationToken ct);
    Task<IReadOnlyList<AccountantUser>> UsersAsync(CancellationToken ct);
    Task<bool> ReplaceAsync(Guid userId, Guid actor, IReadOnlyList<string> permissions, CancellationToken ct);
}
