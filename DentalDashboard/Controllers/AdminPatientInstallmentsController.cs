using System.Globalization;
using DentalDashboard.Domain.Secretary.Accountant.PatientFinance.Entities;
using DentalDashboard.Domain.Secretary.Accountant.PatientFinance.Enums;
using DentalDashboard.Infrastracture.Context;
using DentalDashboard.Utilities.Time;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.Controllers;

public sealed class AdminInstallmentsFilter
{
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
    public string? Search { get; set; }
    public string? Status { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

public static class PatientInstallmentReportRules
{
    public static IQueryable<PatientInstallment> Filter(IQueryable<PatientInstallment> query,
        AdminInstallmentsFilter filter, DateOnly today)
    {
        query = query.Where(x => !x.IsDeleted && !x.FinancialCase.IsDeleted &&
            x.FinancialCase.Status != PatientFinancialCaseStatus.Cancelled);
        if (filter.FromDate is { } from) {
            var start = from.ToDateTime(TimeOnly.MinValue);
            query = query.Where(x => x.DueDate >= start);
        }
        if (filter.ToDate is { } to) {
            var end = to.AddDays(1).ToDateTime(TimeOnly.MinValue);
            query = query.Where(x => x.DueDate < end);
        }
        var day = today.ToDateTime(TimeOnly.MinValue);
        return filter.Status switch {
            "paid" => query.Where(x => x.PaidAtUtc.HasValue),
            "unpaid" => query.Where(x => !x.PaidAtUtc.HasValue),
            "overdue" => query.Where(x => !x.PaidAtUtc.HasValue && x.DueDate < day),
            "pending" => query.Where(x => !x.PaidAtUtc.HasValue && x.DueDate >= day),
            _ => query
        };
    }
}

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/admin/patient-installments")]
public sealed class AdminPatientInstallmentsController(DentalContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] AdminInstallmentsFilter filter, CancellationToken ct)
    {
        if (filter.FromDate < new DateOnly(1900, 1, 1) || filter.ToDate < new DateOnly(1900, 1, 1) ||
            filter.ToDate == DateOnly.MaxValue || filter.FromDate > filter.ToDate ||
            filter.Status is not (null or "" or "paid" or "unpaid" or "overdue" or "pending"))
            return BadRequest(new { message = "بازه سررسید یا وضعیت اقساط معتبر نیست." });
        var today = IranTimeHelper.TodayInIran();
        var query = PatientInstallmentReportRules.Filter(db.PatientInstallments.AsNoTracking(), filter, today);
        if (!string.IsNullOrWhiteSpace(filter.Search)) {
            var term = filter.Search.Trim();
            query = query.Where(x => (x.FinancialCase.Patient.FirstName + " " + x.FinancialCase.Patient.LastName).Contains(term) ||
                x.FinancialCase.Patient.PhoneNumber.Contains(term) || db.PatientFiles.Any(f =>
                    f.PhoneNumber == x.FinancialCase.Patient.PhoneNumber && f.FileNumber.ToString() == term));
        }
        var day = today.ToDateTime(TimeOnly.MinValue);
        var totalCount = await query.CountAsync(ct);
        var totalAmount = await query.SumAsync(x => (decimal?)x.Amount, ct) ?? 0;
        var paidAmount = await query.Where(x => x.PaidAtUtc.HasValue).SumAsync(x => (decimal?)x.Amount, ct) ?? 0;
        var overdueAmount = await query.Where(x => !x.PaidAtUtc.HasValue && x.DueDate < day).SumAsync(x => (decimal?)x.Amount, ct) ?? 0;
        var page = Math.Max(1, filter.Page);
        var pageSize = Math.Clamp(filter.PageSize, 1, 100);
        var rows = await query.OrderBy(x => x.DueDate).ThenBy(x => x.Id).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new {
                x.Id, CaseId = x.PatientFinancialCaseId, x.Amount, x.DueDate, x.PaidAtUtc,
                PatientName = (x.FinancialCase.Patient.FirstName + " " + x.FinancialCase.Patient.LastName).Trim(),
                x.FinancialCase.Patient.PhoneNumber,
                FileNumber = db.PatientFiles.Where(f => f.PhoneNumber == x.FinancialCase.Patient.PhoneNumber)
                    .OrderByDescending(f => f.CreatedAt).Select(f => f.FileNumber.ToString()).FirstOrDefault() ?? "",
                Description = x.FinancialCase.Notes,
                PaidBy = db.Users.Where(u => u.Id == x.PaidByUserId).Select(u => (u.FirstName + " " + u.LastName).Trim()).FirstOrDefault()
            }).ToListAsync(ct);
        return Ok(new {
            Items = rows.Select(x => new {
                x.Id, x.CaseId, x.Amount, x.PatientName, x.PhoneNumber, x.FileNumber, x.Description, x.PaidBy,
                // Installment schedules are calendar dates, not UTC instants like cheque due dates.
                DueDate = x.DueDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                PaidAtUtc = x.PaidAtUtc.HasValue ? DateTime.SpecifyKind(x.PaidAtUtc.Value, DateTimeKind.Utc) : (DateTime?)null,
                Status = x.PaidAtUtc.HasValue ? "paid" : x.DueDate < day ? "overdue" : "pending"
            }), TotalCount = totalCount, PageNumber = page, PageSize = pageSize,
            Summary = new { TotalAmount = totalAmount, PaidAmount = paidAmount, RemainingAmount = totalAmount - paidAmount, OverdueAmount = overdueAmount }
        });
    }
}
