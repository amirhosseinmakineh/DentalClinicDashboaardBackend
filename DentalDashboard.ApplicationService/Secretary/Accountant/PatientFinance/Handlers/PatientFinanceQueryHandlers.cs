using System.Globalization;
using DentalDashboard.ApplicationService.Contract.Secretary.Accountant.PatientFinance.Queries;
using DentalDashboard.Domain.Models;
using DentalDashboard.Domain.Secretary.Accountant.PatientFinance.Entities;
using DentalDashboard.Domain.Secretary.Accountant.PatientFinance.Enums;
using DentalDashboard.Domain.Secretary.Accountant.PatientFinance.IRepositories;
using DentalDashboard.Utilities.Time;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.ApplicationService.Secretary.Accountant.PatientFinance.Handlers;

internal static class QueryTools
{
    public static (int page, int size) Page(PatientFinancePagedQuery request) =>
        (Math.Max(1, request.Page), Math.Clamp(request.PageSize, 1, 100));

    public static string Name(string firstName, string lastName) =>
        (firstName + " " + lastName).Trim();

    public static (DateTime start, DateTime end)? PersianMonth(int? year, int? month)
    {
        if (year is null && month is null)
            return null;

        if (year is null || month is null || month < 1 || month > 12)
            throw new ArgumentException("سال و ماه باید با هم و معتبر ارسال شوند");

        var persianCalendar = new PersianCalendar();

        var start = DateTime.SpecifyKind(
            persianCalendar.ToDateTime(year.Value, month.Value, 1, 0, 0, 0, 0),
            DateTimeKind.Utc);

        var daysInMonth = persianCalendar.GetDaysInMonth(year.Value, month.Value);

        var end = DateTime.SpecifyKind(
            persianCalendar.ToDateTime(year.Value, month.Value, daysInMonth, 23, 59, 59, 999, 0),
            DateTimeKind.Utc);

        return (start, end);
    }
}

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

public sealed class GetPatientFinancialCaseDetailsQueryHandler(
    IPatientFinanceRepository patientFinanceRepository)
    : IQueryHandler<GetPatientFinancialCaseDetailsQuery, PatientFinancialCaseDetailsDto?>
{
    public Task<PatientFinancialCaseDetailsDto?> HandleAsync(
        GetPatientFinancialCaseDetailsQuery request,
        CancellationToken cancellationToken = default) =>
        patientFinanceRepository.Cases
            .AsNoTracking()
            .Where(financialCase =>
                financialCase.Id == request.PatientFinancialCaseId)
            .Select(financialCase => new PatientFinancialCaseDetailsDto(
                new(
                    financialCase.Id,
                    financialCase.PatientId,
                    financialCase.Patient.Id,
                    (financialCase.Patient.FirstName + " " +
                     financialCase.Patient.LastName).Trim(),
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
                    financialCase.CreatedAt),
                financialCase.Cheques.Count(
                    cheque => cheque.Status != PatientChequeStatus.Cancelled),
                financialCase.Cheques
                    .Where(cheque => cheque.Status != PatientChequeStatus.Cancelled)
                    .Sum(cheque => (decimal?)cheque.Amount) ?? 0,
                financialCase.PromissoryNotes.Count(
                    promissoryNote =>
                        promissoryNote.Status != PatientPromissoryNoteStatus.Cancelled),
                financialCase.PromissoryNotes
                    .Where(promissoryNote =>
                        promissoryNote.Status != PatientPromissoryNoteStatus.Cancelled)
                    .Sum(promissoryNote => (decimal?)promissoryNote.Amount) ?? 0,
                financialCase.Cheques
                    .OrderBy(cheque => cheque.DueDate)
                    .ThenBy(cheque => cheque.Id)
                    .Select(cheque => new PatientChequeDto(
                        cheque.Id,
                        cheque.PatientFinancialCaseId,
                        financialCase.PatientId,
                        (financialCase.Patient.FirstName + " " +
                         financialCase.Patient.LastName).Trim(),
                        patientFinanceRepository.PatientFiles
                            .Where(patientFile =>
                                patientFile.PhoneNumber == financialCase.Patient.PhoneNumber)
                            .Select(patientFile => patientFile.FileNumber.ToString())
                            .FirstOrDefault() ?? "",
                        cheque.Amount,
                        cheque.SayadNumber,
                        cheque.OwnerName,
                        cheque.DueDate,
                        cheque.Status))
                    .ToList(),
                financialCase.PromissoryNotes
                    .OrderBy(promissoryNote => promissoryNote.DueDate)
                    .ThenBy(promissoryNote => promissoryNote.Id)
                    .Select(promissoryNote => new PatientPromissoryNoteDto(
                        promissoryNote.Id,
                        promissoryNote.PatientFinancialCaseId,
                        financialCase.PatientId,
                        (financialCase.Patient.FirstName + " " +
                         financialCase.Patient.LastName).Trim(),
                        patientFinanceRepository.PatientFiles
                            .Where(patientFile =>
                                patientFile.PhoneNumber == financialCase.Patient.PhoneNumber)
                            .Select(patientFile => patientFile.FileNumber.ToString())
                            .FirstOrDefault() ?? "",
                        promissoryNote.SerialNumber,
                        promissoryNote.Amount,
                        promissoryNote.DueDate,
                        promissoryNote.Status))
                    .ToList()))
            .FirstOrDefaultAsync(cancellationToken);
}

