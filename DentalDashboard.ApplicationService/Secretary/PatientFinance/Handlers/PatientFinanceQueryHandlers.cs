using System.Globalization;
using DentalDashboard.ApplicationService.Contract.Secretary.PatientFinance
    .Queries;
using DentalDashboard.Domain.Models;
using DentalDashboard.Domain.Secretary.PatientFinance.Enums;
using DentalDashboard.Domain.Secretary.PatientFinance.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.ApplicationService.Secretary.PatientFinance.Handlers;
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
    var start =
        c.ToDateTime(year.Value, month.Value, 1, 0, 0, 0, DateTimeKind.Utc);
    var days = c.GetDaysInMonth(year.Value, month.Value);
    return (start, c.ToDateTime(year.Value, month.Value, days, 23, 59, 59, 999,
                                DateTimeKind.Utc));
  }
}
public sealed class GetPatientFinancialCasesQueryHandler(
    IPatientFinanceRepository repo)
    : IQueryHandler<GetPatientFinancialCasesQuery,
                    PaginatedResult<PatientFinancialCaseDto>> {
  public async Task<PaginatedResult<PatientFinancialCaseDto>>
  HandleAsync(GetPatientFinancialCasesQuery q, CancellationToken ct = default) {
    var x = repo.Cases.AsNoTracking();
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
                      (a.Patient.User.FirstName + " " + a.Patient.User.LastName)
                          .Contains(s) ||
                      a.Patient.User.PhoneNumber.Contains(s));
    }
    var (p, z) = QueryTools.Page(q);
    var count = await x.CountAsync(ct);var items=await x.OrderByDescending(a=>a.CreatedAt).Skip((p-1)*z).Take(z).Select(a=>new PatientFinancialCaseDto(a.Id,a.PatientId,(a.Patient.User.FirstName+" "+a.Patient.User.LastName).Trim(),a.Patient.User.PhoneNumber,(int)a.Service,a.Service.ToString(),a.TotalAmount,a.Transactions.Sum(t=>(decimal?)t.Amount)??0,a.TotalAmount-(a.Transactions.Sum(t=>(decimal?)t.Amount)??0),a.Debts.Where(d=>d.Status==PatientDebtStatus.Unpaid).Sum(d=>(decimal?)d.Amount)??0,a.AgreementType,a.Status,a.CreatedAt)).ToListAsync(ct);
    return new() { Items = items, TotalCount = count, PageNumber = p,
                   PageSize = z };
  }
}
public sealed class GetPatientFinancialCaseDetailsQueryHandler(
    IPatientFinanceRepository repo)
    : IQueryHandler<GetPatientFinancialCaseDetailsQuery,
                    PatientFinancialCaseDetailsDto?> {public Task<PatientFinancialCaseDetailsDto?> HandleAsync(GetPatientFinancialCaseDetailsQuery q,CancellationToken ct=default)=>repo.Cases.AsNoTracking().Where(a=>a.Id==q.PatientFinancialCaseId).Select(a=>new PatientFinancialCaseDetailsDto(new(a.Id,a.PatientId,(a.Patient.User.FirstName+" "+a.Patient.User.LastName).Trim(),a.Patient.User.PhoneNumber,(int)a.Service,a.Service.ToString(),a.TotalAmount,a.Transactions.Sum(t=>(decimal?)t.Amount)??0,a.TotalAmount-(a.Transactions.Sum(t=>(decimal?)t.Amount)??0),a.Debts.Where(d=>d.Status==PatientDebtStatus.Unpaid).Sum(d=>(decimal?)d.Amount)??0,a.AgreementType,a.Status,a.CreatedAt),a.Cheques.Count,a.Cheques.Sum(c=>(decimal?)c.Amount)??0,a.PromissoryNotes.Count,a.PromissoryNotes.Sum(n=>(decimal?)n.Amount)??0)).FirstOrDefaultAsync(ct);
}
public sealed class GetPatientFinancialCaseSummaryQueryHandler(
    IPatientFinanceRepository repo)
    : IQueryHandler<GetPatientFinancialCaseSummaryQuery,
                    PatientFinancialCaseSummaryDto?> {public Task<PatientFinancialCaseSummaryDto?> HandleAsync(GetPatientFinancialCaseSummaryQuery q,CancellationToken ct=default)=>repo.Cases.AsNoTracking().Where(a=>a.Id==q.PatientFinancialCaseId).Select(a=>new PatientFinancialCaseSummaryDto(a.TotalAmount,a.Transactions.Sum(t=>(decimal?)t.Amount)??0,a.TotalAmount-(a.Transactions.Sum(t=>(decimal?)t.Amount)??0),a.Cheques.Sum(c=>(decimal?)c.Amount)??0,a.Cheques.Where(c=>c.Status==PatientChequeStatus.Paid).Sum(c=>(decimal?)c.Amount)??0,a.Cheques.Where(c=>c.Status==PatientChequeStatus.Pending).Sum(c=>(decimal?)c.Amount)??0,a.Cheques.Where(c=>c.Status==PatientChequeStatus.Unpaid).Sum(c=>(decimal?)c.Amount)??0,a.PromissoryNotes.Sum(n=>(decimal?)n.Amount)??0,a.PromissoryNotes.Where(n=>n.Status==PatientPromissoryNoteStatus.Paid).Sum(n=>(decimal?)n.Amount)??0,a.PromissoryNotes.Where(n=>n.Status==PatientPromissoryNoteStatus.Pending).Sum(n=>(decimal?)n.Amount)??0,a.PromissoryNotes.Where(n=>n.Status==PatientPromissoryNoteStatus.Unpaid).Sum(n=>(decimal?)n.Amount)??0,a.Debts.Where(d=>d.Status==PatientDebtStatus.Unpaid).Sum(d=>(decimal?)d.Amount)??0)).FirstOrDefaultAsync(ct);
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
                       (a.FinancialCase.Patient.User.FirstName + " " +
                        a.FinancialCase.Patient.User.LastName)
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
                    (a.FinancialCase.Patient.User.FirstName + " " +
                     a.FinancialCase.Patient.User.LastName)
                        .Trim(),
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
                       (a.FinancialCase.Patient.User.FirstName + " " +
                        a.FinancialCase.Patient.User.LastName)
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
                                (a.FinancialCase.Patient.User.FirstName + " " +
                                 a.FinancialCase.Patient.User.LastName)
                                    .Trim(),
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
      x = x.Where(a => (a.FinancialCase.Patient.User.FirstName + " " +
                        a.FinancialCase.Patient.User.LastName)
                           .Contains(s) ||
                       a.FinancialCase.Patient.User.PhoneNumber.Contains(s));
    }
    var (p, z) = QueryTools.Page(q);
    var count = await x.CountAsync(ct);
    var items = await x.OrderBy(a => a.DueDate)
                    .Skip((p - 1) * z)
                    .Take(z)
                    .Select(a => new PatientDebtDto(
                                a.Id, a.FinancialCase.PatientId,
                                (a.FinancialCase.Patient.User.FirstName + " " +
                                 a.FinancialCase.Patient.User.LastName)
                                    .Trim(),
                                a.FinancialCase.Patient.User.PhoneNumber,
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
                                a.FinancialCase.PatientId, a.Amount, a.Type,
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
    var from = q.FromDate ?? DateTime.UtcNow.Date;
    var to = q.ToDate ?? from.AddDays(7);
    var cheques =
        repo.Cheques.AsNoTracking()
            .Where(x => x.Status == PatientChequeStatus.Pending &&
                        x.DueDate >= from && x.DueDate <= to &&
                        (q.PatientId == null ||
                         x.FinancialCase.PatientId == q.PatientId))
            .Select(x => new PatientFinancialCommitmentDto(
                        x.Id, PatientFinancialCommitmentType.Cheque,
                        x.PatientFinancialCaseId, x.FinancialCase.PatientId,
                        (x.FinancialCase.Patient.User.FirstName + " " +
                         x.FinancialCase.Patient.User.LastName)
                            .Trim(),
                        x.Amount, x.DueDate, (int)x.Status));
    var notes =
        repo.PromissoryNotes.AsNoTracking()
            .Where(x => x.Status == PatientPromissoryNoteStatus.Pending &&
                        x.DueDate >= from && x.DueDate <= to &&
                        (q.PatientId == null ||
                         x.FinancialCase.PatientId == q.PatientId))
            .Select(x => new PatientFinancialCommitmentDto(
                        x.Id, PatientFinancialCommitmentType.PromissoryNote,
                        x.PatientFinancialCaseId, x.FinancialCase.PatientId,
                        (x.FinancialCase.Patient.User.FirstName + " " +
                         x.FinancialCase.Patient.User.LastName)
                            .Trim(),
                        x.Amount, x.DueDate, (int)x.Status));
    var all = q.Type == PatientFinancialCommitmentType.Cheque ? cheques
              : q.Type == PatientFinancialCommitmentType.PromissoryNote
                  ? notes
                  : cheques.Concat(notes);
    var (p, z) = QueryTools.Page(q);
    var count = await all.CountAsync(ct);
    var items = await all.OrderBy(x => x.DueDate)
                    .Skip((p - 1) * z)
                    .Take(z)
                    .ToListAsync(ct);
    return new() { Items = items, TotalCount = count, PageNumber = p,
                   PageSize = z };
  }
}
