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

public sealed class GetDuePatientFinancialCommitmentsQueryHandler(
    IPatientFinanceRepository patientFinanceRepository)
    : IQueryHandler<GetDuePatientFinancialCommitmentsQuery, PaginatedResult<PatientFinancialCommitmentDto>>
{
    public async Task<PaginatedResult<PatientFinancialCommitmentDto>> HandleAsync(
        GetDuePatientFinancialCommitmentsQuery request,
        CancellationToken cancellationToken = default)
    {
        var today = IranTimeHelper.TodayInIran();
        var defaultToDate =
            IranTimeHelper.GetIranDayRangeAsUtc(today.AddDays(3)).EndUtc;

        var fromDate = request.FromDate;
        var toDate = request.ToDate ?? defaultToDate;

        var chequesQuery = patientFinanceRepository.Cheques
            .AsNoTracking()
            .Where(cheque =>
                cheque.Status == PatientChequeStatus.Pending &&
                (!fromDate.HasValue || cheque.DueDate >= fromDate.Value) &&
                cheque.DueDate <= toDate &&
                (request.PatientId == null ||
                 cheque.FinancialCase.PatientId == request.PatientId));

        var promissoryNotesQuery = patientFinanceRepository.PromissoryNotes
            .AsNoTracking()
            .Where(promissoryNote =>
                promissoryNote.Status == PatientPromissoryNoteStatus.Pending &&
                (!fromDate.HasValue || promissoryNote.DueDate >= fromDate.Value) &&
                promissoryNote.DueDate <= toDate &&
                (request.PatientId == null ||
                 promissoryNote.FinancialCase.PatientId == request.PatientId));

        IQueryable<PatientFinancialCommitmentDto> SelectCheques(
            IQueryable<PatientCheque> query) =>
            query.Select(cheque => new PatientFinancialCommitmentDto(
                cheque.Id,
                PatientFinancialCommitmentType.Cheque,
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
                cheque.DueDate,
                (int)cheque.Status));

        IQueryable<PatientFinancialCommitmentDto> SelectNotes(
            IQueryable<PatientPromissoryNote> query) =>
            query.Select(promissoryNote => new PatientFinancialCommitmentDto(
                promissoryNote.Id,
                PatientFinancialCommitmentType.PromissoryNote,
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
                promissoryNote.Amount,
                promissoryNote.DueDate,
                (int)promissoryNote.Status));

        var (pageNumber, pageSize) = QueryTools.Page(request);
        var skipCount = (pageNumber - 1) * pageSize;

        if (request.Type == PatientFinancialCommitmentType.Cheque)
        {
            var selectedCount = await chequesQuery.CountAsync(cancellationToken);

            var selectedItems = await SelectCheques(
                    chequesQuery
                        .OrderBy(cheque => cheque.DueDate)
                        .ThenBy(cheque => cheque.Id)
                        .Skip(skipCount)
                        .Take(pageSize))
                .ToListAsync(cancellationToken);

            return new()
            {
                Items = selectedItems,
                TotalCount = selectedCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        if (request.Type == PatientFinancialCommitmentType.PromissoryNote)
        {
            var selectedCount =
                await promissoryNotesQuery.CountAsync(cancellationToken);

            var selectedItems = await SelectNotes(
                    promissoryNotesQuery
                        .OrderBy(promissoryNote => promissoryNote.DueDate)
                        .ThenBy(promissoryNote => promissoryNote.Id)
                        .Skip(skipCount)
                        .Take(pageSize))
                .ToListAsync(cancellationToken);

            return new()
            {
                Items = selectedItems,
                TotalCount = selectedCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        // Applying Concat after these DTO projections is not translatable by EF
        // Core because each projection contains a correlated patient-file lookup.
        // Read only the prefix needed from each source, then merge that small result
        // in memory so global ordering and pagination remain correct.
        var chequeCount = await chequesQuery.CountAsync(cancellationToken);
        var noteCount =
            await promissoryNotesQuery.CountAsync(cancellationToken);

        var windowSize = skipCount + pageSize;

        var chequeWindow = await SelectCheques(
                chequesQuery
                    .OrderBy(cheque => cheque.DueDate)
                    .ThenBy(cheque => cheque.Id)
                    .Take(windowSize))
            .ToListAsync(cancellationToken);

        var noteWindow = await SelectNotes(
                promissoryNotesQuery
                    .OrderBy(promissoryNote => promissoryNote.DueDate)
                    .ThenBy(promissoryNote => promissoryNote.Id)
                    .Take(windowSize))
            .ToListAsync(cancellationToken);

        var items = chequeWindow
            .Concat(noteWindow)
            .OrderBy(commitment => commitment.DueDate)
            .ThenBy(commitment => commitment.Type)
            .ThenBy(commitment => commitment.Id)
            .Skip(skipCount)
            .Take(pageSize)
            .ToList();

        var totalCount = chequeCount + noteCount;

        return new()
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }
}
