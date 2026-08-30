using DentalDashboard.Accountant.Application.Contracts.DTOs;
using DentalDashboard.Accountant.Domain.Enums;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using DentalDashboard.Framwork.Domain;

namespace DentalDashboard.Accountant.Application.Contracts.Queries;

public sealed class GetFinancialTransactionsQuery : IQuery<Result<FinancialTransactionPage>>
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