public sealed class GetPatientFinancialCaseSummaryQueryHandler(
    IPatientFinanceRepository patientFinanceRepository)
    : IQueryHandler<GetPatientFinancialCaseSummaryQuery, PatientFinancialCaseSummaryDto?>
{
    public Task<PatientFinancialCaseSummaryDto?> HandleAsync(
        GetPatientFinancialCaseSummaryQuery request,
        CancellationToken cancellationToken = default) =>
        patientFinanceRepository.Cases
            .AsNoTracking()
            .Where(financialCase =>
                financialCase.Id == request.PatientFinancialCaseId)
            .Select(financialCase => new PatientFinancialCaseSummaryDto(
                financialCase.TotalAmount,
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
                financialCase.Cheques
                    .Where(cheque => cheque.Status != PatientChequeStatus.Cancelled)
                    .Sum(cheque => (decimal?)cheque.Amount) ?? 0,
                financialCase.Cheques
                    .Where(cheque => cheque.Status == PatientChequeStatus.Paid)
                    .Sum(cheque => (decimal?)cheque.Amount) ?? 0,
                financialCase.Cheques
                    .Where(cheque => cheque.Status == PatientChequeStatus.Pending)
                    .Sum(cheque => (decimal?)cheque.Amount) ?? 0,
                financialCase.Cheques
                    .Where(cheque => cheque.Status == PatientChequeStatus.Unpaid)
                    .Sum(cheque => (decimal?)cheque.Amount) ?? 0,
                financialCase.PromissoryNotes
                    .Where(promissoryNote =>
                        promissoryNote.Status != PatientPromissoryNoteStatus.Cancelled)
                    .Sum(promissoryNote => (decimal?)promissoryNote.Amount) ?? 0,
                financialCase.PromissoryNotes
                    .Where(promissoryNote =>
                        promissoryNote.Status == PatientPromissoryNoteStatus.Paid)
                    .Sum(promissoryNote => (decimal?)promissoryNote.Amount) ?? 0,
                financialCase.PromissoryNotes
                    .Where(promissoryNote =>
                        promissoryNote.Status == PatientPromissoryNoteStatus.Pending)
                    .Sum(promissoryNote => (decimal?)promissoryNote.Amount) ?? 0,
                financialCase.PromissoryNotes
                    .Where(promissoryNote =>
                        promissoryNote.Status == PatientPromissoryNoteStatus.Unpaid)
                    .Sum(promissoryNote => (decimal?)promissoryNote.Amount) ?? 0,
                financialCase.Debts
                    .Where(debt => debt.Status == PatientDebtStatus.Unpaid)
                    .Sum(debt => (decimal?)debt.Amount) ?? 0))
            .FirstOrDefaultAsync(cancellationToken);
}

public sealed class GetPatientFinancialSummaryQueryHandler(
    IPatientFinanceRepository patientFinanceRepository)
    : IQueryHandler<GetPatientFinancialSummaryQuery, PatientFinancialSummaryDto?>
{
    public async Task<PatientFinancialSummaryDto?> HandleAsync(
        GetPatientFinancialSummaryQuery request,
        CancellationToken cancellationToken = default)
    {
        if (!await patientFinanceRepository.Cases.AnyAsync(
                financialCase => financialCase.PatientId == request.PatientId,
                cancellationToken))
            return null;

        var totalAmount = await patientFinanceRepository.Cases
            .Where(financialCase =>
                financialCase.PatientId == request.PatientId &&
                financialCase.Status != PatientFinancialCaseStatus.Cancelled)
            .SumAsync(
                financialCase => (decimal?)financialCase.TotalAmount,
                cancellationToken) ?? 0;

        var paidAmount = await patientFinanceRepository.Transactions
            .Where(transaction =>
                transaction.FinancialCase.PatientId == request.PatientId)
            .SumAsync(
                transaction => (decimal?)transaction.Amount,
                cancellationToken) ?? 0;

        return new(
            request.PatientId,
            totalAmount,
            paidAmount,
            totalAmount - paidAmount,
            await patientFinanceRepository.Debts
                .Where(debt =>
                    debt.FinancialCase.PatientId == request.PatientId &&
                    debt.Status == PatientDebtStatus.Unpaid)
                .SumAsync(debt => (decimal?)debt.Amount, cancellationToken) ?? 0,
            await patientFinanceRepository.Cases.CountAsync(
                financialCase =>
                    financialCase.PatientId == request.PatientId &&
                    financialCase.Status == PatientFinancialCaseStatus.Active,
                cancellationToken),
            await patientFinanceRepository.Cheques.CountAsync(
                cheque =>
                    cheque.FinancialCase.PatientId == request.PatientId &&
                    cheque.Status == PatientChequeStatus.Unpaid,
                cancellationToken),
            await patientFinanceRepository.PromissoryNotes.CountAsync(
                promissoryNote =>
                    promissoryNote.FinancialCase.PatientId == request.PatientId &&
                    promissoryNote.Status == PatientPromissoryNoteStatus.Unpaid,
                cancellationToken));
    }
}

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