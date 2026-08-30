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
public sealed class GetFinancialTransactionDetailsQueryHandler : IQueryHandler<GetFinancialTransactionDetailsQuery, Result<FinancialTransactionDto>?>
{
    private readonly IAccountantRepository repository;
    public GetFinancialTransactionDetailsQueryHandler(IAccountantRepository repository)
    {
        this.repository = repository;
    }

    public async Task<Result<FinancialTransactionDto>?> HandleAsync(GetFinancialTransactionDetailsQuery query, CancellationToken cancellationToken = default)
    {
        var transaction = await repository.FinancialTransactions.AsNoTracking()
            .Include(x => x.ExpenseCategory)
            .FirstOrDefaultAsync(x => x.Id == query.Id && !x.IsDeleted, cancellationToken);
        return transaction is null ? null : Result<FinancialTransactionDto>.Success(GetFinancialTransactionsQueryHandler.Map(transaction));
    }
}

