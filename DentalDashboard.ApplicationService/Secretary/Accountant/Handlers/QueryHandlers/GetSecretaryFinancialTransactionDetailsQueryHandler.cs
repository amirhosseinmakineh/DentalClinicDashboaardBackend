using DentalDashboard.ApplicationService.Secretary.Accountant;
using DentalDashboard.ApplicationService.Contract.Secretary.Accountant.Commands;
using DentalDashboard.ApplicationService.Contract.Secretary.Accountant.DTOs;
using DentalDashboard.ApplicationService.Contract.Secretary.Accountant.Queries;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Domain.Secretary.Accountant.Entities;
using DentalDashboard.Domain.Secretary.Accountant.Enums;
using DentalDashboard.Domain.Secretary.Accountant.IRepositories;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using DentalDashboard.Framwork.Domain;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.ApplicationService.Secretary.Accountant.Handlers.QueryHandlers;
public sealed class GetSecretaryFinancialTransactionDetailsQueryHandler : IQueryHandler<GetSecretaryFinancialTransactionDetailsQuery, Result<SecretaryFinancialTransactionDto>?>
{
    private readonly ISecretaryAccountRepository repository;
    public GetSecretaryFinancialTransactionDetailsQueryHandler(ISecretaryAccountRepository repository)
    {
        this.repository = repository;
    }

    public async Task<Result<SecretaryFinancialTransactionDto>?> HandleAsync(GetSecretaryFinancialTransactionDetailsQuery query, CancellationToken cancellationToken = default)
    {
        var transaction = await repository.FinancialTransactions.AsNoTracking()
            .Include(x => x.ExpenseCategory)
            .FirstOrDefaultAsync(x => x.Id == query.Id && !x.IsDeleted, cancellationToken);
        return transaction is null ? null : Result<SecretaryFinancialTransactionDto>.Success(GetSecretaryFinancialTransactionsQueryHandler.Map(transaction));
    }
}

