using DentalDashboard.ApplicationService.Accounting.Services;
using DentalDashboard.ApplicationService.Contract.Accounting;
using System.Data;
using System.Security.Claims;
using DentalDashboard.Domain.Secretary.Accountant.PatientFinance.Entities;
using DentalDashboard.Domain.Secretary.Accountant.PatientFinance.Enums;
using DentalDashboard.Infrastracture.Context;
using DentalDashboard.Utilities.Time;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.Controllers;

public static class PatientGuaranteeCollectionRules
{
    public static bool CanCollect(decimal guarantee, decimal overdueDebt, decimal remaining) =>
        DentalDashboard.Domain.Accounting.GuaranteeCollectionRules.CanCollect(guarantee, overdueDebt, remaining);
}

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/admin/reports/patient-finances/cheques")]
public sealed class PatientGuaranteeCollectionController(AccountingSettlementService settlements) : ControllerBase
{
    [HttpPost("{id:long}/collect-guarantee")]
    public async Task<IActionResult> Collect(long id, CancellationToken ct)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ??
            User.FindFirstValue("userId") ?? User.FindFirstValue("Id"), out var actor)) return Unauthorized();
        try { var result = await settlements.CollectGuaranteeAsync(id, actor, ct); return Ok(new { amount = ((AccountingCollectedAmount)result.Data!).Amount, message = result.Message }); }
        catch (AccountingValidationException error) { return Conflict(new { message = error.Message }); }
    }
}
