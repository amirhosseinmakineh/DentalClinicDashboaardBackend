using System.Globalization;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using DentalDashboard.Accounting.Contracts.PatientFinance.Queries;
using DentalDashboard.Domain.Models;
using DentalDashboard.Accounting.Domain.PatientFinance.Entities;
using DentalDashboard.Accounting.Domain.PatientFinance.Enums;
using DentalDashboard.Accounting.Domain.PatientFinance.IRepositories;
using DentalDashboard.Utilities.Time;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.Accounting.Application.PatientFinance.Handlers;

public sealed class GetPatientFinancialTransactionsQueryHandler(
    IPatientFinanceRepository patientFinanceRepository)
    : IQueryHandler<GetPatientFinancialTransactionsQuery, PaginatedResult<PatientFinancialTransactionDto>>
{
    public async Task<PaginatedResult<PatientFinancialTransactionDto>> HandleAsync(
        GetPatientFinancialTransactionsQuery request,
        CancellationToken cancellationToken = default)
    {
        var transactionsQuery = patientFinanceRepository.Transactions.AsNoTracking();

        if (request.PatientId.HasValue)
            transactionsQuery = transactionsQuery.Where(
                transaction =>
                    transaction.FinancialCase.PatientId == request.PatientId);

        if (request.PatientFinancialCaseId.HasValue)
            transactionsQuery = transactionsQuery.Where(
                transaction =>
                    transaction.PatientFinancialCaseId ==
                    request.PatientFinancialCaseId);

        if (request.SourceType.HasValue)
            transactionsQuery = transactionsQuery.Where(
                transaction => transaction.SourceType == request.SourceType);

        if (request.FromDate.HasValue)
            transactionsQuery = transactionsQuery.Where(
                transaction => transaction.CreatedAt >= request.FromDate);

        if (request.ToDate.HasValue)
            transactionsQuery = transactionsQuery.Where(
                transaction => transaction.CreatedAt <= request.ToDate);

        var (pageNumber, pageSize) = QueryTools.Page(request);
        var totalCount = await transactionsQuery.CountAsync(cancellationToken);

        var items = await transactionsQuery
            .OrderByDescending(transaction => transaction.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(transaction => new PatientFinancialTransactionDto(
                transaction.Id,
                transaction.PatientFinancialCaseId,
                transaction.FinancialCase.PatientId,
                (transaction.FinancialCase.Patient.FirstName + " " +
                 transaction.FinancialCase.Patient.LastName).Trim(),
                patientFinanceRepository.PatientFiles
                    .Where(patientFile =>
                        patientFile.PhoneNumber ==
                        transaction.FinancialCase.Patient.PhoneNumber)
                    .Select(patientFile => patientFile.FileNumber.ToString())
                    .FirstOrDefault() ?? "",
                transaction.Amount,
                transaction.Type,
                transaction.SourceType,
                transaction.SourceId,
                transaction.CreatedAt))
            .ToListAsync(cancellationToken);

        return new()
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }
}
