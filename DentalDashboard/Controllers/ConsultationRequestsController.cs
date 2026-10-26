using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DentalDashboard.Domain.Enums;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Domain.Models;
using DentalDashboard.Infrastracture.Context;
using DentalDashboard.LeadManagement.Application.Consultation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/consultation-requests")]
public sealed class ConsultationRequestsController(DentalContext db, ILeadAssignmentRepository leads) : ControllerBase
{
    [HttpGet("services")]
    public async Task<IActionResult> Services(CancellationToken cancellationToken) => Ok(await db.SecretarySaleServices
        .AsNoTracking().Where(x => x.IsActive && !x.IsDeleted).OrderBy(x => x.Title)
        .Select(x => new { id = x.Id, title = x.Title }).ToListAsync(cancellationToken));

    [HttpPost]
    [EnableRateLimiting("consultation-requests")]
    [RequestSizeLimit(16_384)]
    public async Task<IActionResult> Create([FromBody] ConsultationRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(idempotencyKey, out var requestId) || requestId == Guid.Empty)
            return BadRequest(new { message = "شناسه درخواست معتبر نیست. صفحه را بازخوانی کنید." });
        var name = ConsultationInput.NormalizeName(request.FullName);
        var phone = ConsultationInput.NormalizePhone(request.PhoneNumber);
        var description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        var services = request.ServiceIds.Distinct().Order().ToArray();
        if (!ConsultationInput.IsValidName(name))
            return BadRequest(new { message = "نام و نام خانوادگی باید حداقل ۳ حرف معتبر داشته باشد." });
        if (!ConsultationInput.IsValidPhone(phone))
            return BadRequest(new { message = "شماره موبایل ایران معتبر نیست." });
        if (services.Length == 0 || services.Length > 10 || description?.Length > 1000 ||
            description?.Any(c => char.IsControl(c) && c != '\n' && c != '\r' && c != '\t') == true)
            return BadRequest(new { message = "خدمات یا توضیحات واردشده معتبر نیست." });

        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
            JsonSerializer.Serialize(new { name, phone, services, description }))));
        var previous = await db.LeadAssignments.AsNoTracking()
            .SingleOrDefaultAsync(x => x.ConsultationRequestId == requestId, cancellationToken);
        if (previous is not null)
            return previous.ConsultationRequestHash == hash ? Saved() : Conflict(new { message = "شناسه درخواست با اطلاعات دیگری استفاده شده است." });

        var validServices = await db.SecretarySaleServices.CountAsync(x => services.Contains(x.Id) && x.IsActive && !x.IsDeleted, cancellationToken);
        if (validServices != services.Length)
            return BadRequest(new { message = "لطفاً خدمات معتبر و فعال کلینیک را انتخاب کنید." });

        // Use the existing serializable insertion and historical-phone rules.
        // Save the graph atomically; allocation is exclusively performed by existing workers/pickup.
        var lead = new LeadAssignment
        {
            UserName = name, PhoneNumber = phone, SourceType = LeadSourceType.Website,
            CreatedAt = DateTime.UtcNow, AssignmentType = LeadAssignmentType.RealTime,
            LeadAssignmentState = LeadAssignmentState.New, RequiresThreeMinuteCall = true,
            ConsultationRequestId = requestId, ConsultationRequestHash = hash,
            ConsultationDescription = description,
            RequestedServices = services.Select(id => new LeadRequestedService { ServiceId = id }).ToList()
        };
        var inserted = await leads.AddNewLeadsIfPhoneDoesNotExistAsync([lead], cancellationToken);
        if (inserted == 1) return Saved();

        // Another identical retry may have committed while this request waited on the phone lock.
        previous = await db.LeadAssignments.AsNoTracking()
            .SingleOrDefaultAsync(x => x.ConsultationRequestId == requestId, cancellationToken);
        if (previous?.ConsultationRequestHash == hash) return Saved();
        // Never return another person's lead id, name, number or assignment history.
        return Conflict(new { message = "امکان ثبت درخواست جدید با این اطلاعات وجود ندارد. برای پیگیری با کلینیک تماس بگیرید." });
    }

    private OkObjectResult Saved() => Ok(new
    {
        saved = true,
        message = "درخواست مشاوره شما با موفقیت ثبت شد. همکاران ما برای هماهنگی با شما تماس خواهند گرفت."
    });
}
