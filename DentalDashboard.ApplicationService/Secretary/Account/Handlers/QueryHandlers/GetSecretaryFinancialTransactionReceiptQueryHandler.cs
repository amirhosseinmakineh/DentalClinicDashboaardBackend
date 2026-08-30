using DentalDashboard.ApplicationService.Contract.IServices;
using DentalDashboard.ApplicationService.Contract.Secretary.Account.Queries;
using DentalDashboard.Domain.Secretary.Account.IRepositories;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.ApplicationService.Secretary.Account.Handlers.QueryHandlers;

public sealed class GetSecretaryFinancialTransactionReceiptQueryHandler : IQueryHandler<GetSecretaryFinancialTransactionReceiptQuery, FinancialTransactionReceiptResponse?>
{
    private readonly ISecretaryAccountRepository repository;
    private readonly IFinancialTransactionReceiptService receiptService;

    public GetSecretaryFinancialTransactionReceiptQueryHandler(
        ISecretaryAccountRepository repository,
        IFinancialTransactionReceiptService receiptService)
    {
        this.repository = repository;
        this.receiptService = receiptService;
    }

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
            .Include(x => x.ExpenseCategory)
            .FirstOrDefaultAsync(x => x.Id == query.Id && !x.IsDeleted, cancellationToken);

        if (transaction is null)
        {
            return null;
        }

        var transactionDto = GetSecretaryFinancialTransactionsQueryHandler.Map(transaction);

        return receiptService.Create(transactionDto);
    }
}
