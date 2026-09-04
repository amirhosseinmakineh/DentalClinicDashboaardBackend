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

public sealed class GetPatientPromissoryNotesQueryHandler(
    IPatientFinanceRepository patientFinanceRepository)
    : IQueryHandler<GetPatientPromissoryNotesQuery, PaginatedResult<PatientPromissoryNoteDto>>
{
    public async Task<PaginatedResult<PatientPromissoryNoteDto>> HandleAsync(
        GetPatientPromissoryNotesQuery request,
        CancellationToken cancellationToken = default)
    {
        var promissoryNotesQuery = patientFinanceRepository.PromissoryNotes.AsNoTracking();

        if (request.PatientFinancialCaseId.HasValue)
            promissoryNotesQuery = promissoryNotesQuery.Where(
                promissoryNote =>
                    promissoryNote.PatientFinancialCaseId ==
                    request.PatientFinancialCaseId);

        if (request.PatientId.HasValue)
            promissoryNotesQuery = promissoryNotesQuery.Where(
                promissoryNote =>
                    promissoryNote.FinancialCase.PatientId == request.PatientId);

        if (request.Status.HasValue)
            promissoryNotesQuery = promissoryNotesQuery.Where(
                promissoryNote => promissoryNote.Status == request.Status);

        if (request.FromDueDate.HasValue)
            promissoryNotesQuery = promissoryNotesQuery.Where(
                promissoryNote => promissoryNote.DueDate >= request.FromDueDate);

        if (request.ToDueDate.HasValue)
            promissoryNotesQuery = promissoryNotesQuery.Where(
                promissoryNote => promissoryNote.DueDate <= request.ToDueDate);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var searchTerm = request.Search.Trim();

            promissoryNotesQuery = promissoryNotesQuery.Where(promissoryNote =>
                promissoryNote.SerialNumber.Contains(searchTerm) ||
                (promissoryNote.FinancialCase.Patient.FirstName + " " +
                 promissoryNote.FinancialCase.Patient.LastName).Contains(searchTerm));
        }

        var (pageNumber, pageSize) = QueryTools.Page(request);
        var totalCount = await promissoryNotesQuery.CountAsync(cancellationToken);

        var items = await promissoryNotesQuery
            .OrderBy(promissoryNote => promissoryNote.DueDate)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(promissoryNote => new PatientPromissoryNoteDto(
                promissoryNote.Id,
                promissoryNote.PatientFinancialCaseId,
                promissoryNote.FinancialCase.PatientId,
                (promissoryNote.FinancialCase.Patient.FirstName + " " +
                 promissoryNote.FinancialCase.Patient.LastName).Trim(),
                patientFinanceRepository.PatientFiles
                    .Where(patientFile =>
                        patientFile.PhoneNumber ==
                        promissoryNote.FinancialCase.Patient.PhoneNumber)
                    .Select(patientFile => patientFile.FileNumber.ToString())
                    .FirstOrDefault() ?? "",
                promissoryNote.SerialNumber,
                promissoryNote.Amount,
                promissoryNote.DueDate,
                promissoryNote.Status))
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
