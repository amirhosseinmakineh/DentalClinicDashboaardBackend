using DentalDashboard.ApplicationService.Contract.Secretary.PatientFiles;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DentalDashboard.Controllers;

[ApiController]
[Authorize(Roles = "Secretary")]
[Route("api/secretary/patient-files")]
public sealed class SecretaryPatientFilesController(ICommandDispatcher commands, IQueryDispatcher queries) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] GetPatientFilesQuery query, CancellationToken ct) =>
        Ok(await queries.DispatchAsync(query, ct));

    [HttpGet("eligible-patients")]
    public async Task<IActionResult> GetEligiblePatients([FromQuery] SearchPatientsEligibleForFileQuery query, CancellationToken ct) =>
        Ok(await queries.DispatchAsync(query, ct));

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id, CancellationToken ct) =>
        Ok(await queries.DispatchAsync(new GetPatientFileByIdQuery(id), ct));

    [HttpPost]
    public async Task<IActionResult> Create(CreatePatientFileRequest request, CancellationToken ct) =>
        Ok(await commands.DispatchAsync(new CreatePatientFileCommand(request.PatientId), ct));

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, UpdatePatientFileRequest request, CancellationToken ct) =>
        Ok(await commands.DispatchAsync(new UpdatePatientFileCommand(id, request.FirstName, request.LastName, request.PhoneNumber), ct));

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id, CancellationToken ct) =>
        Ok(await commands.DispatchAsync(new DeletePatientFileCommand(id), ct));

    [HttpPost("import")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> Import([FromForm] PatientFileImportForm form, CancellationToken ct)
    {
        if (form.File is null)
            return BadRequest("فایل الزامی است");
        await using var stream = form.File.OpenReadStream();
        return Ok(await commands.DispatchAsync(new ImportPatientFilesCommand(stream, form.File.FileName, form.File.Length), ct));
    }
}

public sealed record CreatePatientFileRequest(long PatientId);
public sealed record UpdatePatientFileRequest(string FirstName, string LastName, string PhoneNumber);
public sealed class PatientFileImportForm { public IFormFile? File { get; set; } }
