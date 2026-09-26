using DentalDashboard.ApplicationService.Contract.Secretary.PatientFiles;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using DentalDashboard.Framwork.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DentalDashboard.Controllers;

[ApiController]
[Route("api/secretary/patient-files")]
[Authorize(Roles = "Admin,Secretary")]
public sealed class SecretaryPatientFilesController(ICommandDispatcher commandDispatcher, IQueryDispatcher queryDispatcher) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] GetPatientFilesQuery query, CancellationToken cancellationToken)
    {
        if (!SetScope(query)) return Unauthorized();
        return ToResponse(await queryDispatcher.DispatchAsync(query, cancellationToken));
    }

    [HttpGet("eligible-patients")]
    public async Task<IActionResult> GetEligiblePatients([FromQuery] SearchPatientsEligibleForFileQuery query, CancellationToken cancellationToken) =>
        ToResponse(await queryDispatcher.DispatchAsync(query, cancellationToken));

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken)
    {
        if (!TryGetScope(out var secretaryUserId, out var isAdmin)) return Unauthorized();
        return ToResponse(await queryDispatcher.DispatchAsync(
            new GetPatientFileByIdQuery(id, secretaryUserId, isAdmin), cancellationToken));
    }

    [HttpPost]
    [Authorize(Roles = "Secretary")]
    public async Task<IActionResult> Create(CreatePatientFileRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var secretaryUserId)) return Unauthorized();
        return ToResponse(await commandDispatcher.DispatchAsync(
            new CreatePatientFileCommand(
                request.FirstName,
                request.LastName,
                request.PhoneNumber,
                request.Description,
                secretaryUserId),
            cancellationToken));
    }

    [HttpPost("from-reservation")]
    [Authorize(Roles = "Secretary")]
    public async Task<IActionResult> CreateFromReservation(CreatePatientFileFromReservationRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var secretaryUserId)) return Unauthorized();
        return ToResponse(await commandDispatcher.DispatchAsync(
            new CreatePatientFileFromReservationCommand(request.PatientId, secretaryUserId), cancellationToken));
    }

    [HttpPost("{id:long}/financial-identity")]
    public async Task<IActionResult> EnsureFinancialIdentity(long id, CancellationToken cancellationToken)
    {
        if (!TryGetScope(out var secretaryUserId, out var isAdmin)) return Unauthorized();
        return ToResponse(await commandDispatcher.DispatchAsync(
            new EnsurePatientFileFinancialIdentityCommand(id, secretaryUserId, isAdmin),
            cancellationToken));
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, UpdatePatientFileRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetScope(out var secretaryUserId, out var isAdmin)) return Unauthorized();
        var result = await commandDispatcher.DispatchAsync(
            new UpdatePatientFileCommand(
                id,
                request.FirstName,
                request.LastName,
                request.PhoneNumber,
                request.Description,
                secretaryUserId,
                isAdmin),
            cancellationToken);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
    {
        if (!TryGetScope(out var secretaryUserId, out var isAdmin)) return Unauthorized();
        var result = await commandDispatcher.DispatchAsync(
            new DeletePatientFileCommand(id, secretaryUserId, isAdmin),
            cancellationToken);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    [HttpPost("import")]
    [Authorize(Roles = "Secretary")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> Import([FromForm] PatientFileImportForm form, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var secretaryUserId)) return Unauthorized();
        if (form.File is null) return BadRequest("فایل الزامی است");

        await using var fileStream = form.File.OpenReadStream();
        var result = await commandDispatcher.DispatchAsync(
            new ImportPatientFilesCommand(
                fileStream,
                form.File.FileName,
                form.File.Length,
                secretaryUserId),
            cancellationToken);

        if (!result.IsSuccess || result.Data is null)
            return BadRequest(Result.Failure(result.Message));

        return result.Data.Success ? Ok(result.Data) : BadRequest(result.Data);
    }

    private bool SetScope(GetPatientFilesQuery query)
    {
        if (!TryGetScope(out var secretaryUserId, out var isAdmin)) return false;
        query.SecretaryUserId = secretaryUserId;
        query.IsAdmin = isAdmin;
        return true;
    }

    private bool TryGetScope(out Guid? secretaryUserId, out bool isAdmin)
    {
        isAdmin = User.IsInRole("Admin");
        secretaryUserId = null;

        if (isAdmin) return true;
        if (!TryGetCurrentUserId(out var userId)) return false;

        secretaryUserId = userId;
        return true;
    }

    private bool TryGetCurrentUserId(out Guid userId)
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier) ??
                    User.FindFirstValue("userId") ??
                    User.FindFirstValue("Id");
        return Guid.TryParse(claim, out userId);
    }

    private IActionResult ToResponse<T>(Result<T> result) =>
        result.IsSuccess && result.Data is not null
            ? Ok(result.Data)
            : BadRequest(Result.Failure(result.Message));
}

public sealed record CreatePatientFileRequest(
    string FirstName,
    string LastName,
    string PhoneNumber,
    string? Description);

public sealed record CreatePatientFileFromReservationRequest(long PatientId);

public sealed record UpdatePatientFileRequest(
    string FirstName,
    string LastName,
    string PhoneNumber,
    string? Description);

public sealed class PatientFileImportForm
{
    public IFormFile? File { get; set; }
}
