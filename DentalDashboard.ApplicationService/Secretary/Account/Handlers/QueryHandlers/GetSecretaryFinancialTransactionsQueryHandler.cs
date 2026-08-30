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
public sealed class GetSecretaryFinancialTransactionsQueryHandler : IQueryHandler<GetSecretaryFinancialTransactionsQuery, Result<SecretaryFinancialTransactionPage>>
{
    private readonly ISecretaryAccountRepository repository;

    public GetSecretaryFinancialTransactionsQueryHandler(ISecretaryAccountRepository repository)
    {
        this.repository = repository;
    }

    public async Task<Result<SecretaryFinancialTransactionPage>> HandleAsync(GetSecretaryFinancialTransactionsQuery query, CancellationToken cancellationToken = default)
    {
        var page = Math.Max(query.Page, SecretaryAccountConstants.DefaultPage);
        var pageSize = Math.Clamp(query.PageSize, SecretaryAccountConstants.MinimumPageSize, SecretaryAccountConstants.MaximumPageSize);
        var transactions = repository.FinancialTransactions.AsNoTracking().Where(x => !x.IsDeleted);

        if (query.Type.HasValue)
        {
            transactions = transactions.Where(x => x.Type == query.Type.Value);
        }
        if (query.FromDate.HasValue)
        {
            transactions = transactions.Where(x => x.TransactionDate >= query.FromDate.Value);
        }
        if (query.ToDate.HasValue)
        {
            transactions = transactions.Where(x => x.TransactionDate <= query.ToDate.Value);
        }
        if (query.ExpenseCategoryId.HasValue)
        {
            transactions = transactions.Where(x => x.ExpenseCategoryId == query.ExpenseCategoryId.Value);
        }
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            transactions = transactions.Where(x =>
                (x.Subject != null && x.Subject.Contains(search)) ||
                (x.CounterpartyName != null && x.CounterpartyName.Contains(search)) ||
                (x.TrackingNumber != null && x.TrackingNumber.Contains(search)) ||
                (x.Description != null && x.Description.Contains(search)));
        }

        var totalCount = await transactions.CountAsync(cancellationToken);
        var entities = await transactions.Include(x => x.ExpenseCategory)
            .OrderByDescending(x => x.TransactionDate)
            .ThenByDescending(x => x.Id)
            .Skip((page - SecretaryAccountConstants.DefaultPage) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        var items = entities.Select(Map).ToList();

        return Result<SecretaryFinancialTransactionPage>.Success(new SecretaryFinancialTransactionPage(items, page, pageSize, totalCount));
    }

    internal static SecretaryFinancialTransactionDto Map(FinancialTransaction transaction)
    {
        return new SecretaryFinancialTransactionDto
        {
            Id = transaction.Id,
            Type = transaction.Type,
            TypeTitle = transaction.Type.GetTitle(),
            Amount = transaction.Amount,
            TransactionDate = transaction.TransactionDate,
            Subject = transaction.Subject,
            CounterpartyName = transaction.CounterpartyName,
            PaymentMethod = transaction.PaymentMethod,
            PaymentMethodTitle = transaction.PaymentMethod.GetTitle(),
            TrackingNumber = transaction.TrackingNumber,
            Description = transaction.Description,
            ReceiptUrl = transaction.ReceiptUrl,
            ExpenseCategoryId = transaction.ExpenseCategoryId,
            ExpenseCategoryTitle = transaction.ExpenseCategory == null ? null : transaction.ExpenseCategory.Title,
            CreatedAt = transaction.CreatedAt
        };
    }

}

