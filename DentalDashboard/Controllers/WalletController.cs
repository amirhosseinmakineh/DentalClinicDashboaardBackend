using DentalDashboard.ApplicationService.Contract.Dtos.Financial;
using DentalDashboard.ApplicationService.Contract.IServices;
using DentalDashboard.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace DentalDashboard.Controllers;

[ApiController]
[Route("api/wallet/{userId:guid}")]
public class WalletController : ControllerBase
{
    private readonly IFinancialTransactionService service;
    public WalletController(IFinancialTransactionService service) => this.service = service;

    [HttpGet]
    public Task<IActionResult> Get(Guid userId, CancellationToken cancellationToken) =>
        Execute(() => service.GetUserWalletAsync(userId, cancellationToken));

    [HttpPost("deposit")]
    public Task<IActionResult> Deposit(Guid userId, WalletTransactionRequest request, CancellationToken cancellationToken) =>
        Execute(() => service.AddWalletTransactionAsync(userId, request, WalletTransactionType.Deposit, cancellationToken));

    [HttpPost("withdraw")]
    public Task<IActionResult> Withdraw(Guid userId, WalletTransactionRequest request, CancellationToken cancellationToken) =>
        Execute(() => service.AddWalletTransactionAsync(userId, request, WalletTransactionType.Withdrawal, cancellationToken));

    private static async Task<IActionResult> Execute(Func<Task<WalletDto>> action)
    {
        try { return new OkObjectResult(await action()); }
        catch (ArgumentException ex) { return new BadRequestObjectResult(new ProblemDetails { Title = "Validation failed", Detail = ex.Message, Status = 400 }); }
        catch (KeyNotFoundException ex) { return new NotFoundObjectResult(new ProblemDetails { Title = "Resource not found", Detail = ex.Message, Status = 404 }); }
        catch (InvalidOperationException ex) { return new ConflictObjectResult(new ProblemDetails { Title = "Wallet operation rejected", Detail = ex.Message, Status = 409 }); }
    }
}
