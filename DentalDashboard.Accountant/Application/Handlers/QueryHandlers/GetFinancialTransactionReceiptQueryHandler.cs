using DentalDashboard.Accountant.Application.Contracts.Services;
using DentalDashboard.Accountant.Application.Contracts.Queries;
using DentalDashboard.Accountant.Domain.IRepositories;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.Accountant.Application.Handlers.QueryHandlers;

public sealed class GetFinancialTransactionReceiptQueryHandler : IQueryHandler<GetFinancialTransactionReceiptQuery, FinancialTransactionReceiptResponse?>
{
    private readonly IAccountantRepository repository;
    private readonly IFinancialTransactionReceiptService receiptService;

    public GetFinancialTransactionReceiptQueryHandler(
        IAccountantRepository repository,
        IFinancialTransactionReceiptService receiptService)
    {
        this.repository = repository;
        this.receiptService = receiptService;
    }

    public async Task<FinancialTransactionReceiptResponse?> HandleAsync(
        GetFinancialTransactionReceiptQuery query,
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

        var transactionDto = GetFinancialTransactionsQueryHandler.Map(transaction);

        return receiptService.Create(transactionDto);
    }
}
