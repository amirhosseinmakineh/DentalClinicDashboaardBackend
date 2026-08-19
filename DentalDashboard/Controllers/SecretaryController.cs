using DentalDashboard.ApplicationService.Contract.Requests.Secretary.Commands;
using DentalDashboard.ApplicationService.Contract.Requests.Secretary.Queries;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using DentalDashboard.ApplicationService.Contract.IServices;
using DentalDashboard.ApplicationService.Contract.Responses.Secretary;
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
    private readonly IUserRepository userRepository;

    public SecretaryController(
        ICommandDispatcher dispatcher,
        IQueryDispatcher queryDispatcher,
        ISecretaryAccessService accessService,
        ILeadAssignmentRepository leadAssignmentRepository,
        IReservationRepository reservationRepository,
        IUserRepository userRepository)
    {
        this.dispatcher = dispatcher;
        this.queryDispatcher = queryDispatcher;
        this.accessService = accessService;
        this.leadAssignmentRepository = leadAssignmentRepository;
        this.reservationRepository = reservationRepository;
        this.userRepository = userRepository;
    }

    [HttpGet("reservation-form-options")]
    [Authorize]
    public async Task<IActionResult> GetReservationFormOptions(
        [FromQuery] string? patientSearch,
        [FromQuery] string? consultantSearch,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetCurrentUserId(out var userId)) return Unauthorized();

        var access = await accessService.GetAccessAsync(userId, cancellationToken);
        if (!access.IsSecretary ||
            !await accessService.HasPermissionAsync(userId, SecretaryPermissionType.CreateReservation, cancellationToken) ||
            !await accessService.HasPermissionAsync(userId, SecretaryPermissionType.ViewPatients, cancellationToken))
            return Forbid();

        var take = Math.Clamp(pageSize, 1, 100);
        var patientsQuery = leadAssignmentRepository.GetAll()
            .AsNoTracking()
            .Where(x => !x.IsDeleted &&
                        x.ConsultantProfileId.HasValue &&
                        x.ReportSubmittedAt.HasValue &&
                        (x.CallResult == LeadCallResult.Contacted || x.CallResult == LeadCallResult.Converted) &&
                        !x.ConsultantProfile!.IsDeleted &&
                        x.ConsultantProfile.IsCompleteProfile &&
                        !x.ConsultantProfile.User.IsDeleted &&
                        x.ConsultantProfile.User.IsActive &&
                        !reservationRepository.GetAll().Any(r =>
                            !r.IsDeleted && !r.IsCanceled && r.LeadAssignmentId == x.Id));

        if (!string.IsNullOrWhiteSpace(patientSearch))
        {
            var search = patientSearch.Trim();
            patientsQuery = patientsQuery.Where(x =>
                x.UserName.Contains(search) || x.PhoneNumber.Contains(search));
        }

        var patients = await patientsQuery
            .OrderBy(x => x.UserName)
            .ThenByDescending(x => x.Id)
            .Take(take)
            .Select(x => new ReservationPatientOptionResponse
            {
                LeadAssignmentId = x.Id,
                ConsultantProfileId = x.ConsultantProfileId!.Value,
                FullName = x.UserName,
                PhoneNumber = x.PhoneNumber
            })
            .ToListAsync(cancellationToken);

        var consultantsQuery = userRepository.GetAll()
            .AsNoTracking()
            .Where(x => !x.IsDeleted && x.IsActive &&
                        x.ConsultantProfile != null &&
                        !x.ConsultantProfile.IsDeleted &&
                        x.ConsultantProfile.IsCompleteProfile &&
                        x.UserRoles.Any(ur => !ur.IsDeleted &&
                                              !ur.Role.IsDeleted &&
                                              ur.Role.RoleName == "Consultant"));

        if (!string.IsNullOrWhiteSpace(consultantSearch))
        {
            var search = consultantSearch.Trim();
            consultantsQuery = consultantsQuery.Where(x =>
                x.FirstName.Contains(search) || x.LastName.Contains(search) ||
                (x.FirstName + " " + x.LastName).Contains(search) || x.PhoneNumber.Contains(search));
        }

        var consultants = await consultantsQuery
            .OrderBy(x => x.LastName)
            .ThenBy(x => x.FirstName)
            .Take(take)
            .Select(x => new ReservationConsultantOptionResponse
            {
                ProfileId = x.ConsultantProfile!.Id,
                FirstName = x.FirstName,
                LastName = x.LastName,
                PhoneNumber = x.PhoneNumber
            })
            .ToListAsync(cancellationToken);

        return Ok(Result<ReservationFormOptionsResponse>.Success(new ReservationFormOptionsResponse
        {
            Patients = patients,
            Consultants = consultants
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
