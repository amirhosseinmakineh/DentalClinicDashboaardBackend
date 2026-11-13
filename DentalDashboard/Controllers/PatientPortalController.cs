using System.Security.Claims;
using DentalDashboard.Domain.Secretary.Accountant.PatientFinance.Enums;
using DentalDashboard.Infrastracture.Context;
using DentalDashboard.Utilities.Hasher;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.Controllers;

public sealed record PatientSelfProfileUpdate(string FirstName, string LastName);
public sealed record PatientSelfPasswordChange(string CurrentPassword, string NewPassword);

[ApiController]
[Route("api/patient/portal")]
[Authorize(Roles = "Patient")]
public sealed class PatientPortalController(DentalContext db) : ControllerBase
{
    private Guid? CurrentUserId
    {
        get
        {
            var raw = User.FindFirstValue(ClaimTypes.NameIdentifier) ??
                User.FindFirstValue("userId") ?? User.FindFirstValue("Id");
            return Guid.TryParse(raw, out var id) ? id : null;
        }
    }

    [HttpGet("overview")]
    public async Task<IActionResult> Overview(CancellationToken ct)
    {
        if (CurrentUserId is not { } id) return Unauthorized();
        var user = await db.Users.AsNoTracking()
            .Where(x => x.Id == id && !x.IsDeleted && x.IsActive)
            .Select(x => new { x.Id, x.FirstName, x.LastName, x.PhoneNumber, x.BirthDate, x.Gender,
                NationalCode = x.PatientProfile != null && !x.PatientProfile.IsDeleted ? x.PatientProfile.NationalCode : null })
            .SingleOrDefaultAsync(ct);
        if (user is null) return NotFound();

        var cases = await db.PatientFinancialCases.AsNoTracking()
            .Where(x => x.PatientId == id && !x.IsDeleted)
            .Include(x => x.Cheques).Include(x => x.PromissoryNotes)
            .Include(x => x.Transactions).Include(x => x.Debts)
            .AsSplitQuery().OrderByDescending(x => x.CreatedAt).ToListAsync(ct);

        var workflows = await db.PatientFileWorkflows.AsNoTracking()
            .Where(x => x.FinancialCaseId.HasValue && cases.Select(c => c.Id).Contains(x.FinancialCaseId.Value))
            .Select(x => new { x.FinancialCaseId, x.PatientFileId, x.TreatmentConfirmedAtUtc,
                x.FinanceConfirmedAtUtc, x.ProfileConfirmedAtUtc, x.ApprovedAtUtc })
            .ToListAsync(ct);
        var fileIds = workflows.Select(x => x.PatientFileId).ToList();
        var files = await db.PatientFiles.AsNoTracking().Where(x => fileIds.Contains(x.Id) && !x.IsDeleted)
            .Select(x => new { x.Id, x.FileNumber, x.CreatedAt }).ToListAsync(ct);

        var result = cases.Select(c =>
        {
            var paid = c.PrePaymentAmount + c.DepositAmount +
                c.Transactions.Where(t => !t.IsDeleted).Sum(t => t.Amount);
            var wf = workflows.FirstOrDefault(x => x.FinancialCaseId == c.Id);
            var file = files.FirstOrDefault(x => x.Id == wf?.PatientFileId);
            var cheques = c.Cheques.Where(x => !x.IsDeleted).OrderBy(x => x.DueDate)
                .Select(x => new { x.Id, x.Amount, x.DueDate, x.Status, x.IsGuarantee, x.SayadNumber }).ToList();
            var notes = c.PromissoryNotes.Where(x => !x.IsDeleted).OrderBy(x => x.DueDate)
                .Select(x => new { x.Id, x.Amount, x.DueDate, x.Status, x.SerialNumber }).ToList();
            var debts = c.Debts.Where(x => !x.IsDeleted).OrderBy(x => x.DueDate)
                .Select(x => new { x.Id, x.Amount, x.DueDate, x.Status, x.SourceType }).ToList();
            var transactions = c.Transactions.Where(x => !x.IsDeleted).OrderByDescending(x => x.CreatedAt)
                .Select(x => new { x.Id, x.Amount, x.CreatedAt, x.Type, x.SourceType }).ToList();
            var stage = wf is null ? "FinancialCaseOnly" :
                wf.TreatmentConfirmedAtUtc is null ? "TreatmentPending" :
                wf.FinanceConfirmedAtUtc is null ? "FinancePending" :
                wf.ProfileConfirmedAtUtc is null ? "ProfilePending" :
                wf.ApprovedAtUtc is null ? "TreatmentApprovalPending" : "TreatmentApproved";
            return new { c.Id, c.CreatedAt, c.TotalAmount, PaidAmount = paid,
                RemainingAmount = Math.Max(0, c.TotalAmount - paid), c.PaymentMethod, c.Status,
                c.Services, c.ServiceUnitCountsJson, c.InstallmentStatus, c.AgreementType,
                FileNumber = file?.FileNumber, Stage = stage,
                wf?.TreatmentConfirmedAtUtc, wf?.FinanceConfirmedAtUtc, wf?.ProfileConfirmedAtUtc,
                wf?.ApprovedAtUtc, Cheques = cheques, Notes = notes, Debts = debts, Transactions = transactions };
        }).ToList();
        return Ok(new {
            profile = user,
            summary = new { TotalTreatment = result.Sum(x => x.TotalAmount),
                TotalPaid = result.Sum(x => x.PaidAmount),
                TotalRemaining = result.Sum(x => x.RemainingAmount),
                CaseCount = result.Count },
            cases = result
        });
    }

    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile(PatientSelfProfileUpdate request, CancellationToken ct)
    {
        if (CurrentUserId is not { } id) return Unauthorized();
        var first = request.FirstName?.Trim();
        var last = request.LastName?.Trim();
        if (string.IsNullOrWhiteSpace(first) || string.IsNullOrWhiteSpace(last) ||
            first.Length > 100 || last.Length > 100)
            return BadRequest(new { message = "نام و نام خانوادگی باید بین ۱ تا ۱۰۰ کاراکتر باشند." });
        var user = await db.Users.SingleOrDefaultAsync(x => x.Id == id && !x.IsDeleted && x.IsActive, ct);
        if (user is null) return NotFound();
        user.FirstName = first; user.LastName = last; user.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Ok(new { user.FirstName, user.LastName });
    }

    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(PatientSelfPasswordChange request, CancellationToken ct)
    {
        if (CurrentUserId is not { } id) return Unauthorized();
        if (string.IsNullOrEmpty(request.NewPassword) || request.NewPassword.Length < 10 ||
            request.NewPassword.Length > 128 || request.NewPassword == request.CurrentPassword)
            return BadRequest(new { message = "رمز جدید باید ۱۰ تا ۱۲۸ کاراکتر و متفاوت با رمز قبلی باشد." });
        var user = await db.Users.SingleOrDefaultAsync(x => x.Id == id && !x.IsDeleted && x.IsActive, ct);
        if (user is null) return NotFound();
        if (!PasswordHasher.VerifyPassword(request.CurrentPassword, user.PasswordHash))
            return BadRequest(new { message = "رمز عبور فعلی صحیح نیست." });
        user.PasswordHash = PasswordHasher.HashPassword(request.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Ok(new { message = "رمز عبور تغییر کرد. برای امنیت، دوباره وارد حساب شوید." });
    }
}
