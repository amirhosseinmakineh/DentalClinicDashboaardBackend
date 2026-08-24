using DentalDashboard.ApplicationService.Contract.Requests.Secretary.Commands;
using DentalDashboard.ApplicationService.Contract.Requests.Secretary.Queries;
using DentalDashboard.ApplicationService.Contract.Requests.FollowUp.Commands;
using DentalDashboard.ApplicationService.Contract.Requests.FollowUp.Queries;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using DentalDashboard.ApplicationService.Contract.IServices;
using DentalDashboard.ApplicationService.Contract.Responses.Secretary;
using DentalDashboard.ApplicationService.Contract.Responses;
using DentalDashboard.Domain.Enums;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Framwork.Domain;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.Controllers;

[Route("api/[controller]")]
[ApiController]
public class SecretaryController : ControllerBase
{
    private readonly ICommandDispatcher dispatcher;
    private readonly IQueryDispatcher queryDispatcher;
    private readonly ISecretaryAccessService accessService;
    private readonly ILeadAssignmentRepository leadAssignmentRepository;
    private readonly IReservationRepository reservationRepository;

    public SecretaryController(
        ICommandDispatcher dispatcher,
        IQueryDispatcher queryDispatcher,
        ISecretaryAccessService accessService,
        ILeadAssignmentRepository leadAssignmentRepository,
        IReservationRepository reservationRepository)
    {
        this.dispatcher = dispatcher;
        this.queryDispatcher = queryDispatcher;
        this.accessService = accessService;
        this.leadAssignmentRepository = leadAssignmentRepository;
        this.reservationRepository = reservationRepository;
    }

    [HttpGet("follow-ups/patients")]
    [Authorize]
    public async Task<IActionResult> SearchFollowUpPatients(
        [FromQuery] SearchSecretaryFollowUpPatientsQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(
            await queryDispatcher.DispatchAsync(query, cancellationToken)
        );
    }

    [HttpGet("follow-ups/patients/{patientId:long}")]
    [Authorize]
    public async Task<IActionResult> GetFollowUpPatient(
        long patientId,
        CancellationToken cancellationToken)
    {
        var result = await queryDispatcher.DispatchAsync(
            new GetSecretaryFollowUpPatientInfoQuery
            {
                PatientId = patientId
            },
            cancellationToken
        );

        return result is null
            ? NotFound(Result.Failure("بیمار یا رزرو مرتبط یافت نشد"))
            : Ok(result);
    }

    [HttpGet("follow-ups")]
    [Authorize]
    public async Task<IActionResult> GetFollowUps(
        [FromQuery] GetSecretaryFollowUpsQuery query,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
            return Unauthorized();

        query.SecretaryUserId = userId;

        return Ok(
            await queryDispatcher.DispatchAsync(query, cancellationToken)
        );
    }

    [HttpGet("follow-ups/{id:long}")]
    [Authorize]
    public async Task<IActionResult> GetFollowUp(
        long id,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
            return Unauthorized();

        var result = await queryDispatcher.DispatchAsync(
            new GetSecretaryFollowUpByIdQuery
            {
                Id = id,
                SecretaryUserId = userId
            },
            cancellationToken
        );

        return result is null
            ? NotFound(Result.Failure("پیگیری یافت نشد"))
            : Ok(result);
    }

    [HttpPost("follow-ups")]
    [Authorize]
    public async Task<IActionResult> CreateFollowUp(
        CreateSecretaryFollowUpCommand command,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
            return Unauthorized();

        command.SecretaryUserId = userId;

        return Ok(
            await dispatcher.DispatchAsync(command, cancellationToken)
        );
    }

    [HttpPut("follow-ups/{id:long}")]
    [Authorize]
    public async Task<IActionResult> UpdateFollowUp(
        long id,
        UpdateSecretaryFollowUpCommand command,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
            return Unauthorized();

        command.Id = id;
        command.SecretaryUserId = userId;

        return Ok(
            await dispatcher.DispatchAsync(command, cancellationToken)
        );
    }

    [HttpDelete("follow-ups/{id:long}")]
    [Authorize]
    public async Task<IActionResult> DeleteFollowUp(
        long id,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
            return Unauthorized();

        return Ok(
            await dispatcher.DispatchAsync(
                new DeleteSecretaryFollowUpCommand
                {
                    Id = id,
                    SecretaryUserId = userId
                },
                cancellationToken
            )
        );
    }

    [HttpGet("after-sales-patients")]
    [Authorize]
    public async Task<IActionResult> GetAfterSalesPatients(
        [FromQuery] string? searchText,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetCurrentUserId(out var userId)) return Unauthorized();

