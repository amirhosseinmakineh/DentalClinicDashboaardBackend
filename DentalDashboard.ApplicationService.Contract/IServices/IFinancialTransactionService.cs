using DentalDashboard.ApplicationService.Contract.Dtos.Financial;
using DentalDashboard.Domain.Enums;

namespace DentalDashboard.ApplicationService.Contract.IServices;

public interface IFinancialTransactionService
{
    Task<FinancialTransactionDto> CreateTransactionAsync(CreateFinancialTransactionRequest request, Guid createdByUserId,
        CancellationToken cancellationToken = default);
    Task<FinancialTransactionDto> GetTransactionAsync(long id, CancellationToken cancellationToken = default);
    Task<WalletDto> GetUserWalletAsync(Guid userId, int page = 1, int pageSize = 20,
        CancellationToken cancellationToken = default);
    Task<WalletDto> AddWalletTransactionAsync(Guid userId, WalletTransactionRequest request,
        WalletTransactionType type, Guid performedByUserId, CancellationToken cancellationToken = default);
}
