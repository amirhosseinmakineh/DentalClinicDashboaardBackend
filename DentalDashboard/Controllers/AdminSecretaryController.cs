using System.Security.Claims;
using DentalDashboard.ApplicationService.Contract.Dtos.Secretary;
using DentalDashboard.ApplicationService.Contract.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DentalDashboard.Controllers;

[ApiController]
[Route("api/admin/secretary")]
[Authorize(Roles = "Admin")]
public sealed class AdminSecretaryController : ControllerBase
{
    private readonly ISecretaryAccessService accessService;
    public AdminSecretaryController(ISecretaryAccessService accessService) => this.accessService = accessService;

    [HttpGet("{userId:guid}/schedule")]
    public async Task<IActionResult> GetSchedule(Guid userId, CancellationToken cancellationToken)
    {
        var access = await accessService.GetAccessAsync(userId, cancellationToken);
        if (!access.IsSecretary) return BadRequest(new { message = "کاربر باید نقش منشی داشته باشد" });
        var days = await accessService.GetScheduleAsync(userId, cancellationToken);
        return Ok(new { userId, secretaryType = access.Type, days = days.Select(x => x.ToString()) });
    }

    [HttpPut("{userId:guid}/schedule")]
    public async Task<IActionResult> UpdateSchedule(Guid userId, UpdateSecretaryScheduleDto request,
        CancellationToken cancellationToken)
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("userId") ?? User.FindFirstValue("Id");
        if (!Guid.TryParse(claim, out var adminUserId)) return Unauthorized();
        var parsedDays = new List<DayOfWeek>();
        foreach (var day in request.Days)
        {
            if (!Enum.TryParse<DayOfWeek>(day, true, out var parsedDay) || !Enum.IsDefined(parsedDay))
                return BadRequest(new { message = $"روز '{day}' معتبر نیست" });
            parsedDays.Add(parsedDay);
        }
        var result = await accessService.UpdateScheduleAsync(userId, request.SecretaryType,
            parsedDays, adminUserId, cancellationToken);
        if (!result.Succeeded) return BadRequest(new { message = result.Error });
        return NoContent();
    }
}
