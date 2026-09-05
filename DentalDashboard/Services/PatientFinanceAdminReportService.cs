using ClosedXML.Excel;
using DentalDashboard.Domain.Secretary.Accountant.PatientFinance.Enums;
using DentalDashboard.Domain.Secretary.Accountant.PatientFinance.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.Services;

public sealed class PatientFinanceAdminReportFilter
{
    public string? Search { get; set; }
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
    DateTime CreatedAt);

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
        var items = await Project(query)
            .OrderByDescending(item => item.CreatedAt)
            .ThenByDescending(item => item.CaseId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new(items, summary.CaseCount, page, pageSize, summary);
    }

    public async Task<byte[]> ExportExcelAsync(
        PatientFinanceAdminReportFilter filter,
        CancellationToken cancellationToken = default)
    {
        var query = BuildQuery(filter);
        var summary = await BuildSummaryAsync(query, cancellationToken);
        var items = await Project(query)
            .OrderByDescending(item => item.CreatedAt)
            .ThenByDescending(item => item.CaseId)
            .ToListAsync(cancellationToken);

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("گزارش حسابداری بیماران");
        sheet.RightToLeft = true;
        var headers = new[]
        {
            "ردیف", "نام بیمار", "شماره تماس", "شماره پرونده", "خدمت",
            "مبلغ کل", "پیش‌پرداخت", "ودیعه", "پرداخت قطعی", "مانده",
            "بدهی باز", "مبلغ چک‌ها", "مبلغ سفته‌ها", "نوع توافق",
            "وضعیت پرونده", "ثبت‌کننده", "تاریخ ثبت", "شناسه پرونده مالی"
        };

        for (var column = 0; column < headers.Length; column++)
            sheet.Cell(1, column + 1).Value = headers[column];

        for (var index = 0; index < items.Count; index++)
        {
            var item = items[index];
            var row = index + 2;
            sheet.Cell(row, 1).Value = index + 1;
            sheet.Cell(row, 2).Value = item.PatientName;
            sheet.Cell(row, 3).Value = item.PhoneNumber;
            sheet.Cell(row, 4).Value = item.FileNumber;
            sheet.Cell(row, 5).Value = item.ServiceName;
            sheet.Cell(row, 6).Value = item.TotalAmount;
            sheet.Cell(row, 7).Value = item.PrePaymentAmount;
            sheet.Cell(row, 8).Value = item.DepositAmount;
            sheet.Cell(row, 9).Value = item.PaidAmount;
            sheet.Cell(row, 10).Value = item.RemainingAmount;
            sheet.Cell(row, 11).Value = item.UnpaidDebtAmount;
            sheet.Cell(row, 12).Value = item.ChequeAmount;
            sheet.Cell(row, 13).Value = item.PromissoryNoteAmount;
            sheet.Cell(row, 14).Value = AgreementLabel(item.AgreementType);
            sheet.Cell(row, 15).Value = StatusLabel(item.Status);
            sheet.Cell(row, 16).Value = item.CreatedBy;
            sheet.Cell(row, 17).Value = item.CreatedAt;
            sheet.Cell(row, 18).Value = item.CaseId.ToString();
        }

        var summaryRow = items.Count + 3;
        sheet.Cell(summaryRow, 1).Value = "جمع گزارش";
        sheet.Cell(summaryRow, 6).Value = summary.TotalAmount;
        sheet.Cell(summaryRow, 7).Value = summary.PrePaymentAmount;
        sheet.Cell(summaryRow, 8).Value = summary.DepositAmount;
        sheet.Cell(summaryRow, 9).Value = summary.PaidAmount;
        sheet.Cell(summaryRow, 10).Value = summary.RemainingAmount;
        sheet.Cell(summaryRow, 11).Value = summary.UnpaidDebtAmount;
        sheet.Cell(summaryRow, 12).Value = summary.ChequeAmount;
        sheet.Cell(summaryRow, 13).Value = summary.PromissoryNoteAmount;

        var headerRange = sheet.Range(1, 1, 1, headers.Length);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#0F766E");
        headerRange.Style.Font.FontColor = XLColor.White;
        sheet.Range(summaryRow, 1, summaryRow, headers.Length).Style.Font.Bold = true;
        sheet.Range(2, 6, summaryRow, 13).Style.NumberFormat.Format = "#,##0.###";
        sheet.Column(17).Style.DateFormat.Format = "yyyy/MM/dd HH:mm";
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

        if (filter.ServiceId.HasValue)
            query = query.Where(item => (int)item.Service == filter.ServiceId.Value);
        if (filter.AgreementType.HasValue)
            query = query.Where(item => item.AgreementType == filter.AgreementType.Value);
        if (filter.Status.HasValue)
            query = query.Where(item => item.Status == filter.Status.Value);
        if (filter.FromDate.HasValue)
            query = query.Where(item => item.CreatedAt >= filter.FromDate.Value);
        if (filter.ToDate.HasValue)
            query = query.Where(item => item.CreatedAt < filter.ToDate.Value.Date.AddDays(1));

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
            item.Transactions.Where(transaction => transaction.Type == PatientFinancialTransactionType.Payment)
                .Sum(transaction => (decimal?)transaction.Amount) ?? 0,
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
            item.CreatedAt));

    private async Task<PatientFinanceAdminReportSummary> BuildSummaryAsync(
        IQueryable<DentalDashboard.Domain.Secretary.Accountant.PatientFinance.Entities.PatientFinancialCase> query,
        CancellationToken cancellationToken)
    {
        var values = await query.GroupBy(_ => 1).Select(group => new
        {
            CaseCount = group.Count(),
            TotalAmount = group.Sum(item => item.TotalAmount),
            PrePaymentAmount = group.Sum(item => item.PrePaymentAmount),
            DepositAmount = group.Sum(item => item.DepositAmount),
            PaidAmount = group.Sum(item => item.Transactions
                .Where(transaction => transaction.Type == PatientFinancialTransactionType.Payment)
                .Sum(transaction => (decimal?)transaction.Amount) ?? 0),
            UnpaidDebtAmount = group.Sum(item => item.Debts
                .Where(debt => debt.Status == PatientDebtStatus.Unpaid)
                .Sum(debt => (decimal?)debt.Amount) ?? 0),
            ChequeAmount = group.Sum(item => item.Cheques
                .Where(cheque => cheque.Status != PatientChequeStatus.Cancelled)
                .Sum(cheque => (decimal?)cheque.Amount) ?? 0),
            PromissoryNoteAmount = group.Sum(item => item.PromissoryNotes
                .Where(note => note.Status != PatientPromissoryNoteStatus.Cancelled)
                .Sum(note => (decimal?)note.Amount) ?? 0)
        }).SingleOrDefaultAsync(cancellationToken);

        if (values is null)
            return new(0, 0, 0, 0, 0, 0, 0, 0, 0);

        return new(
            values.CaseCount,
            values.TotalAmount,
            values.PrePaymentAmount,
            values.DepositAmount,
            values.PaidAmount,
            Math.Max(values.TotalAmount - values.PrePaymentAmount - values.DepositAmount - values.PaidAmount, 0),
            values.UnpaidDebtAmount,
            values.ChequeAmount,
            values.PromissoryNoteAmount);
    }

    private static string AgreementLabel(PatientFinancialAgreementType value) => value switch
    {
        PatientFinancialAgreementType.PrePayment => "پیش‌پرداخت",
        PatientFinancialAgreementType.Deposit => "ودیعه",
        _ => value.ToString()
    };

    private static string StatusLabel(PatientFinancialCaseStatus value) => value switch
    {
        PatientFinancialCaseStatus.Active => "فعال",
        PatientFinancialCaseStatus.Completed => "تسویه‌شده",
        PatientFinancialCaseStatus.Cancelled => "لغوشده",
        _ => value.ToString()
    };
}
