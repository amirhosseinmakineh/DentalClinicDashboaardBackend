using System.Globalization;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using DentalDashboard.ApplicationService.Contract.Secretary.Accountant.PatientFinance.Queries;
using DentalDashboard.Domain.Models;
using DentalDashboard.Domain.Secretary.Accountant.PatientFinance.Entities;
using DentalDashboard.Domain.Secretary.Accountant.PatientFinance.Enums;
using DentalDashboard.Domain.Secretary.Accountant.PatientFinance.IRepositories;
using DentalDashboard.Utilities.Time;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.ApplicationService.Secretary.Accountant.PatientFinance.Handlers;

public sealed class GetPatientDebtsQueryHandler(
    IPatientFinanceRepository patientFinanceRepository)
    : IQueryHandler<GetPatientDebtsQuery, PaginatedResult<PatientDebtDto>>
{
    public async Task<PaginatedResult<PatientDebtDto>> HandleAsync(
        GetPatientDebtsQuery request,
        CancellationToken cancellationToken = default)
    {
        var debtsQuery = patientFinanceRepository.Debts.AsNoTracking();

        if (request.PatientId.HasValue)
            debtsQuery = debtsQuery.Where(
                debt => debt.FinancialCase.PatientId == request.PatientId);

        if (request.PatientFinancialCaseId.HasValue)
            debtsQuery = debtsQuery.Where(
                debt => debt.PatientFinancialCaseId == request.PatientFinancialCaseId);

        if (request.SourceType.HasValue)
            debtsQuery = debtsQuery.Where(
                debt => debt.SourceType == request.SourceType);

        if (request.Status.HasValue)
            debtsQuery = debtsQuery.Where(
                debt => debt.Status == request.Status);

        if (request.Status == PatientDebtStatus.Unpaid)
        {
            debtsQuery = debtsQuery.Where(debt =>
                (debt.SourceType == PatientDebtSourceType.Cheque
                    ? patientFinanceRepository.Cheques.Any(cheque =>
                        cheque.Id == debt.SourceId &&
                        cheque.Status == PatientChequeStatus.Unpaid)
                    : patientFinanceRepository.PromissoryNotes.Any(promissoryNote =>
                        promissoryNote.Id == debt.SourceId &&
                        promissoryNote.Status == PatientPromissoryNoteStatus.Unpaid)) &&
                !patientFinanceRepository.Transactions.Any(transaction =>
                    transaction.SourceId == debt.SourceId &&
                    transaction.Type == PatientFinancialTransactionType.Payment &&
                    ((debt.SourceType == PatientDebtSourceType.Cheque &&
                      transaction.SourceType ==
                      PatientFinancialTransactionSourceType.Cheque) ||
                     (debt.SourceType == PatientDebtSourceType.PromissoryNote &&
                      transaction.SourceType ==
                      PatientFinancialTransactionSourceType.PromissoryNote))));
        }

        var persianMonth = QueryTools.PersianMonth(request.Year, request.Month);

        if (persianMonth.HasValue)
            debtsQuery = debtsQuery.Where(debt =>
                debt.DueDate >= persianMonth.Value.start &&
                debt.DueDate <= persianMonth.Value.end);

        if (request.FromDueDate.HasValue)
            debtsQuery = debtsQuery.Where(
                debt => debt.DueDate >= request.FromDueDate);

        if (request.ToDueDate.HasValue)
            debtsQuery = debtsQuery.Where(
                debt => debt.DueDate <= request.ToDueDate);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var searchTerm = request.Search.Trim();

            debtsQuery = debtsQuery.Where(debt =>
                (debt.FinancialCase.Patient.FirstName + " " +
                 debt.FinancialCase.Patient.LastName).Contains(searchTerm) ||
                debt.FinancialCase.Patient.PhoneNumber.Contains(searchTerm));
        }

        var (pageNumber, pageSize) = QueryTools.Page(request);
        var totalCount = await debtsQuery.CountAsync(cancellationToken);

        var items = await debtsQuery
            .OrderBy(debt => debt.DueDate)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(debt => new PatientDebtDto(
                debt.Id,
                debt.FinancialCase.PatientId,
                (debt.FinancialCase.Patient.FirstName + " " +
                 debt.FinancialCase.Patient.LastName).Trim(),
                patientFinanceRepository.PatientFiles
                    .Where(patientFile =>
                        patientFile.PhoneNumber ==
                        debt.FinancialCase.Patient.PhoneNumber)
                    .Select(patientFile => patientFile.FileNumber.ToString())
                    .FirstOrDefault() ?? "",
                debt.FinancialCase.Patient.PhoneNumber,
                debt.PatientFinancialCaseId,
                debt.FinancialCase.Service.ToString(),
                debt.Amount,
                debt.SourceType,
                debt.SourceId,
                debt.DueDate,
                debt.Status))
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
