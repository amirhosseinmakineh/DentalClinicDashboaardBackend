using DentalDashboard.ApplicationService.Contract.Dtos.Secretary;
using DentalDashboard.ApplicationService.Contract.IServices;
using DentalDashboard.ApplicationService.Contract.Requests.User.Queries.User;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DentalDashboard.Controllers;

[ApiController]
[Route("api/admin/secretary")]
[Route("api/admin/secretaries")]
[Authorize(Roles = "Admin")]
public sealed class AdminSecretaryController : DashboardApiControllerBase
{
    private readonly ISecretaryAccessService accessService;
    private readonly IQueryDispatcher queryDispatcher;

    public AdminSecretaryController(ISecretaryAccessService accessService, IQueryDispatcher queryDispatcher)
    {
        this.accessService = accessService;
        this.queryDispatcher = queryDispatcher;
    }

    [HttpGet]
    public async Task<IActionResult> GetSecretaries(
        [FromQuery] GetUsersQuery query,
        CancellationToken cancellationToken)
    {
        query.RoleName = "Secretary";
        var result = await queryDispatcher.DispatchAsync(query, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{userId:guid}/schedule")]
    public async Task<IActionResult> GetSchedule(Guid userId, CancellationToken cancellationToken)
    {
        var access = await accessService.GetAccessAsync(userId, cancellationToken);
        var days = await accessService.GetScheduleAsync(userId, cancellationToken);
        var permissions = await accessService.GetPermissionScheduleAsync(userId, cancellationToken);
        return Ok(new
        {
            userId,
            secretaryType = access.Type,
            days = days.Select(x => x.ToString()),dayPermissions = days.Select(day => new
            {
                day = day.ToString(),
                permissions = permissions.GetValueOrDefault(day, []).Select(x => x.ToString())
            })
        });
    }

    [HttpPut("{userId:guid}/schedule")]
    public async Task<IActionResult> UpdateSchedule(Guid userId, UpdateSecretaryScheduleDto request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var adminUserId))
        {
            return Unauthorized();
        }
        var configurations = new Dictionary<DayOfWeek, IReadOnlyCollection<DentalDashboard.Domain.Enums.SecretaryPermissionType>>();
        var requested = request.DayPermissions.Count > 0
            ? request.DayPermissions
            : request.Days.Select(x => new SecretaryDayPermissionsDto { Day = x }).ToList();
        foreach (var item in requested)
        {
            if (!Enum.TryParse<DayOfWeek>(item.Day, true, out var parsedDay) || !Enum.IsDefined(parsedDay))
                return BadRequest(new { message = $"روز '{item.Day}' معتبر نیست" });
            var permissions = item.Permissions?
                .Where(permission => permission.HasValue)
                .Select(permission => permission.GetValueOrDefault())
                .ToArray() ?? [];
            if (!configurations.TryAdd(parsedDay, permissions))
                return BadRequest(new { message = $"روز '{item.Day}' تکراری است" });
        }
        var result = await accessService.UpdateScheduleAsync(userId, request.SecretaryType,
            configurations, adminUserId, cancellationToken);
        if (!result.Succeeded) return BadRequest(new { message = result.Error });
        return NoContent();
    }
}
