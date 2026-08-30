using DentalDashboard.Accountant.Application;
using DentalDashboard.Accountant.Application.Contracts.Commands;
using DentalDashboard.Accountant.Application.Contracts.DTOs;
using DentalDashboard.Accountant.Application.Contracts.Queries;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Accountant.Domain.Entities;
using DentalDashboard.Accountant.Domain.Enums;
using DentalDashboard.Accountant.Domain.IRepositories;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using DentalDashboard.Framwork.Domain;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.Accountant.Application.Handlers.QueryHandlers;
public sealed class GetFinancialSummaryQueryHandler : IQueryHandler<GetFinancialSummaryQuery, Result<FinancialSummaryDto>>
{
    private readonly IAccountantRepository repository;
    public GetFinancialSummaryQueryHandler(IAccountantRepository repository)
    {
        this.repository = repository;
    }

    public async Task<Result<FinancialSummaryDto>> HandleAsync(GetFinancialSummaryQuery query, CancellationToken cancellationToken = default)
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
        return Result<FinancialSummaryDto>.Success(new FinancialSummaryDto(income, expense, income - expense, totals?.IncomeCount ?? 0, totals?.ExpenseCount ?? 0));
    }
}

