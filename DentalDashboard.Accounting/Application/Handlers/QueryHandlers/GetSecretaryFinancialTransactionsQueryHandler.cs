using DentalDashboard.ApplicationService.Secretary.Accountant;
using DentalDashboard.ApplicationService.Secretary.Accountant.Mappings;
using DentalDashboard.ApplicationService.Contract.Secretary.Accountant.DTOs;
using DentalDashboard.ApplicationService.Contract.Secretary.Accountant.Queries;
using DentalDashboard.Domain.Secretary.Accountant.IRepositories;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using DentalDashboard.Framwork.Domain;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.ApplicationService.Secretary.Accountant.Handlers.QueryHandlers;

public sealed class GetSecretaryFinancialTransactionsQueryHandler(
    ISecretaryAccountRepository repository)
    : IQueryHandler<
        GetSecretaryFinancialTransactionsQuery,
        Result<SecretaryFinancialTransactionPage>>
{
    public async Task<Result<SecretaryFinancialTransactionPage>> HandleAsync(
        GetSecretaryFinancialTransactionsQuery query,
        CancellationToken cancellationToken = default)
    {
        var page = Math.Max(query.Page, SecretaryAccountConstants.DefaultPage);
        var pageSize = Math.Clamp(
            query.PageSize,
            SecretaryAccountConstants.MinimumPageSize,
            SecretaryAccountConstants.MaximumPageSize);
        var transactions = repository.FinancialTransactions
            .AsNoTracking()
            .Where(transaction => !transaction.IsDeleted);

        if (query.Type.HasValue)
        {
            transactions = transactions.Where(
                transaction => transaction.Type == query.Type.Value);
        }
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
        if (query.ExpenseCategoryId.HasValue)
        {
            transactions = transactions.Where(transaction =>
                transaction.ExpenseCategoryId == query.ExpenseCategoryId.Value);
        }
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var searchTerm = query.Search.Trim();
            transactions = transactions.Where(transaction =>
                (transaction.Subject != null &&
                 transaction.Subject.Contains(searchTerm)) ||
                (transaction.CounterpartyName != null &&
                 transaction.CounterpartyName.Contains(searchTerm)) ||
                (transaction.Description != null &&
                 transaction.Description.Contains(searchTerm)));
        }

        var totalCount = await transactions.CountAsync(cancellationToken);
        var entities = await transactions
            .Include(transaction => transaction.ExpenseCategory)
            .OrderByDescending(transaction => transaction.TransactionDate)
            .ThenByDescending(transaction => transaction.Id)
            .Skip((page - SecretaryAccountConstants.DefaultPage) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        var items = entities
            .Select(SecretaryFinancialTransactionMapper.ToDto)
            .ToList();

        return Result<SecretaryFinancialTransactionPage>.Success(
            new SecretaryFinancialTransactionPage(
                items,
                page,
                pageSize,
                totalCount));
    }
}
