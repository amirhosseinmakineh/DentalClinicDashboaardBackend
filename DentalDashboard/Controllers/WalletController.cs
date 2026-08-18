using System.Security.Claims;
using DentalDashboard.ApplicationService.Contract.Dtos.Financial;
using DentalDashboard.ApplicationService.Contract.IServices;
using DentalDashboard.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DentalDashboard.Controllers;

[Authorize]
[ApiController]
[Route("api/wallet/{userId:guid}")]
public class WalletController : ControllerBase
{
    private readonly IFinancialTransactionService service;
    public WalletController(IFinancialTransactionService service) => this.service = service;

    [HttpGet]
    public async Task<IActionResult> Get(Guid userId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        Ok(await service.GetUserWalletAsync(userId, page, pageSize, cancellationToken));

    [Authorize(Roles = "Admin")]
    [HttpPost("deposit")]
    public async Task<IActionResult> Deposit(Guid userId, WalletTransactionRequest request, CancellationToken cancellationToken) =>
        Ok(await service.AddWalletTransactionAsync(userId, request, WalletTransactionType.Deposit, CurrentUserId(), cancellationToken));

    [Authorize(Roles = "Admin")]
    [HttpPost("withdraw")]
    public async Task<IActionResult> Withdraw(Guid userId, WalletTransactionRequest request, CancellationToken cancellationToken) =>
        Ok(await service.AddWalletTransactionAsync(userId, request, WalletTransactionType.Withdrawal, CurrentUserId(), cancellationToken));

    private Guid CurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var id) ? id : throw new UnauthorizedAccessException("Authenticated user id is invalid.");
    }
}
