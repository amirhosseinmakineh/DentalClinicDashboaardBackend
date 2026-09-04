using DentalDashboard.ApplicationService.Secretary.Accountant;
using DentalDashboard.ApplicationService.Contract.Secretary.Accountant.DTOs;
using DentalDashboard.ApplicationService.Contract.Secretary.Accountant.Queries;
using DentalDashboard.Domain.Secretary.Accountant.Enums;
using DentalDashboard.Domain.Secretary.Accountant.IRepositories;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using DentalDashboard.Framwork.Domain;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.ApplicationService.Secretary.Accountant.Handlers.QueryHandlers;

public sealed class GetSecretaryFinancialSummaryQueryHandler(
    ISecretaryAccountRepository repository)
    : IQueryHandler<GetSecretaryFinancialSummaryQuery, Result<SecretaryFinancialSummaryDto>>
{
    public async Task<Result<SecretaryFinancialSummaryDto>> HandleAsync(
        GetSecretaryFinancialSummaryQuery query,
        CancellationToken cancellationToken = default)
    {
        var transactions = repository.FinancialTransactions
            .AsNoTracking()
            .Where(transaction => !transaction.IsDeleted);

        if (query.FromDate.HasValue)
        {
            transactions = transactions.Where(
                transaction => transaction.TransactionDate >= query.FromDate.Value);
        }

        if (query.ToDate.HasValue)
        {
            transactions = transactions.Where(
                transaction => transaction.TransactionDate <= query.ToDate.Value);
        }

        var totals = await transactions
            .GroupBy(_ => 1)
            .Select(group => new
            {
                Income = group
                    .Where(transaction => transaction.Type == FinancialTransactionType.Income)
                    .Sum(transaction => transaction.Amount),
                Expense = group
                    .Where(transaction => transaction.Type == FinancialTransactionType.Expense)
                    .Sum(transaction => transaction.Amount),
                IncomeCount = group.Count(
                    transaction => transaction.Type == FinancialTransactionType.Income),
                ExpenseCount = group.Count(
                    transaction => transaction.Type == FinancialTransactionType.Expense)
            })
            .FirstOrDefaultAsync(cancellationToken);

        var income = totals?.Income ?? 0;
        var expense = totals?.Expense ?? 0;

        return Result<SecretaryFinancialSummaryDto>.Success(
            new SecretaryFinancialSummaryDto(
                income,
                expense,
                income - expense,
                totals?.IncomeCount ?? 0,
                totals?.ExpenseCount ?? 0));
    }
}
