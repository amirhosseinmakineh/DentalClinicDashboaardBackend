using System.Globalization;
using System.Net;
using System.Text;
using DentalDashboard.ApplicationService.Contract.IServices;
using DentalDashboard.ApplicationService.Contract.Secretary.Account.DTOs;
using DentalDashboard.ApplicationService.Contract.Secretary.Account.Queries;

namespace DentalDashboard.ApplicationService.Services;

public sealed class FinancialTransactionReceiptService : IFinancialTransactionReceiptService
{
    private const string ContentType = "text/html; charset=utf-8";

    public FinancialTransactionReceiptResponse Create(SecretaryFinancialTransactionDto transaction)
    {
        var receiptNumber = transaction.Id.ToString(CultureInfo.InvariantCulture);
        var amount = transaction.Amount.ToString("N0", CultureInfo.GetCultureInfo("fa-IR"));
        var transactionDate = FormatPersianDate(transaction.TransactionDate);
        var subject = EncodeOrDash(transaction.Subject);
        var counterpartyName = EncodeOrDash(transaction.CounterpartyName);
        var description = EncodeOrDash(transaction.Description);
        var expenseCategory = EncodeOrDash(transaction.ExpenseCategoryTitle);

        var html = $$"""
            <!doctype html>
            <html lang="fa" dir="rtl">
            <head>
                <meta charset="utf-8">
                <meta name="viewport" content="width=device-width, initial-scale=1">
                <title>رسید تراکنش شماره {{receiptNumber}}</title>
                <style>
                    * { box-sizing: border-box; }
                    body { margin: 0; padding: 24px; background: #f3f4f6; color: #111827; font-family: Tahoma, Arial, sans-serif; }
                    .receipt { max-width: 720px; margin: 0 auto; padding: 32px; background: #fff; border: 1px solid #d1d5db; border-radius: 16px; }
                    h1 { margin: 0 0 8px; font-size: 24px; text-align: center; }
                    .number { margin-bottom: 28px; color: #4b5563; text-align: center; }
                    dl { display: grid; grid-template-columns: 160px 1fr; gap: 0; margin: 0; border: 1px solid #e5e7eb; border-radius: 10px; overflow: hidden; }
                    dt, dd { margin: 0; padding: 12px 16px; border-bottom: 1px solid #e5e7eb; }
                    dt { background: #f9fafb; font-weight: bold; }
                    dt:last-of-type, dd:last-of-type { border-bottom: 0; }
                    .amount { font-size: 18px; font-weight: bold; }
                    .footer { margin-top: 24px; color: #6b7280; font-size: 12px; text-align: center; }
                    @media (max-width: 520px) { body { padding: 8px; } .receipt { padding: 20px; } dl { grid-template-columns: 1fr; } dt { border-bottom: 0; padding-bottom: 4px; } dd { padding-top: 4px; } }
                    @media print { body { padding: 0; background: #fff; } .receipt { max-width: none; border: 0; box-shadow: none; } }
                </style>
            </head>
            <body>
                <main class="receipt">
                    <h1>رسید تراکنش مالی</h1>
                    <div class="number">شماره رسید: {{receiptNumber}}</div>
                    <dl>
                        <dt>نوع تراکنش</dt><dd>{{WebUtility.HtmlEncode(transaction.TypeTitle)}}</dd>
                        <dt>مبلغ</dt><dd class="amount">{{amount}} تومان</dd>
                        <dt>تاریخ تراکنش</dt><dd>{{transactionDate}}</dd>
                        <dt>موضوع</dt><dd>{{subject}}</dd>
                        <dt>طرف حساب</dt><dd>{{counterpartyName}}</dd>
                        <dt>روش پرداخت</dt><dd>{{WebUtility.HtmlEncode(transaction.PaymentMethodTitle)}}</dd>
                        <dt>دسته‌بندی هزینه</dt><dd>{{expenseCategory}}</dd>
                        <dt>توضیحات</dt><dd>{{description}}</dd>
                    </dl>
                    <div class="footer">این رسید از روی تراکنش ثبت‌شده در سامانه صادر شده است.</div>
                </main>
            </body>
            </html>
            """;

        var content = Encoding.UTF8.GetBytes(html);
        var fileName = $"financial-transaction-receipt-{receiptNumber}.html";

        return new FinancialTransactionReceiptResponse(content, ContentType, fileName);
    }

    private static string FormatPersianDate(DateTime value)
    {
        var calendar = new PersianCalendar();
        var year = calendar.GetYear(value);
        var month = calendar.GetMonth(value);
        var day = calendar.GetDayOfMonth(value);
        var time = value.ToString("HH:mm", CultureInfo.InvariantCulture);

        return $"{year:0000}/{month:00}/{day:00} - {time}";
    }

    private static string EncodeOrDash(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "-";
        }

        return WebUtility.HtmlEncode(value);
    }
}
