using DentalDashboard.Domain.Secretary.Accountant.Enums;

namespace DentalDashboard.ApplicationService.Contract.Secretary.Accountant.DTOs;

public sealed record SecretaryFinancialTransactionPage(
    IReadOnlyList<SecretaryFinancialTransactionDto> Items,
    int Page,
    int PageSize,
    int TotalCount);
