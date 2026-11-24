using System.Text;
using ClosedXML.Excel;
using DentalDashboard.ApplicationService.Contract.Accounting;
using DentalDashboard.Domain.Accounting;

namespace DentalDashboard.ApplicationService.Accounting.Services;

public sealed class AccountingExportService(AccountingQueryService queries, IAccountingAccess access)
{
    public static string CsvCell(string? value)
    {
        value ??= "";
        if (value.TrimStart().StartsWith('=') || value.TrimStart().StartsWith('+') ||
            value.TrimStart().StartsWith('-') || value.TrimStart().StartsWith('@')) value = "'" + value;
        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }
    public async Task<AccountingExportResponse> ExportAsync(AccountingExportQuery query, CancellationToken ct)
    {
        if (!await access.AllowsAsync(query.Actor.UserId, AccountingPermissions.Export, ct)) throw new AccountingForbiddenException();
        AccountingFilterRules.Validate(query.Filter);
        var filter = new AccountingFilter { PageSize = 100, Search = query.Filter.Search, Status = query.Filter.Status,
            FromDate = query.Filter.FromDate, ToDate = query.Filter.ToDate, UserId = query.Filter.UserId, CaseId = query.Filter.CaseId };
        var csv = new StringBuilder("شناسه,شخص,شرح,مبلغ تومان,وضعیت,تاریخ UTC,شماره سند,ضمانت\r\n");
        using var workbook = query.Excel ? new XLWorkbook() : null;
        var sheet = workbook?.Worksheets.Add("حسابداری");
        if (sheet is not null) {
            sheet.RightToLeft = true;
            var headers = new[] { "شناسه", "شخص", "شرح", "مبلغ تومان", "وضعیت", "تاریخ UTC", "شماره سند", "ضمانت", "پرداخت قطعی / مبلغ فروش" };
            for (var i = 0; i < headers.Length; i++) sheet.Cell(1, i + 1).Value = headers[i];
        }
        var rowNumber = 2;
        AccountingPage page;
        do {
            page = await queries.PageAsync(new(query.Actor, query.Section, filter), ct);
            if (page.TotalCount > 100_000) throw new AccountingValidationException("خروجی بیش از ۱۰۰ هزار ردیف است؛ بازه گزارش را کوچک‌تر کنید.");
            foreach (var row in page.Items) {
                if (sheet is not null) {
                    sheet.Cell(rowNumber, 1).Value = row.Id;
                    sheet.Cell(rowNumber, 2).Value = row.Person;
                    sheet.Cell(rowNumber, 3).Value = row.Description;
                    sheet.Cell(rowNumber, 4).Value = row.Amount;
                    sheet.Cell(rowNumber, 5).Value = row.Status;
                    sheet.Cell(rowNumber, 6).Value = row.Date?.ToString("O") ?? "";
                    sheet.Cell(rowNumber, 7).Value = row.Number ?? "";
                    sheet.Cell(rowNumber, 8).Value = row.IsGuarantee ? "بله" : "خیر";
                    if (row.PaidAmount.HasValue) sheet.Cell(rowNumber, 9).Value = row.PaidAmount.Value;
                    rowNumber++;
                }
                else csv.AppendLine(string.Join(',', new[] {
                CsvCell(row.Id), CsvCell(row.Person), CsvCell(row.Description),
                row.Amount.ToString(System.Globalization.CultureInfo.InvariantCulture), CsvCell(row.Status),
                CsvCell(row.Date?.ToString("O")), CsvCell(row.Number), row.IsGuarantee ? "بله" : "خیر"
                }));
            }
            filter.Page++;
        } while ((filter.Page - 1) * filter.PageSize < page.TotalCount);
        if (workbook is not null && sheet is not null) {
            sheet.Row(1).Style.Font.Bold = true;
            sheet.SheetView.FreezeRows(1);
            sheet.Columns().Width = 24;
            sheet.Column(3).Width = 42;
            sheet.Columns(4, 4).Style.NumberFormat.Format = "#,##0.##";
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return new(stream.ToArray(), $"accounting-{query.Section}-{DateTime.UtcNow:yyyy-MM-dd}.xlsx",
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        }
        return new(new UTF8Encoding(true).GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray(),
            $"accounting-{query.Section}-{DateTime.UtcNow:yyyy-MM-dd}.csv");
    }
}
