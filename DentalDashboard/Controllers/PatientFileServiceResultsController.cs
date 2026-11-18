using System.Data;
using System.Text.Json;
using DentalDashboard.Services;
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
        .Include(x => x.FinancialCase!).ThenInclude(x => x.Installments)
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
        var resultIds = result.Items.Select(x => x.Id).ToList();
        var recipients = await db.Reservations.AsNoTracking().Where(x => resultIds.Contains(x.Id) && x.RewardConsultantProfileId.HasValue)
            .Select(x => new { x.Id, Name = (x.RewardConsultantProfile!.User.FirstName + " " + x.RewardConsultantProfile.User.LastName).Trim() })
            .ToDictionaryAsync(x => x.Id, x => x.Name, ct);
        foreach (var item in result.Items) item.RewardConsultantFullName = recipients.GetValueOrDefault(item.Id);
        var reviewable = new List<long>();
        if (await access.HasPermissionAsync(actor, SecretaryPermissionType.ConfirmAttendance))
            foreach (var item in result.Items)
                if (await access.CanAccessReservationAsync(actor, item.Id)) reviewable.Add(item.Id);
        return Ok(new { result.Items, result.PageNumber, result.TotalPages, result.TotalCount, reviewableReservationIds = reviewable });
    }

    private async Task<(PatientFileWorkflow? Workflow, Reservation? Reservation, IActionResult? Error)> RewardScope(
        long patientFileId, long reservationId, CancellationToken ct)
    {
        if (Actor is not { } actor) return (null, null, Unauthorized());
        if (!await access.HasPermissionAsync(actor, SecretaryPermissionType.ConfirmAttendance) ||
            !await access.CanAccessReservationAsync(actor, reservationId)) return (null, null, Forbid());
        var workflow = await Workflow(patientFileId, ct);
        if (workflow is null) return (null, null, NotFound());
        if (!PatientFileWorkflowRules.IsApproved(workflow))
            return (null, null, BadRequest(new { message = "مجوز انجام خدمات این پرونده معتبر نیست؛ پرونده را دوباره بررسی کنید." }));
        var reservation = await db.Reservations.Include(x => x.LeadAssignment)
            .SingleOrDefaultAsync(x => x.Id == reservationId && !x.IsDeleted, ct);
        if (reservation is null || !PatientFileServiceResultRules.Matches(workflow.PatientFile,
            reservation.LeadAssignmentId, reservation.LeadAssignment.PhoneNumber))
            return (null, null, BadRequest(new { message = "این رزرو متعلق به بیمار این پرونده نیست." }));
        return (workflow, reservation, null);
    }

    [HttpGet("reward-context")]
    public async Task<IActionResult> RewardContext(long patientFileId, [FromQuery] long reservationId, CancellationToken ct)
    {
        var scope = await RewardScope(patientFileId, reservationId, ct);
        if (scope.Error is not null) return scope.Error;
        return Ok(await new ServiceRewardPreparationService(db).Context(scope.Workflow!, scope.Reservation!, ct));
    }

    [HttpPost("reward-preview")]
    public async Task<IActionResult> Preview(long patientFileId, ReviewReservationAttendanceCommand command, CancellationToken ct)
    {
        var scope = await RewardScope(patientFileId, command.ReservationId, ct);
        if (scope.Error is not null) return scope.Error;
        var prepared = await new ServiceRewardPreparationService(db).Prepare(scope.Workflow!, scope.Reservation!, command, ct);
        return prepared.Error is not null ? BadRequest(new { message = prepared.Error }) : Ok(prepared.Reward);
    }

    [HttpPost]
    public async Task<IActionResult> Review(long patientFileId, ReviewReservationAttendanceCommand command, CancellationToken ct)
    {
        if (command.Note?.Length > 1000) return BadRequest(new { message = "یادداشت حداکثر ۱۰۰۰ کاراکتر است." });
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var scope = await RewardScope(patientFileId, command.ReservationId, ct);
        if (scope.Error is not null) return scope.Error;
        var received = command.PatientReceivedService ?? command.Approved;
        if (received == true) {
            var prepared = await new ServiceRewardPreparationService(db).Prepare(scope.Workflow!, scope.Reservation!, command, ct);
            if (prepared.Error is not null) return BadRequest(new { message = prepared.Error });
            var quote = prepared.Reward!;
            if (command.ExpectedRewardHash != quote.Hash)
                return Conflict(new { message = "نرخ یا سوابق خدمت تغییر کرده‌اند؛ پاداش را دوباره محاسبه و تأیید کنید." });
            var reservation = scope.Reservation!;
            reservation.RewardConsultantProfileId = quote.Snapshot.ConsultantProfileId;
            reservation.ServiceRewardSnapshotJson = JsonSerializer.Serialize(quote.Snapshot);
            reservation.ConsultantRewardAmount = quote.Snapshot.Total;
            foreach (var line in quote.Snapshot.Lines)
                foreach (var tooth in line.Teeth)
                    db.ConsultantRewardExecutions.Add(new() { FinancialCaseId = quote.Snapshot.FinancialCaseId,
                        ReservationId = reservation.Id, ConsultantProfileId = quote.Snapshot.ConsultantProfileId,
                        ServiceId = line.ServiceId, ToothId = tooth, PerformedAtUtc = quote.Snapshot.CalculatedAtUtc });
            command.ServiceRewardManaged = true;
        }
        command.SecretaryUserId = Actor!.Value;
        try {
            var result = await commands.DispatchAsync(command, ct);
            if (!result.IsSuccess) return BadRequest(result);
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return Ok(result);
        } catch (DbUpdateException error) when (error.InnerException is Microsoft.Data.SqlClient.SqlException { Number: 2601 or 2627 }) {
            await tx.RollbackAsync(ct);
            return Conflict(new { message = "این خدمت هم‌زمان ثبت شده است؛ فرم را تازه کنید." });
        }
    }
}
