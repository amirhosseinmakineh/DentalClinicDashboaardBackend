using ClosedXML.Excel;
using DentalDashboard.Domain.Secretary.Accountant.PatientFinance.Enums;
using DentalDashboard.Domain.Secretary.Accountant.PatientFinance.IRepositories;
using DentalDashboard.Utilities.Time;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.Services;

public sealed class PatientFinanceAdminReportFilter
{
    public string? Search { get; set; }
    public string? PatientName { get; set; }
    public long? FileNumber { get; set; }
    public int? ServiceId { get; set; }
    public PatientFinancialAgreementType? AgreementType { get; set; }
    public PatientFinancialCaseStatus? Status { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public sealed record PatientFinanceAdminReportItem(
    Guid CaseId,
    Guid PatientId,
    string PatientName,
    string PhoneNumber,
    string FileNumber,
    int ServiceId,
    string ServiceName,
    decimal TotalAmount,
    decimal PrePaymentAmount,
    decimal DepositAmount,
    decimal PaidAmount,
    decimal RemainingAmount,
    decimal UnpaidDebtAmount,
    decimal ChequeAmount,
    decimal PromissoryNoteAmount,
    PatientFinancialAgreementType AgreementType,
    PatientFinancialCaseStatus Status,
    string CreatedBy,
    DateTime CreatedAt)
{
    public decimal BalanceAmount { get; init; }
    public string? PaymentMethod { get; init; }
    public string? InstallmentStatus { get; init; }
    public string? GuaranteeDocument { get; init; }
    public DateTime? GuaranteeDate { get; init; }
    public decimal? GuaranteeAmount { get; init; }
    public string? GuaranteeChequeRegistration { get; init; }
    public string? Notes { get; init; }
    public string? ConsultantName { get; init; }
    public string? ReviewItems { get; init; }
    public DateTime? ChequeDate { get; init; }
    public string? ChequeRegistration { get; init; }
    public IReadOnlyList<DateTime> ChequeDates { get; init; } = [];
    public IReadOnlyList<string> ChequeRegistrations { get; init; } = [];
}


public sealed record PatientFinanceAdminReportSummary(
    int CaseCount,
    decimal TotalAmount,
    decimal PrePaymentAmount,
    decimal DepositAmount,
    decimal PaidAmount,
    decimal RemainingAmount,
    decimal UnpaidDebtAmount,
    decimal ChequeAmount,
    decimal PromissoryNoteAmount);

public sealed record PatientFinanceAdminReportResult(
    IReadOnlyList<PatientFinanceAdminReportItem> Items,
    int TotalCount,
    int PageNumber,
    int PageSize,
    PatientFinanceAdminReportSummary Summary);

public sealed class PatientFinanceAdminReportService(IPatientFinanceRepository repository)
{
    public async Task<PatientFinanceAdminReportResult> GetAsync(
        PatientFinanceAdminReportFilter filter,
        CancellationToken cancellationToken = default)
    {
        var query = BuildQuery(filter);
        var summary = await BuildSummaryAsync(query, cancellationToken);
        var page = Math.Max(1, filter.Page);
        var pageSize = Math.Clamp(filter.PageSize, 1, 100);
        var totalCount = await query.CountAsync(cancellationToken);
        var projectedItems = await Project(query
            .OrderByDescending(item => item.CreatedAt)
            .ThenByDescending(item => item.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize))
            .ToListAsync(cancellationToken);
        var items = await WithAllChequesAsync(projectedItems, cancellationToken);
        return new(items, totalCount, page, pageSize, summary);
    }

    public async Task<byte[]> ExportExcelAsync(
        PatientFinanceAdminReportFilter filter,
        CancellationToken cancellationToken = default)
    {
        var query = BuildQuery(filter);
        var projectedItems = await Project(query
            .OrderByDescending(item => item.CreatedAt)
            .ThenByDescending(item => item.Id))
            .ToListAsync(cancellationToken);
        var items = await WithAllChequesAsync(projectedItems, cancellationToken);

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("گزارش حسابداری بیماران");
        sheet.RightToLeft = true;
        var headers = new[]
        {
            "ردیف", "تاریخ", "نام و نام خانوادگی", "شرح خدمات", "مبلغ خدمات",
            "نحوه پرداخت", "وضعیت اقساط", "تاریخ چک", "ثبت چک", "مبلغ پرداختی",
            "بیعانه", "مانده بدهکاری/بستانکاری", "سند تضمین", "تاریخ ضمانت",
            "مبلغ ضمانت", "ثبت چک ضمانت", "توضیحات", "نام مشاور", "مواردی که باید چک شود"
        };

        for (var column = 0; column < headers.Length; column++)
            sheet.Cell(1, column + 1).Value = headers[column];

        for (var index = 0; index < items.Count; index++)
        {
            var item = items[index];
            var row = index + 2;
            sheet.Cell(row, 1).Value = index + 1;
            sheet.Cell(row, 2).Value = item.CreatedAt;
            sheet.Cell(row, 3).Value = item.PatientName;
            sheet.Cell(row, 4).Value = item.ServiceName;
            sheet.Cell(row, 5).Value = item.TotalAmount;
            sheet.Cell(row, 6).Value = item.PaymentMethod ?? "";
            sheet.Cell(row, 7).Value = item.InstallmentStatus ?? "";
            sheet.Cell(row, 8).Value = string.Join("، ", item.ChequeDates.Select(date => date.ToString("yyyy/MM/dd")));
            sheet.Cell(row, 9).Value = string.Join("، ", item.ChequeRegistrations);
            sheet.Cell(row, 10).Value = item.PaidAmount;
            sheet.Cell(row, 11).Value = item.DepositAmount;
            sheet.Cell(row, 12).Value = item.BalanceAmount;
            sheet.Cell(row, 13).Value = item.GuaranteeDocument ?? "";
            if (item.GuaranteeDate.HasValue) sheet.Cell(row, 14).Value = item.GuaranteeDate.Value;
            if (item.GuaranteeAmount.HasValue) sheet.Cell(row, 15).Value = item.GuaranteeAmount.Value;
            sheet.Cell(row, 16).Value = item.GuaranteeChequeRegistration ?? "";
            sheet.Cell(row, 17).Value = item.Notes ?? "";
            sheet.Cell(row, 18).Value = item.ConsultantName ?? "";
            sheet.Cell(row, 19).Value = item.ReviewItems ?? "";
        }

        var headerRange = sheet.Range(1, 1, 1, headers.Length);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#0F766E");
        headerRange.Style.Font.FontColor = XLColor.White;
        foreach (var column in new[] { 5, 10, 11, 12, 15 })
            sheet.Column(column).Style.NumberFormat.Format = "#,##0.###";
        sheet.Column(2).Style.DateFormat.Format = "yyyy/MM/dd HH:mm";
        sheet.Column(14).Style.DateFormat.Format = "yyyy/MM/dd";
        sheet.SheetView.FreezeRows(1);
        sheet.RangeUsed()?.SetAutoFilter();
        sheet.Columns().AdjustToContents(10, 35);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private IQueryable<DentalDashboard.Domain.Secretary.Accountant.PatientFinance.Entities.PatientFinancialCase> BuildQuery(
        PatientFinanceAdminReportFilter filter)
    {
        var query = repository.Cases.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim();
            query = query.Where(item =>
                (item.Patient.FirstName + " " + item.Patient.LastName).Contains(search) ||
                item.Patient.PhoneNumber.Contains(search) ||
                repository.PatientFiles.Any(file =>
                    file.PhoneNumber == item.Patient.PhoneNumber &&
                    file.FileNumber.ToString().Contains(search)));
        }

        if (!string.IsNullOrWhiteSpace(filter.PatientName))
        {
            var patientName = filter.PatientName.Trim();
            query = query.Where(item =>
                (item.Patient.FirstName + " " + item.Patient.LastName).Contains(patientName));
        }

        if (filter.FileNumber.HasValue)
        {
            var fileNumber = filter.FileNumber.Value;
            query = query.Where(item => repository.PatientFiles.Any(file =>
                file.PhoneNumber == item.Patient.PhoneNumber &&
                file.FileNumber == fileNumber));
        }

        if (filter.ServiceId.HasValue)
            query = query.Where(item => (int)item.Service == filter.ServiceId.Value);
        if (filter.AgreementType.HasValue)
            query = query.Where(item => item.AgreementType == filter.AgreementType.Value);
        if (filter.Status.HasValue)
            query = query.Where(item => item.Status == filter.Status.Value);
        if (filter.FromDate.HasValue)
        {
            var fromDate = DateOnly.FromDateTime(filter.FromDate.Value);
            var (startUtc, _) = IranTimeHelper.GetIranDayRangeAsUtc(fromDate);
            query = query.Where(item => item.CreatedAt >= startUtc);
        }

        if (filter.ToDate.HasValue)
        {
            var toDate = DateOnly.FromDateTime(filter.ToDate.Value);
            var (nextDayStartUtc, _) = IranTimeHelper.GetIranDayRangeAsUtc(toDate.AddDays(1));
            query = query.Where(item => item.CreatedAt < nextDayStartUtc);
        }

        return query;
    }

    private IQueryable<PatientFinanceAdminReportItem> Project(
        IQueryable<DentalDashboard.Domain.Secretary.Accountant.PatientFinance.Entities.PatientFinancialCase> query) =>
        query.Select(item => new PatientFinanceAdminReportItem(
            item.Id,
            item.PatientId,
            (item.Patient.FirstName + " " + item.Patient.LastName).Trim(),
            item.Patient.PhoneNumber,
            repository.PatientFiles
                .Where(file => file.PhoneNumber == item.Patient.PhoneNumber)
                .OrderByDescending(file => file.CreatedAt)
                .Select(file => file.FileNumber.ToString())
                .FirstOrDefault() ?? "",
            (int)item.Service,
            item.Service == DentalDashboard.Domain.Enums.DentalServiceType.Composite ? "کامپوزیت" :
            item.Service == DentalDashboard.Domain.Enums.DentalServiceType.Implant ? "ایمپلنت" :
            item.Service == DentalDashboard.Domain.Enums.DentalServiceType.Laminate ? "لمینت" : item.Service.ToString(),
            item.TotalAmount,
            item.PrePaymentAmount,
            item.DepositAmount,
            item.PrePaymentAmount + item.DepositAmount +
            (item.Transactions.Where(transaction => transaction.Type == PatientFinancialTransactionType.Payment)
                .Sum(transaction => (decimal?)transaction.Amount) ?? 0),
            Math.Max(item.TotalAmount - item.PrePaymentAmount - item.DepositAmount -
                (item.Transactions.Where(transaction => transaction.Type == PatientFinancialTransactionType.Payment)
                    .Sum(transaction => (decimal?)transaction.Amount) ?? 0), 0),
            item.Debts.Where(debt => debt.Status == PatientDebtStatus.Unpaid)
                .Sum(debt => (decimal?)debt.Amount) ?? 0,
            item.Cheques.Where(cheque => cheque.Status != PatientChequeStatus.Cancelled)
                .Sum(cheque => (decimal?)cheque.Amount) ?? 0,
            item.PromissoryNotes.Where(note => note.Status != PatientPromissoryNoteStatus.Cancelled)
                .Sum(note => (decimal?)note.Amount) ?? 0,
            item.AgreementType,
            item.Status,
            (item.CreatedByUser.FirstName + " " + item.CreatedByUser.LastName).Trim(),
            item.CreatedAt) { BalanceAmount = item.TotalAmount - item.PrePaymentAmount - item.DepositAmount - (item.Transactions.Where(transaction => transaction.Type == PatientFinancialTransactionType.Payment).Sum(transaction => (decimal?)transaction.Amount) ?? 0), PaymentMethod = item.PaymentMethod, InstallmentStatus = item.InstallmentStatus, GuaranteeDocument = item.GuaranteeDocument, GuaranteeDate = item.GuaranteeDate, GuaranteeAmount = item.GuaranteeAmount, GuaranteeChequeRegistration = item.GuaranteeChequeRegistration, Notes = item.Notes, ConsultantName = item.ConsultantName, ReviewItems = item.ReviewItems, ChequeDate = item.Cheques.Where(cheque => cheque.Status != PatientChequeStatus.Cancelled).OrderBy(cheque => cheque.DueDate).Select(cheque => (DateTime?)cheque.DueDate).FirstOrDefault(), ChequeRegistration = item.Cheques.Where(cheque => cheque.Status != PatientChequeStatus.Cancelled).OrderBy(cheque => cheque.DueDate).Select(cheque => cheque.SayadNumber).FirstOrDefault() });

    private async Task<List<PatientFinanceAdminReportItem>> WithAllChequesAsync(
        List<PatientFinanceAdminReportItem> items,
        CancellationToken cancellationToken)
    {
        if (items.Count == 0) return items;

        var caseIds = items.Select(item => item.CaseId).ToArray();
        var cheques = await repository.Cheques.AsNoTracking()
            .Where(cheque => caseIds.Contains(cheque.PatientFinancialCaseId) &&
                cheque.Status != PatientChequeStatus.Cancelled)
            .OrderBy(cheque => cheque.DueDate)
            .ThenBy(cheque => cheque.Id)
            .Select(cheque => new { cheque.PatientFinancialCaseId, cheque.DueDate, cheque.SayadNumber })
            .ToListAsync(cancellationToken);
        var byCase = cheques.GroupBy(cheque => cheque.PatientFinancialCaseId)
            .ToDictionary(group => group.Key, group => group.ToList());
        return items.Select(item => byCase.TryGetValue(item.CaseId, out var rows)
            ? item with
            {
                ChequeDates = rows.Select(row => row.DueDate).ToArray(),
                ChequeRegistrations = rows.Select(row => row.SayadNumber).ToArray()
            }
            : item).ToList();
    }

    private async Task<PatientFinanceAdminReportSummary> BuildSummaryAsync(
        IQueryable<DentalDashboard.Domain.Secretary.Accountant.PatientFinance.Entities.PatientFinancialCase> query,
        CancellationToken cancellationToken)
    {
        var values = await query.Select(item => new
        {
            item.TotalAmount,
            item.PrePaymentAmount,
            item.DepositAmount,
            PaidAmount = item.PrePaymentAmount + item.DepositAmount +
                (item.Transactions
                .Where(transaction => transaction.Type == PatientFinancialTransactionType.Payment)
                .Sum(transaction => (decimal?)transaction.Amount) ?? 0),
            UnpaidDebtAmount = item.Debts
                .Where(debt => debt.Status == PatientDebtStatus.Unpaid)
                .Sum(debt => (decimal?)debt.Amount) ?? 0,
            ChequeAmount = item.Cheques
                .Where(cheque => cheque.Status != PatientChequeStatus.Cancelled)
                .Sum(cheque => (decimal?)cheque.Amount) ?? 0,
            PromissoryNoteAmount = item.PromissoryNotes
                .Where(note => note.Status != PatientPromissoryNoteStatus.Cancelled)
                .Sum(note => (decimal?)note.Amount) ?? 0
        }).ToListAsync(cancellationToken);

        if (values.Count == 0)
            return new(0, 0, 0, 0, 0, 0, 0, 0, 0);

        var totalAmount = values.Sum(item => item.TotalAmount);
        var prePaymentAmount = values.Sum(item => item.PrePaymentAmount);
        var depositAmount = values.Sum(item => item.DepositAmount);
        var paidAmount = values.Sum(item => item.PaidAmount);

        return new(
            values.Count,
            totalAmount,
            prePaymentAmount,
            depositAmount,
            paidAmount,
            values.Sum(item => Math.Max(
                item.TotalAmount - item.PaidAmount,
                0)),
            values.Sum(item => item.UnpaidDebtAmount),
            values.Sum(item => item.ChequeAmount),
            values.Sum(item => item.PromissoryNoteAmount));
    }

}
