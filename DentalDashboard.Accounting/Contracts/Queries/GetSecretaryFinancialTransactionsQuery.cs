using DentalDashboard.Accounting.Contracts.DTOs;
using DentalDashboard.Accounting.Domain.Enums;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using DentalDashboard.Framwork.Domain;

namespace DentalDashboard.Accounting.Contracts.Queries;

public sealed class GetSecretaryFinancialTransactionsQuery : IQuery<Result<SecretaryFinancialTransactionPage>>
{
    public const int DefaultPage = 1;
    public const int DefaultPageSize = 20;

    public FinancialTransactionType? Type { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? Search { get; set; }
    public long? ExpenseCategoryId { get; set; }
    public int Page { get; set; } = DefaultPage;
    public int PageSize { get; set; } = DefaultPageSize;
}
