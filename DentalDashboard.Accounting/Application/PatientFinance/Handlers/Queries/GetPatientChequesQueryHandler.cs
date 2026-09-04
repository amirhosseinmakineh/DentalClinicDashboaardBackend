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

public sealed class GetPatientChequesQueryHandler(
    IPatientFinanceRepository patientFinanceRepository)
    : IQueryHandler<GetPatientChequesQuery, PaginatedResult<PatientChequeDto>>
{
    public async Task<PaginatedResult<PatientChequeDto>> HandleAsync(
        GetPatientChequesQuery request,
        CancellationToken cancellationToken = default)
    {
        var chequesQuery = patientFinanceRepository.Cheques.AsNoTracking();

        if (request.PatientFinancialCaseId.HasValue)
            chequesQuery = chequesQuery.Where(
                cheque => cheque.PatientFinancialCaseId == request.PatientFinancialCaseId);

        if (request.PatientId.HasValue)
            chequesQuery = chequesQuery.Where(
                cheque => cheque.FinancialCase.PatientId == request.PatientId);

        if (request.Status.HasValue)
            chequesQuery = chequesQuery.Where(
                cheque => cheque.Status == request.Status);

        if (request.FromDueDate.HasValue)
            chequesQuery = chequesQuery.Where(
                cheque => cheque.DueDate >= request.FromDueDate);

        if (request.ToDueDate.HasValue)
            chequesQuery = chequesQuery.Where(
                cheque => cheque.DueDate <= request.ToDueDate);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var searchTerm = request.Search.Trim();

            chequesQuery = chequesQuery.Where(cheque =>
                cheque.SayadNumber.Contains(searchTerm) ||
                cheque.OwnerName.Contains(searchTerm) ||
                (cheque.FinancialCase.Patient.FirstName + " " +
                 cheque.FinancialCase.Patient.LastName).Contains(searchTerm));
        }

        var (pageNumber, pageSize) = QueryTools.Page(request);
        var totalCount = await chequesQuery.CountAsync(cancellationToken);

        var items = await chequesQuery
            .OrderBy(cheque => cheque.DueDate)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(cheque => new PatientChequeDto(
                cheque.Id,
                cheque.PatientFinancialCaseId,
                cheque.FinancialCase.PatientId,
                (cheque.FinancialCase.Patient.FirstName + " " +
                 cheque.FinancialCase.Patient.LastName).Trim(),
                patientFinanceRepository.PatientFiles
                    .Where(patientFile =>
                        patientFile.PhoneNumber ==
                        cheque.FinancialCase.Patient.PhoneNumber)
                    .Select(patientFile => patientFile.FileNumber.ToString())
                    .FirstOrDefault() ?? "",
                cheque.Amount,
                cheque.SayadNumber,
                cheque.OwnerName,
                cheque.DueDate,
                cheque.Status))
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
