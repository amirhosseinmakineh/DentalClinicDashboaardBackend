using System.Globalization;
using DentalDashboard.ApplicationService.Contract.Secretary.Accountant.PatientFinance
    .Queries;
using DentalDashboard.Domain.Models;
using DentalDashboard.Domain.Secretary.Accountant.PatientFinance.Entities;
using DentalDashboard.Domain.Secretary.Accountant.PatientFinance.Enums;
using DentalDashboard.Domain.Secretary.Accountant.PatientFinance.IRepositories;
using DentalDashboard.Utilities.Time;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.ApplicationService.Secretary.Accountant.PatientFinance.Handlers;
internal static class QueryTools {
  public static (int page, int size)
      Page(PatientFinancePagedQuery q) => (Math.Max(1, q.Page),
                                           Math.Clamp(q.PageSize, 1, 100));
  public static string Name(string first,
                            string last) => (first + " " + last).Trim();
  public static (DateTime start, DateTime end)? PersianMonth(int? year,
                                                             int? month) {
    if (year is null && month is null)
      return null;
    if (year is null || month is null || month < 1 || month > 12)
      throw new ArgumentException("سال و ماه باید با هم و معتبر ارسال شوند");
    var c = new PersianCalendar();
    var start = DateTime.SpecifyKind(
        c.ToDateTime(year.Value, month.Value, 1, 0, 0, 0, 0),
        DateTimeKind.Utc);
    var days = c.GetDaysInMonth(year.Value, month.Value);
    var end = DateTime.SpecifyKind(
        c.ToDateTime(year.Value, month.Value, days, 23, 59, 59, 999, 0),
        DateTimeKind.Utc);
    return (start, end);
  }
}
public sealed class GetPatientFinancialCasesQueryHandler(
    IPatientFinanceRepository repo)
    : IQueryHandler<GetPatientFinancialCasesQuery,
                    PaginatedResult<PatientFinancialCaseDto>> {
  public async Task<PaginatedResult<PatientFinancialCaseDto>>
  HandleAsync(GetPatientFinancialCasesQuery q, CancellationToken ct = default) {
    var x = repo.Cases.AsNoTracking().AsQueryable();
    if (q.PatientId.HasValue)
      x = x.Where(a => a.PatientId == q.PatientId);
    if (q.ServiceId.HasValue)
      x = x.Where(a => (int)a.Service == q.ServiceId);
    if (q.AgreementType.HasValue)
      x = x.Where(a => a.AgreementType == q.AgreementType);
    if (q.Status.HasValue)
      x = x.Where(a => a.Status == q.Status);
    if (q.FromDate.HasValue)
      x = x.Where(a => a.CreatedAt >= q.FromDate);
    if (q.ToDate.HasValue)
      x = x.Where(a => a.CreatedAt <= q.ToDate);
    if (!string.IsNullOrWhiteSpace(q.Search)) {
      var s = q.Search.Trim();
      x = x.Where(a =>
                      (a.Patient.FirstName + " " + a.Patient.LastName)
                          .Contains(s) ||
                      a.Patient.PhoneNumber.Contains(s));
    }
    var (p, z) = QueryTools.Page(q);
    // This endpoint is the patient list for the finance area. A patient can
    // have several financial cases, but must only consume one row/page slot.
    // The most recently created matching case represents that patient.
    var count = await x.Select(a => a.PatientId).Distinct().CountAsync(ct);
    var patientIds = await x.GroupBy(a => a.PatientId)
        .Select(g => new { PatientId = g.Key, LastCaseAt = g.Max(a => a.CreatedAt) })
        .OrderByDescending(a => a.LastCaseAt)
        .ThenBy(a => a.PatientId)
        .Skip((p - 1) * z)
        .Take(z)
        .Select(a => a.PatientId)
        .ToListAsync(ct);

    var matchingCases = await x.Where(a => patientIds.Contains(a.PatientId))
        .Select(a => new PatientFinancialCaseDto(
            a.Id, a.PatientId, a.Patient.Id,
            (a.Patient.FirstName + " " + a.Patient.LastName).Trim(),
            repo.PatientFiles.Where(f => f.PhoneNumber == a.Patient.PhoneNumber)
                .Select(f => f.FileNumber.ToString()).FirstOrDefault() ?? "",
            a.Patient.PhoneNumber, (int)a.Service, a.Service == DentalDashboard.Domain.Enums.DentalServiceType.Composite ? "کامپوزیت" : a.Service == DentalDashboard.Domain.Enums.DentalServiceType.Implant ? "ایمپلنت" : a.Service == DentalDashboard.Domain.Enums.DentalServiceType.Laminate ? "لمینت" : a.Service.ToString(),
            a.TotalAmount, a.PrePaymentAmount, a.DepositAmount,
            a.Transactions.Where(t => t.Type == PatientFinancialTransactionType.Payment).Sum(t => (decimal?)t.Amount) ?? 0,
            Math.Max(a.TotalAmount - (a.Transactions.Where(t => t.Type == PatientFinancialTransactionType.Payment).Sum(t => (decimal?)t.Amount) ?? 0), 0),
            a.Debts.Where(d => d.Status == PatientDebtStatus.Unpaid)
                .Sum(d => (decimal?)d.Amount) ?? 0,
            a.AgreementType, a.Status, a.CreatedAt))
        .ToListAsync(ct);

    var casesByPatient = matchingCases.GroupBy(a => a.PatientId)
        .ToDictionary(g => g.Key,
            g => g.OrderByDescending(a => a.CreatedAt).ThenByDescending(a => a.Id).First());
    var items = patientIds.Select(id => casesByPatient[id]).ToList();
    return new() { Items = items, TotalCount = count, PageNumber = p,
                   PageSize = z };
  }
}
public sealed class GetPatientFinancialCaseDetailsQueryHandler(
    IPatientFinanceRepository repo)
    : IQueryHandler<GetPatientFinancialCaseDetailsQuery,
                    PatientFinancialCaseDetailsDto?> {
  public Task<PatientFinancialCaseDetailsDto?> HandleAsync(
      GetPatientFinancialCaseDetailsQuery q, CancellationToken ct = default) =>
      repo.Cases.AsNoTracking()
          .Where(a => a.Id == q.PatientFinancialCaseId)
          .Select(a => new PatientFinancialCaseDetailsDto(
              new(a.Id, a.PatientId, a.Patient.Id,
                  (a.Patient.FirstName + " " + a.Patient.LastName).Trim(),
                  repo.PatientFiles
                      .Where(f => f.PhoneNumber == a.Patient.PhoneNumber)
                      .Select(f => f.FileNumber.ToString()).FirstOrDefault() ?? "",
                  a.Patient.PhoneNumber, (int)a.Service, a.Service == DentalDashboard.Domain.Enums.DentalServiceType.Composite ? "کامپوزیت" : a.Service == DentalDashboard.Domain.Enums.DentalServiceType.Implant ? "ایمپلنت" : a.Service == DentalDashboard.Domain.Enums.DentalServiceType.Laminate ? "لمینت" : a.Service.ToString(),
                  a.TotalAmount, a.PrePaymentAmount, a.DepositAmount,
                  a.Transactions.Where(t => t.Type == PatientFinancialTransactionType.Payment).Sum(t => (decimal?)t.Amount) ?? 0,
                  Math.Max(a.TotalAmount -
                      (a.Transactions.Where(t => t.Type == PatientFinancialTransactionType.Payment).Sum(t => (decimal?)t.Amount) ?? 0), 0),
                  a.Debts.Where(d => d.Status == PatientDebtStatus.Unpaid)
                      .Sum(d => (decimal?)d.Amount) ?? 0,
                  a.AgreementType, a.Status, a.CreatedAt),
              a.Cheques.Count(c => c.Status != PatientChequeStatus.Cancelled),
              a.Cheques.Where(c => c.Status != PatientChequeStatus.Cancelled).Sum(c => (decimal?)c.Amount) ?? 0,
              a.PromissoryNotes.Count(n => n.Status != PatientPromissoryNoteStatus.Cancelled),
              a.PromissoryNotes.Where(n => n.Status != PatientPromissoryNoteStatus.Cancelled).Sum(n => (decimal?)n.Amount) ?? 0,
              a.Cheques.OrderBy(c => c.DueDate).ThenBy(c => c.Id)
                  .Select(c => new PatientChequeDto(
                      c.Id, c.PatientFinancialCaseId, a.PatientId,
                      (a.Patient.FirstName + " " + a.Patient.LastName).Trim(),
                      repo.PatientFiles
                          .Where(f => f.PhoneNumber == a.Patient.PhoneNumber)
                          .Select(f => f.FileNumber.ToString()).FirstOrDefault() ?? "",
                      c.Amount, c.SayadNumber, c.OwnerName, c.DueDate, c.Status))
                  .ToList(),
              a.PromissoryNotes.OrderBy(n => n.DueDate).ThenBy(n => n.Id)
                  .Select(n => new PatientPromissoryNoteDto(
                      n.Id, n.PatientFinancialCaseId, a.PatientId,
                      (a.Patient.FirstName + " " + a.Patient.LastName).Trim(),
                      repo.PatientFiles
                          .Where(f => f.PhoneNumber == a.Patient.PhoneNumber)
                          .Select(f => f.FileNumber.ToString()).FirstOrDefault() ?? "",
                      n.SerialNumber, n.Amount, n.DueDate, n.Status)).ToList()))
          .FirstOrDefaultAsync(ct);
}
public sealed class GetPatientFinancialCaseSummaryQueryHandler(
    IPatientFinanceRepository patientFinanceRepository)
    : IQueryHandler<GetPatientFinancialCaseSummaryQuery,
                    PatientFinancialCaseSummaryDto?> {
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
    IPatientFinanceRepository repo)
    : IQueryHandler<GetPatientFinancialSummaryQuery,
                    PatientFinancialSummaryDto?> {
  public async Task<PatientFinancialSummaryDto?>
  HandleAsync(GetPatientFinancialSummaryQuery q,
              CancellationToken ct = default) {
    if (!await repo.Cases.AnyAsync(x => x.PatientId == q.PatientId, ct))
      return null;var total=await repo.Cases.Where(x=>x.PatientId==q.PatientId&&x.Status!=PatientFinancialCaseStatus.Cancelled).SumAsync(x=>(decimal?)x.TotalAmount,ct)??0;var paid=await repo.Transactions.Where(x=>x.FinancialCase.PatientId==q.PatientId).SumAsync(x=>(decimal?)x.Amount,ct)??0;return new(q.PatientId,total,paid,total-paid,await repo.Debts.Where(x=>x.FinancialCase.PatientId==q.PatientId&&x.Status==PatientDebtStatus.Unpaid).SumAsync(x=>(decimal?)x.Amount,ct)??0,await repo.Cases.CountAsync(x=>x.PatientId==q.PatientId&&x.Status==PatientFinancialCaseStatus.Active,ct),await repo.Cheques.CountAsync(x=>x.FinancialCase.PatientId==q.PatientId&&x.Status==PatientChequeStatus.Unpaid,ct),await repo.PromissoryNotes.CountAsync(x=>x.FinancialCase.PatientId==q.PatientId&&x.Status==PatientPromissoryNoteStatus.Unpaid,ct));
  }
}
public sealed class GetPatientChequesQueryHandler(
    IPatientFinanceRepository repo)
    : IQueryHandler<GetPatientChequesQuery, PaginatedResult<PatientChequeDto>> {
  public async Task<PaginatedResult<PatientChequeDto>>
  HandleAsync(GetPatientChequesQuery q, CancellationToken ct = default) {
    var x = repo.Cheques.AsNoTracking();
    if (q.PatientFinancialCaseId.HasValue)
      x = x.Where(a => a.PatientFinancialCaseId == q.PatientFinancialCaseId);
    if (q.PatientId.HasValue)
      x = x.Where(a => a.FinancialCase.PatientId == q.PatientId);
    if (q.Status.HasValue)
      x = x.Where(a => a.Status == q.Status);
    if (q.FromDueDate.HasValue)
      x = x.Where(a => a.DueDate >= q.FromDueDate);
    if (q.ToDueDate.HasValue)
      x = x.Where(a => a.DueDate <= q.ToDueDate);
    if (!string.IsNullOrWhiteSpace(q.Search)) {
      var s = q.Search.Trim();
      x = x.Where(a => a.SayadNumber.Contains(s) || a.OwnerName.Contains(s) ||
                       (a.FinancialCase.Patient.FirstName + " " +
                        a.FinancialCase.Patient.LastName)
                           .Contains(s));
    }
    var (p, z) = QueryTools.Page(q);
    var count = await x.CountAsync(ct);
    var items =
        await x.OrderBy(a => a.DueDate)
            .Skip((p - 1) * z)
            .Take(z)
            .Select(
                a => new PatientChequeDto(
                    a.Id, a.PatientFinancialCaseId, a.FinancialCase.PatientId,
                    (a.FinancialCase.Patient.FirstName + " " +
                     a.FinancialCase.Patient.LastName)
                        .Trim(),
                    repo.PatientFiles
                        .Where(f => f.PhoneNumber == a.FinancialCase.Patient.PhoneNumber)
                        .Select(f => f.FileNumber.ToString()).FirstOrDefault() ?? "",
                    a.Amount, a.SayadNumber, a.OwnerName, a.DueDate, a.Status))
            .ToListAsync(ct);
    return new() { Items = items, TotalCount = count, PageNumber = p,
                   PageSize = z };
  }
}
public sealed class GetPatientPromissoryNotesQueryHandler(
    IPatientFinanceRepository repo)
    : IQueryHandler<GetPatientPromissoryNotesQuery,
                    PaginatedResult<PatientPromissoryNoteDto>> {
  public async Task<PaginatedResult<PatientPromissoryNoteDto>>
  HandleAsync(GetPatientPromissoryNotesQuery q,
              CancellationToken ct = default) {
    var x = repo.PromissoryNotes.AsNoTracking();
    if (q.PatientFinancialCaseId.HasValue)
      x = x.Where(a => a.PatientFinancialCaseId == q.PatientFinancialCaseId);
    if (q.PatientId.HasValue)
      x = x.Where(a => a.FinancialCase.PatientId == q.PatientId);
    if (q.Status.HasValue)
      x = x.Where(a => a.Status == q.Status);
    if (q.FromDueDate.HasValue)
      x = x.Where(a => a.DueDate >= q.FromDueDate);
    if (q.ToDueDate.HasValue)
      x = x.Where(a => a.DueDate <= q.ToDueDate);
    if (!string.IsNullOrWhiteSpace(q.Search)) {
      var s = q.Search.Trim();
      x = x.Where(a => a.SerialNumber.Contains(s) ||
                       (a.FinancialCase.Patient.FirstName + " " +
                        a.FinancialCase.Patient.LastName)
                           .Contains(s));
    }
    var (p, z) = QueryTools.Page(q);
    var count = await x.CountAsync(ct);
    var items = await x.OrderBy(a => a.DueDate)
                    .Skip((p - 1) * z)
                    .Take(z)
                    .Select(a => new PatientPromissoryNoteDto(
                                a.Id, a.PatientFinancialCaseId,
                                a.FinancialCase.PatientId,
                                (a.FinancialCase.Patient.FirstName + " " +
                                 a.FinancialCase.Patient.LastName)
                                    .Trim(),
                                repo.PatientFiles
                                    .Where(f => f.PhoneNumber == a.FinancialCase.Patient.PhoneNumber)
                                    .Select(f => f.FileNumber.ToString()).FirstOrDefault() ?? "",
                                a.SerialNumber, a.Amount, a.DueDate, a.Status))
                    .ToListAsync(ct);
    return new() { Items = items, TotalCount = count, PageNumber = p,
                   PageSize = z };
  }
}
public sealed class GetPatientDebtsQueryHandler(IPatientFinanceRepository repo)
    : IQueryHandler<GetPatientDebtsQuery, PaginatedResult<PatientDebtDto>> {
  public async Task<PaginatedResult<PatientDebtDto>>
  HandleAsync(GetPatientDebtsQuery q, CancellationToken ct = default) {
    var x = repo.Debts.AsNoTracking();
    if (q.PatientId.HasValue)
      x = x.Where(a => a.FinancialCase.PatientId == q.PatientId);
    if (q.PatientFinancialCaseId.HasValue)
      x = x.Where(a => a.PatientFinancialCaseId == q.PatientFinancialCaseId);
    if (q.SourceType.HasValue)
      x = x.Where(a => a.SourceType == q.SourceType);
    if (q.Status.HasValue)
      x = x.Where(a => a.Status == q.Status);
    if (q.Status == PatientDebtStatus.Unpaid)
      x = x.Where(a =>
          (a.SourceType == PatientDebtSourceType.Cheque
               ? repo.Cheques.Any(c => c.Id == a.SourceId &&
                                       c.Status == PatientChequeStatus.Unpaid)
               : repo.PromissoryNotes.Any(n => n.Id == a.SourceId &&
                   n.Status == PatientPromissoryNoteStatus.Unpaid)) &&
          !repo.Transactions.Any(t =>
              t.SourceId == a.SourceId &&
              t.Type == PatientFinancialTransactionType.Payment &&
              ((a.SourceType == PatientDebtSourceType.Cheque &&
                t.SourceType == PatientFinancialTransactionSourceType.Cheque) ||
               (a.SourceType == PatientDebtSourceType.PromissoryNote &&
                t.SourceType == PatientFinancialTransactionSourceType.PromissoryNote))));
    var month = QueryTools.PersianMonth(q.Year, q.Month);
    if (month.HasValue)
      x = x.Where(a => a.DueDate >= month.Value.start &&
                       a.DueDate <= month.Value.end);
    if (q.FromDueDate.HasValue)
      x = x.Where(a => a.DueDate >= q.FromDueDate);
    if (q.ToDueDate.HasValue)
      x = x.Where(a => a.DueDate <= q.ToDueDate);
    if (!string.IsNullOrWhiteSpace(q.Search)) {
      var s = q.Search.Trim();
      x = x.Where(a => (a.FinancialCase.Patient.FirstName + " " +
                        a.FinancialCase.Patient.LastName)
                           .Contains(s) ||
                       a.FinancialCase.Patient.PhoneNumber.Contains(s));
    }
    var (p, z) = QueryTools.Page(q);
    var count = await x.CountAsync(ct);
    var items = await x.OrderBy(a => a.DueDate)
                    .Skip((p - 1) * z)
                    .Take(z)
                    .Select(a => new PatientDebtDto(
                                a.Id, a.FinancialCase.PatientId,
                                (a.FinancialCase.Patient.FirstName + " " +
                                 a.FinancialCase.Patient.LastName)
                                    .Trim(),
                                repo.PatientFiles
                                    .Where(f => f.PhoneNumber == a.FinancialCase.Patient.PhoneNumber)
                                    .Select(f => f.FileNumber.ToString()).FirstOrDefault() ?? "",
                                a.FinancialCase.Patient.PhoneNumber,
                                a.PatientFinancialCaseId,
                                a.FinancialCase.Service.ToString(), a.Amount,
                                a.SourceType, a.SourceId, a.DueDate, a.Status))
                    .ToListAsync(ct);
    return new() { Items = items, TotalCount = count, PageNumber = p,
                   PageSize = z };
  }
}
public sealed class GetPatientFinancialTransactionsQueryHandler(
    IPatientFinanceRepository repo)
    : IQueryHandler<GetPatientFinancialTransactionsQuery,
                    PaginatedResult<PatientFinancialTransactionDto>> {
  public async Task<PaginatedResult<PatientFinancialTransactionDto>>
  HandleAsync(GetPatientFinancialTransactionsQuery q,
              CancellationToken ct = default) {
    var x = repo.Transactions.AsNoTracking();
    if (q.PatientId.HasValue)
      x = x.Where(a => a.FinancialCase.PatientId == q.PatientId);
    if (q.PatientFinancialCaseId.HasValue)
      x = x.Where(a => a.PatientFinancialCaseId == q.PatientFinancialCaseId);
    if (q.SourceType.HasValue)
      x = x.Where(a => a.SourceType == q.SourceType);
    if (q.FromDate.HasValue)
      x = x.Where(a => a.CreatedAt >= q.FromDate);
    if (q.ToDate.HasValue)
      x = x.Where(a => a.CreatedAt <= q.ToDate);
    var (p, z) = QueryTools.Page(q);
    var count = await x.CountAsync(ct);
    var items = await x.OrderByDescending(a => a.CreatedAt)
                    .Skip((p - 1) * z)
                    .Take(z)
                    .Select(a => new PatientFinancialTransactionDto(
                                a.Id, a.PatientFinancialCaseId,
                                a.FinancialCase.PatientId,
                                (a.FinancialCase.Patient.FirstName + " " +
                                 a.FinancialCase.Patient.LastName).Trim(),
                                repo.PatientFiles
                                    .Where(f => f.PhoneNumber == a.FinancialCase.Patient.PhoneNumber)
                                    .Select(f => f.FileNumber.ToString()).FirstOrDefault() ?? "",
                                a.Amount, a.Type,
                                a.SourceType, a.SourceId, a.CreatedAt))
                    .ToListAsync(ct);
    return new() { Items = items, TotalCount = count, PageNumber = p,
                   PageSize = z };
  }
}
public sealed class GetDuePatientFinancialCommitmentsQueryHandler(
    IPatientFinanceRepository repo)
    : IQueryHandler<GetDuePatientFinancialCommitmentsQuery,
                    PaginatedResult<PatientFinancialCommitmentDto>> {
  public async Task<PaginatedResult<PatientFinancialCommitmentDto>>
  HandleAsync(GetDuePatientFinancialCommitmentsQuery q,
              CancellationToken ct = default) {
    var today = IranTimeHelper.TodayInIran();
    var defaultTo = IranTimeHelper.GetIranDayRangeAsUtc(today.AddDays(3)).EndUtc;
    var from = q.FromDate;
    var to = q.ToDate ?? defaultTo;
    var cheques =
        repo.Cheques.AsNoTracking()
            .Where(x => x.Status == PatientChequeStatus.Pending &&
                        (!from.HasValue || x.DueDate >= from.Value) &&
                        x.DueDate <= to &&
                        (q.PatientId == null ||
                         x.FinancialCase.PatientId == q.PatientId));
    var notes =
        repo.PromissoryNotes.AsNoTracking()
            .Where(x => x.Status == PatientPromissoryNoteStatus.Pending &&
                        (!from.HasValue || x.DueDate >= from.Value) &&
                        x.DueDate <= to &&
                        (q.PatientId == null ||
                         x.FinancialCase.PatientId == q.PatientId));
    IQueryable<PatientFinancialCommitmentDto> SelectCheques(
        IQueryable<PatientCheque> query) =>
        query.Select(x => new PatientFinancialCommitmentDto(
                        x.Id, PatientFinancialCommitmentType.Cheque,
                        x.PatientFinancialCaseId, x.FinancialCase.PatientId,
                        (x.FinancialCase.Patient.FirstName + " " +
                         x.FinancialCase.Patient.LastName)
                            .Trim(),
                        repo.PatientFiles
                            .Where(f => f.PhoneNumber == x.FinancialCase.Patient.PhoneNumber)
                            .Select(f => f.FileNumber.ToString()).FirstOrDefault() ?? "",
                        x.Amount, x.DueDate, (int)x.Status));
    IQueryable<PatientFinancialCommitmentDto> SelectNotes(
        IQueryable<PatientPromissoryNote> query) =>
        query.Select(x => new PatientFinancialCommitmentDto(
                        x.Id, PatientFinancialCommitmentType.PromissoryNote,
                        x.PatientFinancialCaseId, x.FinancialCase.PatientId,
                        (x.FinancialCase.Patient.FirstName + " " +
                         x.FinancialCase.Patient.LastName)
                            .Trim(),
                        repo.PatientFiles
                            .Where(f => f.PhoneNumber == x.FinancialCase.Patient.PhoneNumber)
                            .Select(f => f.FileNumber.ToString()).FirstOrDefault() ?? "",
                        x.Amount, x.DueDate, (int)x.Status));
    var (p, z) = QueryTools.Page(q);
    var skip = (p - 1) * z;

    if (q.Type == PatientFinancialCommitmentType.Cheque) {
      var selectedCount = await cheques.CountAsync(ct);
      var selectedItems = await SelectCheques(
                                  cheques.OrderBy(x => x.DueDate)
                                      .ThenBy(x => x.Id)
                                      .Skip(skip)
                                      .Take(z))
                              .ToListAsync(ct);
      return new() { Items = selectedItems, TotalCount = selectedCount,
                     PageNumber = p, PageSize = z };
    }
    if (q.Type == PatientFinancialCommitmentType.PromissoryNote) {
      var selectedCount = await notes.CountAsync(ct);
      var selectedItems = await SelectNotes(
                                  notes.OrderBy(x => x.DueDate)
                                      .ThenBy(x => x.Id)
                                      .Skip(skip)
                                      .Take(z))
                              .ToListAsync(ct);
      return new() { Items = selectedItems, TotalCount = selectedCount,
                     PageNumber = p, PageSize = z };
    }

    // Applying Concat after these DTO projections is not translatable by EF
    // Core because each projection contains a correlated patient-file lookup.
    // Read only the prefix needed from each source, then merge that small result
    // in memory so global ordering and pagination remain correct.
    var chequeCount = await cheques.CountAsync(ct);
    var noteCount = await notes.CountAsync(ct);
    var windowSize = skip + z;
    var chequeWindow = await SelectCheques(
                                   cheques.OrderBy(x => x.DueDate)
                                       .ThenBy(x => x.Id)
                                       .Take(windowSize))
                               .ToListAsync(ct);
    var noteWindow = await SelectNotes(
                                 notes.OrderBy(x => x.DueDate)
                                     .ThenBy(x => x.Id)
                                     .Take(windowSize))
                             .ToListAsync(ct);
    var items = chequeWindow.Concat(noteWindow)
                    .OrderBy(x => x.DueDate)
                    .ThenBy(x => x.Type)
                    .ThenBy(x => x.Id)
                    .Skip(skip)
                    .Take(z)
                    .ToList();
    var count = chequeCount + noteCount;
    return new() { Items = items, TotalCount = count, PageNumber = p,
                   PageSize = z };
  }
}
