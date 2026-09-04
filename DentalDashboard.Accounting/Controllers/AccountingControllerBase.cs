using DentalDashboard.Framwork.Domain;
using DentalDashboard.Framwork.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DentalDashboard.Accounting.Controllers;

public abstract class AccountingControllerBase : DashboardApiControllerBase
{
    protected IActionResult WriteAccountingResult<T>(Result<T> result)
    {
        if (result.IsSuccess)
        {
            return Ok(result);
        }

        if (result.Message.Contains("یافت نشد"))
        {
            return NotFound(result);
        }

        var isConflict = result.Message.Contains("قابل ویرایش نیست") ||
                         result.Message.Contains("قابل لغو") ||
                         result.Message.Contains("روز سررسید") ||
                         result.Message.Contains("قبلاً تعیین شده") ||
                         result.Message.Contains("تسویه کامل بدهی امکان‌پذیر نیست");

        return isConflict
            ? StatusCode(StatusCodes.Status409Conflict, result)
            : BadRequest(result);
    }
}
