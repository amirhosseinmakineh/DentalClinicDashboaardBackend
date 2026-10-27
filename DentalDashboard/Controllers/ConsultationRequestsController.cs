using System.Text.RegularExpressions;
using DentalDashboard.Domain.Enums;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Domain.Models;
using DentalDashboard.Infrastracture.Context;
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
    [HttpPost]
    [EnableRateLimiting("consultation")]
    [RequestSizeLimit(8192)]
    public async Task<IActionResult> Create(ConsultationRequest request, CancellationToken ct)
    {
        var name = Regex.Replace(request.FullName?.Trim() ?? "", @"\s+", " ");
        var phone = NormalizePhone(request.PhoneNumber);
        if (name.Length is < 3 or > 100 || name.Count(char.IsLetter) < 3 ||
            !Regex.IsMatch(name, @"^[\p{L}\p{M} \u200c'’\-]+$"))
            return BadRequest(new { message = "نام و نام خانوادگی معتبر وارد کنید (۳ تا ۱۰۰ کاراکتر)." });
        if (!Regex.IsMatch(phone, @"^09[0-9]{9}$"))
            return BadRequest(new { message = "شماره موبایل معتبر ایران وارد کنید." });
        if (request.RequestId == Guid.Empty)
            return BadRequest(new { message = "شناسه درخواست معتبر نیست." });

        try
        {
            // A retry can succeed only for the exact payload that was committed.
            var previous = await db.LeadAssignments.AsNoTracking()
                .FirstOrDefaultAsync(x => x.PhoneNumber == request.PhoneNumber, ct);
            if (previous != null)
                return Replay(previous, name, phone);
            var lead = new LeadAssignment
            {
                UserName = name, PhoneNumber = phone, SourceType = LeadSourceType.Website,
                ConsultationRequestId = request.RequestId, CreatedAt = DateTime.Now,
                AssignmentType = LeadAssignmentType.RealTime, RequiresThreeMinuteCall = true,
                LeadAssignmentState = LeadAssignmentState.New
            };
            // Same serializable phone-deduplication transaction used by Yektanet.
            var inserted = await leads.AddNewLeadsIfPhoneDoesNotExistAsync([lead], ct);
            if (inserted > 0)
                return Ok(new { success = true });
            previous = await db.LeadAssignments.AsNoTracking()
                .FirstOrDefaultAsync(x => x.ConsultationRequestId == request.RequestId, ct);
            return previous != null ? Replay(previous, name, phone)
                : Conflict(new { message = "امکان ثبت درخواست جدید با این اطلاعات وجود ندارد. برای پیگیری با کلینیک تماس بگیرید." });
        }
        catch (Exception exception) when (exception is DbUpdateException or Microsoft.Data.SqlClient.SqlException)
        {
            // No submitted values or database exception details leave this endpoint.
            return StatusCode(503, new { message = "ثبت درخواست موقتاً امکان‌پذیر نیست. لطفاً دوباره تلاش کنید." });
        }
    }

    private IActionResult Replay(LeadAssignment previous, string name, string phone) =>
        previous.SourceType == LeadSourceType.Website && previous.UserName == name && previous.PhoneNumber == phone
            ? Ok(new { success = true })
            : Conflict(new { message = "درخواست مشاوره ای با این شماره از قبل در سیستم موجود میباشد" });

    internal static string NormalizePhone(string? input)
    {
        var digits = string.Concat((input ?? "").Trim().Select(c => c is >= '۰' and <= '۹' ? (char)('0' + c - '۰') : c is >= '٠' and <= '٩' ? (char)('0' + c - '٠') : c));
        digits = Regex.Replace(digits, @"[\s()\-]", "");
        if (digits.StartsWith("+98")) digits = "0" + digits[3..];
        else if (digits.StartsWith("0098")) digits = "0" + digits[4..];
        else if (digits.StartsWith("98") && digits.Length == 12) digits = "0" + digits[2..];
        return digits;
    }
}

public sealed record ConsultationRequest(string? FullName, string? PhoneNumber, Guid RequestId);
