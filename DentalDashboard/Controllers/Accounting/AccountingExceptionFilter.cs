using DentalDashboard.ApplicationService.Contract.Accounting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;

namespace DentalDashboard.Controllers.Accounting;

public sealed class AccountingExceptionFilter(ILogger<AccountingExceptionFilter> logger) : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        var exception = context.Exception;
        var status = exception switch {
            AccountingForbiddenException => StatusCodes.Status403Forbidden,
            AccountingValidationException => StatusCodes.Status400BadRequest,
            DbUpdateConcurrencyException => StatusCodes.Status409Conflict,
            DbUpdateException { InnerException: SqlException { Number: 2601 or 2627 or 1205 } } => StatusCodes.Status409Conflict,
            SqlException { Number: 1205 } => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError
        };
        if (exception is OperationCanceledException && context.HttpContext.RequestAborted.IsCancellationRequested) return;
        if (status == 500) logger.LogError(exception, "Accounting request failed. Trace: {TraceId}", context.HttpContext.TraceIdentifier);
        var message = exception switch {
            AccountingForbiddenException => "اجازه انجام این عملیات حسابداری را ندارید.",
            AccountingValidationException => exception.Message,
            DbUpdateConcurrencyException => "اطلاعات هم‌زمان تغییر کرده است؛ صفحه را دوباره دریافت کنید.",
            _ when status == 409 => "این عملیات هم‌زمان پردازش شده است؛ اطلاعات را دوباره دریافت و بررسی کنید.",
            _ => "خطای سرور در حسابداری؛ کد پیگیری را به پشتیبانی اعلام کنید."
        };
        context.Result = new ObjectResult(new { message, traceId = context.HttpContext.TraceIdentifier }) { StatusCode = status };
        context.ExceptionHandled = true;
    }
}