        var access = await accessService.GetAccessAsync(userId, cancellationToken);
        if (!access.IsSecretary ||
            !await accessService.HasPermissionAsync(userId, SecretaryPermissionType.CreateReservation, cancellationToken) ||
            !await accessService.HasPermissionAsync(userId, SecretaryPermissionType.ViewPatients, cancellationToken))
            return StatusCode(StatusCodes.Status403Forbidden,
                Result.Failure("شما دسترسی مشاهده بیماران و ایجاد رزرو را ندارید"));

        var normalizedPageNumber = Math.Max(pageNumber, 1);
        var normalizedPageSize = Math.Clamp(pageSize, 1, 100);
        var patientsQuery = leadAssignmentRepository.GetAll()
            .AsNoTracking()
            .Where(x => !x.IsDeleted &&
                        x.ReportSubmittedAt.HasValue &&
                        (x.CallResult == LeadCallResult.Contacted || x.CallResult == LeadCallResult.Converted) &&
                        !reservationRepository.GetAll().Any(r =>
                            !r.IsDeleted && !r.IsCanceled && r.LeadAssignmentId == x.Id));

        if (!string.IsNullOrWhiteSpace(searchText))
        {
            var search = searchText.Trim();
            patientsQuery = patientsQuery.Where(x =>
                x.UserName.Contains(search) || x.PhoneNumber.Contains(search));
        }

        var totalCount = await patientsQuery.CountAsync(cancellationToken);
        var patients = await patientsQuery
            .OrderBy(x => x.UserName)
            .ThenByDescending(x => x.Id)
            .Skip((normalizedPageNumber - 1) * normalizedPageSize)
            .Take(normalizedPageSize)
            .Select(x => new AfterSalesPatientOptionResponse
            {
                LeadAssignmentId = x.Id,
                FullName = x.UserName,
                PhoneNumber = x.PhoneNumber,
                ConsultantProfileId = x.ConsultantProfile != null &&
                                      !x.ConsultantProfile.IsDeleted &&
                                      x.ConsultantProfile.IsCompleteProfile &&
                                      !x.ConsultantProfile.User.IsDeleted &&
                                      x.ConsultantProfile.User.IsActive
                    ? x.ConsultantProfileId
                    : null,
                ConsultantFullName = x.ConsultantProfile != null
                    ? x.ConsultantProfile.User.FirstName + " " + x.ConsultantProfile.User.LastName
                    : null,
                ConsultantPhoneNumber = x.ConsultantProfile != null
                    ? x.ConsultantProfile.User.PhoneNumber
                    : null
            })
            .ToListAsync(cancellationToken);

        return Ok(Result<PaginatedResult<AfterSalesPatientOptionResponse>>.Success(new PaginatedResult<AfterSalesPatientOptionResponse>
        {
            Items = patients,
            PageNumber = normalizedPageNumber,
            PageSize = normalizedPageSize,
            TotalCount = totalCount
        }));
    }

    [HttpGet("access")]
    [Authorize]
    public async Task<IActionResult> GetAccess(CancellationToken cancellationToken)
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("userId") ?? User.FindFirstValue("Id");
        if (!Guid.TryParse(claim, out var userId)) return Unauthorized();
        var access = await accessService.GetAccessAsync(userId, cancellationToken);
        if (!access.IsSecretary) return Forbid();
        return Ok(new
        {
            access.IsSecretary,
            access.HasFullAccess,
            AllowedDays = access.AllowedDays.Select(x => x.ToString()),
            Permissions = access.Permissions.Select(x => x.ToString())
        });
    }

    [HttpPost]
    public async Task<IActionResult> CompleteProfile(
        CompleteSecretaryProfileCommand command,
        CancellationToken cancellationToken)
    {
        var result = await dispatcher.DispatchAsync(command, cancellationToken);
        return Ok(result);
    }

    [HttpGet("dashboard/summary")]
    [Authorize]
    public async Task<IActionResult> GetDashboardSummary(CancellationToken cancellationToken)
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("userId") ?? User.FindFirstValue("Id");
        if (!Guid.TryParse(claim, out var userId)) return Unauthorized();
        if (!await accessService.HasPermissionAsync(userId, DentalDashboard.Domain.Enums.SecretaryPermissionType.ViewReservations,
                cancellationToken)) return Forbid();
        var result = await queryDispatcher.DispatchAsync(
            new GetSecretaryDashboardSummaryQuery { SecretaryUserId = userId },
            cancellationToken);
        return Ok(result);
    }

    private bool TryGetCurrentUserId(out Guid userId)
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier) ??
                    User.FindFirstValue("userId") ??
                    User.FindFirstValue("Id");
        return Guid.TryParse(claim, out userId);
    }
}
