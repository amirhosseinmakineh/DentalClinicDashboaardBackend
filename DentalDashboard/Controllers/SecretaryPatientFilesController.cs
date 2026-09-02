using DentalDashboard.ApplicationService.Contract.Secretary.PatientFiles;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using DentalDashboard.Framwork.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DentalDashboard.Controllers;

[ApiController]
[Route("api/secretary/patient-files")]
public sealed class SecretaryPatientFilesController(ICommandDispatcher commandDispatcher, IQueryDispatcher queryDispatcher) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] GetPatientFilesQuery query, CancellationToken cancellationToken) =>
        ToResponse(await queryDispatcher.DispatchAsync(query, cancellationToken));

    [HttpGet("eligible-patients")]
    public async Task<IActionResult> GetEligiblePatients([FromQuery] SearchPatientsEligibleForFileQuery query, CancellationToken cancellationToken) =>
        ToResponse(await queryDispatcher.DispatchAsync(query, cancellationToken));

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken) =>
        ToResponse(await queryDispatcher.DispatchAsync(new GetPatientFileByIdQuery(id), cancellationToken));

    [HttpPost]
    public async Task<IActionResult> Create(CreatePatientFileRequest request, CancellationToken cancellationToken) =>
        ToResponse(await commandDispatcher.DispatchAsync(new CreatePatientFileCommand(request.GetPatientId()), cancellationToken));

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, UpdatePatientFileRequest request, CancellationToken cancellationToken) =>
        Ok(await commandDispatcher.DispatchAsync(new UpdatePatientFileCommand(id, request.FirstName, request.LastName, request.PhoneNumber), cancellationToken));

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken) =>
        Ok(await commandDispatcher.DispatchAsync(new DeletePatientFileCommand(id), cancellationToken));

    [HttpPost("import")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> Import([FromForm] PatientFileImportForm form, CancellationToken cancellationToken)
    {
        if (form.File is null)
            return BadRequest("فایل الزامی است");
        await using var fileStream = form.File.OpenReadStream();
        var result = await commandDispatcher.DispatchAsync(new ImportPatientFilesCommand(fileStream, form.File.FileName, form.File.Length), cancellationToken);
        if (!result.IsSuccess || result.Data is null)
            return BadRequest(Result.Failure(result.Message));
        return result.Data.Success ? Ok(result.Data) : BadRequest(result.Data);
    }

    private IActionResult ToResponse<T>(Result<T> result) =>
        result.IsSuccess && result.Data is not null
            ? Ok(result.Data)
            : BadRequest(Result.Failure(result.Message));
}

public sealed class CreatePatientFileRequest
{
    public long? PatientId { get; init; }
    public long? Id { get; init; }
    public long? LeadAssignmentId { get; init; }
    public long? PatientReferenceId { get; init; }

    public long GetPatientId() =>
        PatientId ?? LeadAssignmentId ?? PatientReferenceId ?? Id ?? 0;
}

public sealed record UpdatePatientFileRequest(string FirstName, string LastName, string PhoneNumber);

public sealed class PatientFileImportForm
{
    public IFormFile? File { get; set; }
}