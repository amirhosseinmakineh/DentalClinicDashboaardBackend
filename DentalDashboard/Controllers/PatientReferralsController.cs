using System.Data;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using DentalDashboard.ApplicationService.Handlers.Helpers;
using DentalDashboard.Domain.Enums;
using DentalDashboard.Domain.Models;
using DentalDashboard.Domain.PatientReferrals;
using DentalDashboard.Infrastracture.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DentalDashboard.Utilities.Time;

namespace DentalDashboard.Controllers;

public record CreateReferralRequest(string FirstName, string LastName, string PhoneNumber, string? Description);
public record ReserveReferralRequest(long ConsultantProfileId, DateTime ReservationAt, string PatientCity, string PatientRegion, List<DentalServiceType> DentalServices, string? Description);
public record RejectReferralRequest(string Reason);
public sealed class ReferralFilters
{
    public string? Search { get; set; }
    public PatientReferralStatus? Status { get; set; }
    public Guid? ReferrerPatientUserId { get; set; }
    public Guid? SecretaryUserId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

[ApiController]
public sealed class PatientReferralsController(DentalContext db) : ControllerBase
{
    private Guid UserId() => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("userId") ?? User.FindFirstValue("Id") ?? User.FindFirstValue("sub"), out var id) ? id : Guid.Empty;

    private async Task<bool> Valid(string role, CancellationToken ct)
    {
        var id = UserId();
        return id != Guid.Empty && await db.Users.AnyAsync(x => x.Id == id && x.IsActive && !x.IsDeleted && x.UserRoles.Any(r => !r.IsDeleted && !r.Role.IsDeleted && r.Role.RoleName == role), ct);
    }

    [HttpPost("api/patient/referrals"), Authorize(Roles = "Patient")]
    public async Task<IActionResult> Create(CreateReferralRequest request, CancellationToken ct)
    {
        if (!await Valid("Patient", ct)) return Forbid();
        var first = request.FirstName?.Trim() ?? "";
        var last = request.LastName?.Trim() ?? "";
        var phone = NormalizePhone(request.PhoneNumber);
        var description = request.Description?.Trim();
        if (first.Length is < 2 or > 100 || last.Length is < 2 or > 100 || !Regex.IsMatch(phone, "^09\\d{9}$") || description?.Length > 500) return BadRequest("اطلاعات معرفی معتبر نیست.");
        var userId = UserId();
        var ownPhone = await db.Users.Where(x => x.Id == userId).Select(x => x.PhoneNumber).SingleAsync(ct);
        if (NormalizePhone(ownPhone) == phone) return BadRequest("امکان معرفی شماره خودتان وجود ندارد.");
        if (await db.PatientReferrals.AnyAsync(x => x.ReferredPhoneNumber == phone && x.Status != PatientReferralStatus.Rejected, ct)) return Conflict("این شماره قبلاً در سامانه معرفی شده است.");
        var entity = new PatientReferral { ReferrerPatientUserId = userId, ReferredFirstName = first, ReferredLastName = last, ReferredPhoneNumber = phone, Description = description, RewardAmount = 1_000_000m };
        db.Add(entity);
        await db.SaveChangesAsync(ct);
        return Created($"/api/patient/referrals/{entity.Id}", await Item(entity.Id, ct));
    }

    [HttpGet("api/patient/referrals"), Authorize(Roles = "Patient")]
    public async Task<IActionResult> PatientList([FromQuery] ReferralFilters filters, CancellationToken ct) => !await Valid("Patient", ct) ? Forbid() : await List(filters, Filter(filters).Where(x => x.ReferrerPatientUserId == UserId()), ct);
    [HttpGet("api/secretary/patient-referrals"), Authorize(Roles = "Secretary")]
    public async Task<IActionResult> SecretaryList([FromQuery] ReferralFilters filters, CancellationToken ct) => !await Valid("Secretary", ct) ? Forbid() : await List(filters, Filter(filters), ct);
    [HttpGet("api/admin/patient-referrals"), Authorize(Roles = "Admin")]
    public async Task<IActionResult> AdminList([FromQuery] ReferralFilters filters, CancellationToken ct) => !await Valid("Admin", ct) ? Forbid() : await List(filters, Filter(filters), ct);

