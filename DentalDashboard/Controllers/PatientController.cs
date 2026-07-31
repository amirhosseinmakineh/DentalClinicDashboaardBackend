using DentalDashboard.Framwork.Domain;
using DentalDashboard.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DentalDashboard.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class PatientController(SecretaryDashboardService service) : ControllerBase
{
    [HttpGet("{patientUserId:guid}/secretary-view")]
    [Authorize(Roles = "Secretary,Admin")]
    public async Task<IActionResult> SecretaryView(Guid patientUserId, CancellationToken ct)
    {
        try { return Ok(Result<PatientSecretaryViewDto>.Success(await service.PatientAsync(patientUserId, ct), "اطلاعات بیمار دریافت شد")); }
        catch (KeyNotFoundException ex) { return NotFound(Result.Failure(ex.Message)); }
    }
}
