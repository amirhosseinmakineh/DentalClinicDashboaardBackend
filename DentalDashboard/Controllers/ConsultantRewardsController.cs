using System.Security.Claims;
using DentalDashboard.Services;
using DentalDashboard.Infrastracture.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DentalDashboard.Controllers;

[ApiController]
[Route("api/consultant/wallet")]
[Authorize(Roles = "Consultant")]
public sealed class ConsultantWalletController(DentalContext context) : ControllerBase
{
    private readonly ConsultantRewardService service = new(context);
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        return Ok(await service.GetConsultantWalletAsync(userId, cancellationToken));
    }

    private bool TryGetUserId(out Guid id) => Guid.TryParse(
        User.FindFirstValue(ClaimTypes.NameIdentifier) ??
        User.FindFirstValue("userId") ?? User.FindFirstValue("Id"), out id);
}

[ApiController]
[Route("api/admin/consultant-rewards")]
[Authorize(Roles = "Admin")]
public sealed class AdminConsultantRewardsController(DentalContext context) : ControllerBase
{
    private readonly ConsultantRewardService service = new(context);
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string? status, CancellationToken cancellationToken) =>
        Ok(await service.GetAdminReportAsync(status, cancellationToken));

    [HttpPost("{reservationId:long}/approve")]
    public Task<IActionResult> Approve(long reservationId, CancellationToken cancellationToken) =>
        Review(reservationId, true, cancellationToken);

    [HttpPost("{reservationId:long}/reject")]
    public Task<IActionResult> Reject(long reservationId, CancellationToken cancellationToken) =>
        Review(reservationId, false, cancellationToken);

    private async Task<IActionResult> Review(long reservationId, bool approved, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await service.ReviewAsync(reservationId, userId, approved, cancellationToken);
        return result.Success ? Ok(new { isSuccess = true, message = result.Message })
            : Conflict(new { isSuccess = false, message = result.Message });
    }

    private bool TryGetUserId(out Guid id) => Guid.TryParse(
        User.FindFirstValue(ClaimTypes.NameIdentifier) ??
        User.FindFirstValue("userId") ?? User.FindFirstValue("Id"), out id);
}