    [HttpGet("api/admin/patient-referrals/export"), Authorize(Roles = "Admin")]
    public async Task<IActionResult> Export([FromQuery] ReferralFilters filters, CancellationToken ct)
    {
        if (!await Valid("Admin", ct)) return Forbid();
        var rows = await Project(Filter(filters).OrderByDescending(x => x.CreatedAt)).ToListAsync(ct);
        var csv = new StringBuilder("ردیف,معرف,بیمار معرفی‌شده,موبایل,وضعیت,مبلغ پاداش,منشی,تاریخ معرفی,تاریخ رزرو,تاریخ بررسی\r\n");
        for (var i = 0; i < rows.Count; i++)
        {
            var x = rows[i];
            csv.AppendLine(string.Join(',', i + 1, Csv(x.ReferrerFullName), Csv(x.FirstName + " " + x.LastName), Csv(x.PhoneNumber), Csv(StatusTitle(x.Status)), x.RewardAmount, Csv(x.SecretaryFullName), Csv(x.CreatedAt.ToString("yyyy-MM-dd HH:mm")), Csv(x.ReservationAt?.ToString("yyyy-MM-dd HH:mm")), Csv(x.ReviewedAt?.ToString("yyyy-MM-dd HH:mm"))));
        }
        return File(new UTF8Encoding(true).GetBytes(csv.ToString()), "text/csv; charset=utf-8", $"patient-referrals-{DateTime.UtcNow:yyyyMMdd-HHmm}.csv");
    }

