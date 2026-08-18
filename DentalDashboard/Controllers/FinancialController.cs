using System.Security.Claims;
using DentalDashboard.ApplicationService.Contract.Dtos.Financial;
using DentalDashboard.ApplicationService.Contract.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DentalDashboard.Controllers;

[Authorize]
[ApiController]
[Route("api/financial/transactions")]
public class FinancialController : ControllerBase
{
    private readonly IFinancialTransactionService service;
    public FinancialController(IFinancialTransactionService service) => this.service = service;

    [HttpPost]
    public async Task<IActionResult> Create(CreateFinancialTransactionRequest request, CancellationToken cancellationToken)
    {
        var result = await service.CreateTransactionAsync(request, CurrentUserId(), cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> Get(long id, CancellationToken cancellationToken) =>
        Ok(await service.GetTransactionAsync(id, cancellationToken));

    private Guid CurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var id) ? id : throw new UnauthorizedAccessException("Authenticated user id is invalid.");
    }
}
