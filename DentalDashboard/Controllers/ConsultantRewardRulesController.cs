using System.Data;
using System.Security.Claims;
using DentalDashboard.Domain.Enums;
using DentalDashboard.Domain.Models;
using DentalDashboard.Infrastracture.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.Controllers;

public sealed record SaveServiceRewardRule(int ServiceId, string Basis, int UnitsPerReward, string RemainderMode,
    decimal Amount, bool IsActive, long? ExpectedVersionId);

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/admin/consultant-reward-rules")]
public sealed class ConsultantRewardRulesController(DentalContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var all = await db.ConsultantServiceRewardRules.AsNoTracking().OrderByDescending(x => x.Id).ToListAsync(ct);
        return Ok(Enum.GetValues<DentalServiceType>().Select(service => new {
            ServiceId = (int)service, ServiceName = service.ToPersian(),
            Rule = all.FirstOrDefault(x => x.ServiceId == (int)service)
        }));
    }

    [HttpPost]
    public async Task<IActionResult> Save(SaveServiceRewardRule request, CancellationToken ct)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("userId") ??
            User.FindFirstValue("Id"), out var actor)) return Unauthorized();
        var rule = new ConsultantServiceRewardRule { ServiceId = request.ServiceId, Basis = request.Basis,
            UnitsPerReward = request.UnitsPerReward, RemainderMode = request.RemainderMode, Amount = request.Amount,
            IsActive = request.IsActive, CreatedAtUtc = DateTime.UtcNow, CreatedByUserId = actor };
        if (ConsultantServiceRewardCalculator.ValidateRule(rule) is { } error) return BadRequest(new { message = error });
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var previous = await db.ConsultantServiceRewardRules.Where(x => x.ServiceId == request.ServiceId)
            .OrderByDescending(x => x.Id).FirstOrDefaultAsync(ct);
        if (previous?.Id != request.ExpectedVersionId)
            return Conflict(new { message = "تنظیمات هم‌زمان تغییر کرده‌اند؛ صفحه را تازه کنید." });
        db.ConsultantServiceRewardRules.Add(rule);
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
        return Ok(new { rule.Id, rule.CreatedAtUtc, message = "نسخه جدید قانون پاداش ذخیره شد؛ پاداش‌های قبلی تغییر نمی‌کنند." });
    }
}
