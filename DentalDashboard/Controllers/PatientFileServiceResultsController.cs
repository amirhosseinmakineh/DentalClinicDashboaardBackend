using System.Security.Claims;
using DentalDashboard.ApplicationService.Contract.IServices;
using DentalDashboard.ApplicationService.Contract.Requests.Reservation.Commands;
using DentalDashboard.Domain.Enums;
using DentalDashboard.Domain.Models;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using DentalDashboard.Infrastracture.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.Controllers;

public static class PatientFileServiceResultRules
{
    public static bool Matches(PatientFile file, long leadAssignmentId, string phoneNumber) =>
        file.PatientReferenceId.HasValue ? file.PatientReferenceId == leadAssignmentId :
        !string.IsNullOrWhiteSpace(file.PhoneNumber) && file.PhoneNumber == phoneNumber;
}

[ApiController]
[Authorize(Roles = "Admin,Secretary")]
[Route("api/secretary/patient-files/{patientFileId:long}/service-results")]
public sealed class PatientFileServiceResultsController(DentalContext db, IQueryDispatcher queries,
    ICommandDispatcher commands, ISecretaryAccessService access) : ControllerBase
{
    private Guid? Actor => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ??
        User.FindFirstValue("userId") ?? User.FindFirstValue("Id"), out var id) ? id : null;

    private Task<PatientFileWorkflow?> Workflow(long id, CancellationToken ct) => db.PatientFileWorkflows
        .AsNoTracking().Include(x => x.PatientFile)
        .Include(x => x.FinancialCase!).ThenInclude(x => x.Cheques)
        .Include(x => x.FinancialCase!).ThenInclude(x => x.PromissoryNotes)
        .Include(x => x.FinancialCase!).ThenInclude(x => x.Transactions)
        .Include(x => x.FinancialCase!).ThenInclude(x => x.Debts)
        .AsSplitQuery().SingleOrDefaultAsync(x => x.PatientFileId == id && !x.PatientFile.IsDeleted, ct);

    [HttpGet]
    public async Task<IActionResult> Get(long patientFileId, [FromQuery] int page = 1, CancellationToken ct = default)
    {
        if (Actor is not { } actor) return Unauthorized();
        if (!User.IsInRole("Admin") && !await access.HasPermissionAsync(actor, SecretaryPermissionType.ViewReservations)) return Forbid();
        var workflow = await Workflow(patientFileId, ct);
        if (workflow is null) return NotFound();
        if (!PatientFileWorkflowRules.IsApproved(workflow))
            return BadRequest(new { message = "ابتدا مجوز انجام خدمات این پرونده را تأیید کنید." });
        var file = workflow.PatientFile;
        if (!file.PatientReferenceId.HasValue && string.IsNullOrWhiteSpace(file.PhoneNumber))
            return BadRequest(new { message = "شناسه بیمار برای یافتن رزرو مشخص نیست." });
        var result = await queries.DispatchAsync(new GetSecretaryReservationsQuery {
            PatientFileLeadAssignmentId = file.PatientReferenceId,
            PatientFilePhoneNumber = file.PhoneNumber,
            SecretaryUserId = actor, IsAdmin = User.IsInRole("Admin"),
            PageNumber = page, PageSize = 10, SortDirection = "desc", IncludeCanceled = false
        }, ct);
        var reviewable = new List<long>();
        if (await access.HasPermissionAsync(actor, SecretaryPermissionType.ConfirmAttendance))
            foreach (var item in result.Items)
                if (await access.CanAccessReservationAsync(actor, item.Id)) reviewable.Add(item.Id);
        return Ok(new { result.Items, result.PageNumber, result.TotalPages, result.TotalCount, reviewableReservationIds = reviewable });
    }

    [HttpPost]
    public async Task<IActionResult> Review(long patientFileId, ReviewReservationAttendanceCommand command, CancellationToken ct)
    {
        if (Actor is not { } actor) return Unauthorized();
        if (!await access.HasPermissionAsync(actor, SecretaryPermissionType.ConfirmAttendance) ||
            !await access.CanAccessReservationAsync(actor, command.ReservationId)) return Forbid();
        var workflow = await Workflow(patientFileId, ct);
        if (workflow is null) return NotFound();
        if (!PatientFileWorkflowRules.IsApproved(workflow))
            return BadRequest(new { message = "مجوز انجام خدمات این پرونده معتبر نیست؛ پرونده را دوباره بررسی کنید." });
        var reservation = await db.Reservations.AsNoTracking().Include(x => x.LeadAssignment)
            .SingleOrDefaultAsync(x => x.Id == command.ReservationId && !x.IsDeleted, ct);
        if (reservation is null || !PatientFileServiceResultRules.Matches(workflow.PatientFile,
            reservation.LeadAssignmentId, reservation.LeadAssignment.PhoneNumber))
            return BadRequest(new { message = "این رزرو متعلق به بیمار این پرونده نیست." });
        // Preserve the existing result/reward handler and its duplicate, date and doctor validation.
        command.SecretaryUserId = actor;
        var result = await commands.DispatchAsync(command, ct);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }
}
