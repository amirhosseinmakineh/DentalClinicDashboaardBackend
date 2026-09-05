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

public sealed class GetPatientFinancialCasesQueryHandler(
    IPatientFinanceRepository patientFinanceRepository)
    : IQueryHandler<GetPatientFinancialCasesQuery, PaginatedResult<PatientFinancialCaseDto>>
{
    public async Task<PaginatedResult<PatientFinancialCaseDto>> HandleAsync(
        GetPatientFinancialCasesQuery request,
        CancellationToken cancellationToken = default)
    {
        var financialCasesQuery = patientFinanceRepository.Cases
            .AsNoTracking()
            .AsQueryable();

        if (request.PatientId.HasValue)
            financialCasesQuery = financialCasesQuery.Where(
                financialCase => financialCase.PatientId == request.PatientId);

        if (request.ServiceId.HasValue)
            financialCasesQuery = financialCasesQuery.Where(
                financialCase => (int)financialCase.Service == request.ServiceId);

        if (request.AgreementType.HasValue)
            financialCasesQuery = financialCasesQuery.Where(
                financialCase => financialCase.AgreementType == request.AgreementType);

        if (request.Status.HasValue)
            financialCasesQuery = financialCasesQuery.Where(
                financialCase => financialCase.Status == request.Status);

        if (request.FromDate.HasValue)
            financialCasesQuery = financialCasesQuery.Where(
                financialCase => financialCase.CreatedAt >= request.FromDate);

        if (request.ToDate.HasValue)
            financialCasesQuery = financialCasesQuery.Where(
                financialCase => financialCase.CreatedAt <= request.ToDate);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var searchTerm = request.Search.Trim();

            financialCasesQuery = financialCasesQuery.Where(financialCase =>
                (financialCase.Patient.FirstName + " " + financialCase.Patient.LastName)
                    .Contains(searchTerm) ||
                financialCase.Patient.PhoneNumber.Contains(searchTerm));
        }

        var (pageNumber, pageSize) = QueryTools.Page(request);

        // This endpoint is the patient list for the finance area. A patient can
        // have several financial cases, but must only consume one row/page slot.
        // The most recently created matching case represents that patient.
        var totalCount = await financialCasesQuery
            .Select(financialCase => financialCase.PatientId)
            .Distinct()
            .CountAsync(cancellationToken);

        var patientIds = await financialCasesQuery
            .GroupBy(financialCase => financialCase.PatientId)
            .Select(patientGroup => new
            {
                PatientId = patientGroup.Key,
                LastCaseAt = patientGroup.Max(financialCase => financialCase.CreatedAt)
            })
            .OrderByDescending(patient => patient.LastCaseAt)
            .ThenBy(patient => patient.PatientId)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(patient => patient.PatientId)
            .ToListAsync(cancellationToken);

        var matchingCases = await financialCasesQuery
            .Where(financialCase => patientIds.Contains(financialCase.PatientId))
            .Select(financialCase => new PatientFinancialCaseDto(
                financialCase.Id,
                financialCase.PatientId,
                financialCase.Patient.Id,
                (financialCase.Patient.FirstName + " " + financialCase.Patient.LastName).Trim(),
                patientFinanceRepository.PatientFiles
                    .Where(patientFile =>
                        patientFile.PhoneNumber == financialCase.Patient.PhoneNumber)
                    .Select(patientFile => patientFile.FileNumber.ToString())
                    .FirstOrDefault() ?? "",
                financialCase.Patient.PhoneNumber,
                (int)financialCase.Service,
                financialCase.Service == DentalDashboard.Domain.Enums.DentalServiceType.Composite
                    ? "کامپوزیت"
                    : financialCase.Service == DentalDashboard.Domain.Enums.DentalServiceType.Implant
                        ? "ایمپلنت"
                        : financialCase.Service == DentalDashboard.Domain.Enums.DentalServiceType.Laminate
                            ? "لمینت"
                            : financialCase.Service.ToString(),
                financialCase.TotalAmount,
                financialCase.PrePaymentAmount,
                financialCase.DepositAmount,
                financialCase.Transactions
                    .Where(transaction =>
                        transaction.Type == PatientFinancialTransactionType.Payment)
                    .Sum(transaction => (decimal?)transaction.Amount) ?? 0,
                Math.Max(
                    financialCase.TotalAmount -
                    (financialCase.Transactions
                        .Where(transaction =>
                            transaction.Type == PatientFinancialTransactionType.Payment)
                        .Sum(transaction => (decimal?)transaction.Amount) ?? 0),
                    0),
                financialCase.Debts
                    .Where(debt => debt.Status == PatientDebtStatus.Unpaid)
                    .Sum(debt => (decimal?)debt.Amount) ?? 0,
                financialCase.AgreementType,
                financialCase.Status,
                financialCase.CreatedAt))
            .ToListAsync(cancellationToken);

        var casesByPatient = matchingCases
            .GroupBy(financialCase => financialCase.PatientId)
            .ToDictionary(
                patientGroup => patientGroup.Key,
                patientGroup => patientGroup
                    .OrderByDescending(financialCase => financialCase.CreatedAt)
                    .ThenByDescending(financialCase => financialCase.Id)
                    .First());

        var items = patientIds
            .Select(patientId => casesByPatient[patientId])
            .ToList();

        return new()
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }
}
