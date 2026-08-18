using DentalDashboard.ApplicationService.Contract.Dtos.Financial;
using DentalDashboard.ApplicationService.Contract.IServices;
using Microsoft.AspNetCore.Mvc;

namespace DentalDashboard.Controllers;

[ApiController]
[Route("api/financial/transactions")]
public class FinancialController : ControllerBase
{
    private readonly IFinancialTransactionService service;
    public FinancialController(IFinancialTransactionService service) => this.service = service;

    [HttpPost]
    public async Task<IActionResult> Create(CreateFinancialTransactionRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await service.CreateTransactionAsync(request, cancellationToken);
            return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
        }
        catch (ArgumentException ex) { return BadRequest(new ProblemDetails { Title = "Validation failed", Detail = ex.Message, Status = 400 }); }
        catch (KeyNotFoundException ex) { return NotFound(new ProblemDetails { Title = "Resource not found", Detail = ex.Message, Status = 404 }); }
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> Get(long id, CancellationToken cancellationToken)
    {
        try
        {
            var result = await service.GetTransactionAsync(id, cancellationToken);
            return result is null ? NotFound(new ProblemDetails { Title = "Transaction not found", Status = 404 }) : Ok(result);
        }
        catch (ArgumentException ex) { return BadRequest(new ProblemDetails { Title = "Validation failed", Detail = ex.Message, Status = 400 }); }
    }
}
