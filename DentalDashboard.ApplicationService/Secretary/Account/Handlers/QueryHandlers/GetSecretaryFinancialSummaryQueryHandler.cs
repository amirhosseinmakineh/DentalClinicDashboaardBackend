using DentalDashboard.ApplicationService.Secretary.Account;
using DentalDashboard.ApplicationService.Contract.Secretary.Account.Commands;
using DentalDashboard.ApplicationService.Contract.Secretary.Account.DTOs;
using DentalDashboard.ApplicationService.Contract.Secretary.Account.Queries;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Domain.Secretary.Account.Entities;
using DentalDashboard.Domain.Secretary.Account.Enums;
using DentalDashboard.Domain.Secretary.Account.IRepositories;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using DentalDashboard.Framwork.Domain;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.ApplicationService.Secretary.Account.Handlers.QueryHandlers;
public sealed class GetSecretaryFinancialSummaryQueryHandler : IQueryHandler<GetSecretaryFinancialSummaryQuery, Result<SecretaryFinancialSummaryDto>>
{
    private readonly ISecretaryAccountRepository repository;
    public GetSecretaryFinancialSummaryQueryHandler(ISecretaryAccountRepository repository)
    {
        this.repository = repository;
    }

    public async Task<Result<SecretaryFinancialSummaryDto>> HandleAsync(GetSecretaryFinancialSummaryQuery query, CancellationToken cancellationToken = default)
    {
        var transactions = repository.FinancialTransactions.AsNoTracking().Where(x => !x.IsDeleted);
        if (query.FromDate.HasValue)
        {
            transactions = transactions.Where(x => x.TransactionDate >= query.FromDate.Value);
        }
        if (query.ToDate.HasValue)
        {
            transactions = transactions.Where(x => x.TransactionDate <= query.ToDate.Value);
        }
        var totals = await transactions.GroupBy(_ => 1).Select(group => new
        {
            Income = group.Where(x => x.Type == FinancialTransactionType.Income).Sum(x => x.Amount),
            Expense = group.Where(x => x.Type == FinancialTransactionType.Expense).Sum(x => x.Amount),
            IncomeCount = group.Count(x => x.Type == FinancialTransactionType.Income),
            ExpenseCount = group.Count(x => x.Type == FinancialTransactionType.Expense)
        }).FirstOrDefaultAsync(cancellationToken);
        var income = totals?.Income ?? 0;
        var expense = totals?.Expense ?? 0;
        return Result<SecretaryFinancialSummaryDto>.Success(new SecretaryFinancialSummaryDto(income, expense, income - expense, totals?.IncomeCount ?? 0, totals?.ExpenseCount ?? 0));
    }
}

