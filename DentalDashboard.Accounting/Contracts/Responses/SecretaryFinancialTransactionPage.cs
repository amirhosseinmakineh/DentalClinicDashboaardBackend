using DentalDashboard.Accounting.Domain.Enums;

namespace DentalDashboard.Accounting.Contracts.DTOs;

public sealed record SecretaryFinancialTransactionPage(
    IReadOnlyList<SecretaryFinancialTransactionDto> Items,
    int Page,
    int PageSize,
    int TotalCount);
