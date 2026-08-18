using DentalDashboard.ApplicationService.Contract.Dtos.Financial;
using DentalDashboard.Domain.Enums;

namespace DentalDashboard.ApplicationService.Contract.IServices;

public interface IFinancialTransactionService
{
    Task<FinancialTransactionDto> CreateTransactionAsync(CreateFinancialTransactionRequest request, CancellationToken cancellationToken = default);
    Task<FinancialTransactionDto?> GetTransactionAsync(long id, CancellationToken cancellationToken = default);
    Task<WalletDto> GetUserWalletAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<WalletDto> AddWalletTransactionAsync(Guid userId, WalletTransactionRequest request,
        WalletTransactionType type, CancellationToken cancellationToken = default);
}
