using DentalDashboard.Accounting.Application.Mappings;
using DentalDashboard.Accounting.Contracts.Services;
using DentalDashboard.Accounting.Contracts.Queries;
using DentalDashboard.Accounting.Domain.IRepositories;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.Accounting.Application.Handlers.QueryHandlers;

public sealed class GetSecretaryFinancialTransactionReceiptQueryHandler(
    ISecretaryAccountRepository repository,
    IFinancialTransactionReceiptService receiptService)
    : IQueryHandler<
        GetSecretaryFinancialTransactionReceiptQuery,
        FinancialTransactionReceiptResponse?>
{
    public async Task<FinancialTransactionReceiptResponse?> HandleAsync(
        GetSecretaryFinancialTransactionReceiptQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.Id <= 0)
        {
            return null;
        }

        var transaction = await repository.FinancialTransactions
            .AsNoTracking()
            .Include(item => item.ExpenseCategory)
            .FirstOrDefaultAsync(
                item => item.Id == query.Id && !item.IsDeleted,
                cancellationToken);

        if (transaction is null)
        {
            return null;
        }

        var transactionDto = SecretaryFinancialTransactionMapper.ToDto(transaction);

        return receiptService.Create(transactionDto);
    }
}