    [HttpPost("api/secretary/patient-referrals/{id:long}/contacted"), Authorize(Roles = "Secretary")]
    public async Task<IActionResult> Contact(long id, CancellationToken ct)
    {
        if (!await Valid("Secretary", ct)) return Forbid();
        var entity = await db.PatientReferrals.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null) return NotFound();
        if (entity.Status != PatientReferralStatus.Submitted) return Conflict("فقط معرفی ثبت‌شده قابل پیگیری است.");
        entity.Status = PatientReferralStatus.Contacted;
        entity.SecretaryUserId = UserId();
        entity.ContactedAt = entity.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Ok(await Item(id, ct));
    }

    [HttpGet("api/secretary/patient-referrals/consultants"), Authorize(Roles = "Secretary")]
    public async Task<IActionResult> Consultants(CancellationToken ct)
    {
        if (!await Valid("Secretary", ct)) return Forbid();
        return Ok(await db.ConsultantProfiles.AsNoTracking().Where(x => !x.IsDeleted && x.IsCompleteProfile && !x.User.IsDeleted && x.User.IsActive).OrderBy(x => x.User.FirstName).ThenBy(x => x.User.LastName).Select(x => new { Id = x.Id, FullName = (x.User.FirstName + " " + x.User.LastName).Trim(), x.User.PhoneNumber }).ToListAsync(ct));
    }

    [HttpPost("api/secretary/patient-referrals/{id:long}/reserve"), Authorize(Roles = "Secretary")]
    public async Task<IActionResult> Reserve(long id, ReserveReferralRequest request, CancellationToken ct)
    {
        if (!await Valid("Secretary", ct)) return Forbid();
        var services = request.DentalServices?.Distinct().ToList() ?? [];
        if (!ReservationAppointmentTime.TryResolve(request.ReservationAt, null, out var appointmentAt, out var appointmentError)) return BadRequest(appointmentError);
        if (appointmentAt <= IranTimeHelper.IranLocalNow || string.IsNullOrWhiteSpace(request.PatientCity) || string.IsNullOrWhiteSpace(request.PatientRegion) || services.Count == 0 || services.Any(x => !Enum.IsDefined(x))) return BadRequest("اطلاعات رزرو معتبر نیست.");
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var entity = await db.PatientReferrals.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null) return NotFound();
        if (entity.Status != PatientReferralStatus.Contacted || entity.ReservationId != null || entity.LeadAssignmentId != null) return Conflict("این معرفی قابل رزرو نیست.");
        var consultant = await db.ConsultantProfiles.Include(x => x.User).SingleOrDefaultAsync(x => x.Id == request.ConsultantProfileId && !x.IsDeleted && x.IsCompleteProfile && x.User.IsActive && !x.User.IsDeleted, ct);
        if (consultant is null) return BadRequest("مشاور معتبر نیست.");
        if (await db.Reservations.CountAsync(x => !x.IsDeleted && !x.IsCanceled && x.ConsultantProfileId == consultant.Id && x.ReservationAt == appointmentAt, ct) >= 10) return Conflict("ظرفیت این بازه زمانی برای مشاور تکمیل است.");
        var now = DateTime.UtcNow;
        var lead = new LeadAssignment { UserName = $"{entity.ReferredFirstName} {entity.ReferredLastName}", PhoneNumber = entity.ReferredPhoneNumber, ConsultantProfileId = consultant.Id, PatientCity = request.PatientCity.Trim(), PatientRegion = request.PatientRegion.Trim(), AssignmentType = LeadAssignmentType.ConsultantPatient, LeadAssignmentState = LeadAssignmentState.Converted, CallResult = LeadCallResult.Converted, ReportSubmittedAt = now, ContactedAt = entity.ContactedAt, AssignedAt = now };
        db.Add(lead);
        await db.SaveChangesAsync(ct);
        var reservation = new Reservation { LeadAssignmentId = lead.Id, ConsultantProfileId = consultant.Id, ReservationAt = appointmentAt, DentalServices = services, Description = request.Description?.Trim(), OwnerType = ReservationOwnerType.Secretary, OwnerUserId = UserId(), SecretaryUserId = UserId(), InitialReservationAt = appointmentAt, LastActivityAt = now };
        db.Add(reservation);
        await db.SaveChangesAsync(ct);
        entity.LeadAssignmentId = lead.Id;
        entity.ReservationId = reservation.Id;
        entity.SecretaryUserId = UserId();
        entity.Status = PatientReferralStatus.ReservedPendingAdminApproval;
        entity.UpdatedAt = now;
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return Ok(await Item(id, ct));
    }

    [HttpPost("api/admin/patient-referrals/{id:long}/approve"), Authorize(Roles = "Admin")]
    public async Task<IActionResult> Approve(long id, CancellationToken ct)
    {
        if (!await Valid("Admin", ct)) return Forbid();
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var entity = await db.PatientReferrals.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null) return NotFound();
        if (entity.Status != PatientReferralStatus.ReservedPendingAdminApproval || entity.ReservationId is null) return Conflict("این معرفی قابل تأیید نیست.");
        var attended = await db.Reservations.AnyAsync(x => x.Id == entity.ReservationId && !x.IsDeleted && !x.IsCanceled && x.PatientReceivedService == true && x.SecretaryApprovedConsultantConfirmation == true, ct);
        if (!attended) return Conflict("تا پیش از تأیید حضور بیمار توسط منشی، پرداخت پاداش مجاز نیست.");
        if (await db.PatientWalletTransactions.AnyAsync(x => x.PatientReferralId == id && x.TransactionType == PatientWalletTransactionType.ReferralReward, ct)) return Conflict("پاداش این معرفی قبلاً پرداخت شده است.");
        var wallet = await db.PatientWallets.SingleOrDefaultAsync(x => x.PatientUserId == entity.ReferrerPatientUserId, ct);
        if (wallet is null) { wallet = new PatientWallet { PatientUserId = entity.ReferrerPatientUserId }; db.Add(wallet); await db.SaveChangesAsync(ct); }
        var now = DateTime.UtcNow;
        wallet.Balance += entity.RewardAmount;
        wallet.UpdatedAt = now;
        db.Add(new PatientWalletTransaction { WalletId = wallet.Id, PatientUserId = entity.ReferrerPatientUserId, PatientReferralId = entity.Id, Amount = entity.RewardAmount, TransactionType = PatientWalletTransactionType.ReferralReward, Description = $"پاداش معرفی بیمار {entity.ReferredFirstName} {entity.ReferredLastName}" });
        entity.Status = PatientReferralStatus.ApprovedRewarded;
        entity.ReviewedByAdminId = UserId();
        entity.ReviewedAt = entity.UpdatedAt = now;
        try { await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); }
        catch (DbUpdateException) { await tx.RollbackAsync(ct); return Conflict("پاداش این معرفی قبلاً پرداخت شده است."); }
        return Ok(await Item(id, ct));
    }

    [HttpPost("api/admin/patient-referrals/{id:long}/reject"), Authorize(Roles = "Admin")]
    public async Task<IActionResult> Reject(long id, RejectReferralRequest request, CancellationToken ct)
    {
        if (!await Valid("Admin", ct)) return Forbid();
        var reason = request.Reason?.Trim() ?? "";
        if (reason.Length is < 1 or > 500) return BadRequest("دلیل رد الزامی و حداکثر ۵۰۰ نویسه است.");
        var entity = await db.PatientReferrals.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null) return NotFound();
        if (entity.Status != PatientReferralStatus.ReservedPendingAdminApproval) return Conflict("این معرفی قابل رد نیست.");
        entity.Status = PatientReferralStatus.Rejected;
        entity.RejectionReason = reason;
        entity.ReviewedByAdminId = UserId();
        entity.ReviewedAt = entity.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Ok(await Item(id, ct));
    }

    [HttpGet("api/patient/referrals/dashboard"), Authorize(Roles = "Patient")]
    public async Task<IActionResult> Dashboard(CancellationToken ct)
    {
        if (!await Valid("Patient", ct)) return Forbid();
        var id = UserId();
        var user = await db.Users.AsNoTracking().SingleAsync(x => x.Id == id, ct);
        var query = db.PatientReferrals.AsNoTracking().Where(x => x.ReferrerPatientUserId == id);
        return Ok(new { user.FirstName, user.LastName, WalletBalance = await db.PatientWallets.Where(x => x.PatientUserId == id).Select(x => (decimal?)x.Balance).SingleOrDefaultAsync(ct) ?? 0, TotalReferrals = await query.CountAsync(ct), PendingReferrals = await query.CountAsync(x => x.Status == PatientReferralStatus.Submitted || x.Status == PatientReferralStatus.Contacted || x.Status == PatientReferralStatus.ReservedPendingAdminApproval, ct), ApprovedReferrals = await query.CountAsync(x => x.Status == PatientReferralStatus.ApprovedRewarded, ct), RejectedReferrals = await query.CountAsync(x => x.Status == PatientReferralStatus.Rejected, ct), RecentReferrals = await Project(query.OrderByDescending(x => x.CreatedAt).Take(10)).ToListAsync(ct) });
    }

    [HttpGet("api/patient/referrals/wallet/transactions"), Authorize(Roles = "Patient")]
    public async Task<IActionResult> Transactions([FromQuery] PatientWalletTransactionType? transactionType, [FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        if (!await Valid("Patient", ct)) return Forbid();
        page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 1, 100);
        var query = db.PatientWalletTransactions.AsNoTracking().Where(x => x.PatientUserId == UserId());
        if (transactionType.HasValue) query = query.Where(x => x.TransactionType == transactionType);
        if (fromDate.HasValue) query = query.Where(x => x.CreatedAt >= fromDate.Value);
        if (toDate.HasValue) query = query.Where(x => x.CreatedAt <= toDate.Value);
        var totalCount = await query.CountAsync(ct);
        var items = await query.OrderByDescending(x => x.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).Select(x => new { TransactionId = x.Id, x.Amount, x.TransactionType, x.Description, x.CreatedAt, x.PatientReferralId, ReferredPatientName = x.PatientReferral.ReferredFirstName + " " + x.PatientReferral.ReferredLastName }).ToListAsync(ct);
        return Ok(new { Items = items, TotalCount = totalCount, Page = page, PageSize = pageSize, TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize) });
    }

    private IQueryable<PatientReferral> Filter(ReferralFilters filters)
    {
        var query = db.PatientReferrals.AsNoTracking();
        if (filters.Status.HasValue) query = query.Where(x => x.Status == filters.Status.Value);
        if (filters.ReferrerPatientUserId.HasValue) query = query.Where(x => x.ReferrerPatientUserId == filters.ReferrerPatientUserId.Value);
        if (filters.SecretaryUserId.HasValue) query = query.Where(x => x.SecretaryUserId == filters.SecretaryUserId.Value);
        if (filters.FromDate.HasValue) query = query.Where(x => x.CreatedAt >= filters.FromDate.Value);
        if (filters.ToDate.HasValue)
        {
            var exclusiveEnd = filters.ToDate.Value.TimeOfDay == TimeSpan.Zero
                ? filters.ToDate.Value.Date.AddDays(1)
                : filters.ToDate.Value;
            query = query.Where(x => x.CreatedAt < exclusiveEnd);
        }
        if (!string.IsNullOrWhiteSpace(filters.Search)) { var search = filters.Search.Trim(); query = query.Where(x => (x.ReferrerPatientUser.FirstName + " " + x.ReferrerPatientUser.LastName).Contains(search) || (x.ReferredFirstName + " " + x.ReferredLastName).Contains(search) || x.ReferredPhoneNumber.Contains(search)); }
        return query;
    }

    private async Task<IActionResult> List(ReferralFilters filters, IQueryable<PatientReferral> query, CancellationToken ct)
    {
        filters.Page = Math.Max(1, filters.Page); filters.PageSize = Math.Clamp(filters.PageSize, 1, 100);
        var totalCount = await query.CountAsync(ct);
        var items = await Project(query.OrderByDescending(x => x.CreatedAt).Skip((filters.Page - 1) * filters.PageSize).Take(filters.PageSize)).ToListAsync(ct);
        return Ok(new { Items = items, TotalCount = totalCount, Page = filters.Page, PageSize = filters.PageSize, TotalPages = (int)Math.Ceiling(totalCount / (double)filters.PageSize) });
    }

    private async Task<object> Item(long id, CancellationToken ct) => await Project(db.PatientReferrals.AsNoTracking().Where(x => x.Id == id)).SingleAsync(ct);
    private static IQueryable<ReferralItem> Project(IQueryable<PatientReferral> query) => query.Select(x => new ReferralItem(x.Id, x.ReferredFirstName, x.ReferredLastName, x.ReferredPhoneNumber, x.Description, x.Status, x.CreatedAt, x.ContactedAt, x.Reservation == null ? null : x.Reservation.ReservationAt, x.ReviewedAt, x.RewardAmount, x.RejectionReason, (x.ReferrerPatientUser.FirstName + " " + x.ReferrerPatientUser.LastName).Trim(), x.SecretaryUser == null ? null : (x.SecretaryUser.FirstName + " " + x.SecretaryUser.LastName).Trim(), x.Reservation != null && !x.Reservation.IsDeleted && !x.Reservation.IsCanceled && x.Reservation.PatientReceivedService == true && x.Reservation.SecretaryApprovedConsultantConfirmation == true));
    private static string NormalizePhone(string? value) { var phone = (value ?? "").Trim().Replace(" ", "").Replace("-", ""); return phone.StartsWith("+98") ? "0" + phone[3..] : phone.StartsWith("0098") ? "0" + phone[4..] : phone; }
    private static string Csv(string? value) => $"\"{(value ?? "").Replace("\"", "\"\"")}\"";
    private static string StatusTitle(PatientReferralStatus status) => status switch { PatientReferralStatus.Submitted => "در انتظار تماس", PatientReferralStatus.Contacted => "تماس گرفته شده", PatientReferralStatus.ReservedPendingAdminApproval => "رزرو شده / در انتظار تأیید", PatientReferralStatus.ApprovedRewarded => "تأیید و پرداخت شده", PatientReferralStatus.Rejected => "رد شده", _ => status.ToString() };
    private sealed record ReferralItem(long Id, string FirstName, string LastName, string PhoneNumber, string? Description, PatientReferralStatus Status, DateTime CreatedAt, DateTime? ContactedAt, DateTime? ReservationAt, DateTime? ReviewedAt, decimal RewardAmount, string? RejectionReason, string ReferrerFullName, string? SecretaryFullName, bool RewardEligible);
}
