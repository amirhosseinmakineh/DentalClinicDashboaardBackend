using DentalDashboard.Accounting.Application.Mappings;
using DentalDashboard.Accounting.Contracts.DTOs;
using DentalDashboard.Accounting.Contracts.Queries;
using DentalDashboard.Accounting.Domain.IRepositories;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using DentalDashboard.Framwork.Domain;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.Accounting.Application.Handlers.QueryHandlers;

public sealed class GetSecretaryFinancialTransactionDetailsQueryHandler(
    ISecretaryAccountRepository repository)
    : IQueryHandler<
        GetSecretaryFinancialTransactionDetailsQuery,
        Result<SecretaryFinancialTransactionDto>?>
{
    public async Task<Result<SecretaryFinancialTransactionDto>?> HandleAsync(
        GetSecretaryFinancialTransactionDetailsQuery query,
        CancellationToken cancellationToken = default)
    {
        var transaction = await repository.FinancialTransactions
            .AsNoTracking()
            .Include(item => item.ExpenseCategory)
            .FirstOrDefaultAsync(
                item => item.Id == query.Id && !item.IsDeleted,
                cancellationToken);

        return transaction is null
            ? null
            : Result<SecretaryFinancialTransactionDto>.Success(
                SecretaryFinancialTransactionMapper.ToDto(transaction));
    }
}
