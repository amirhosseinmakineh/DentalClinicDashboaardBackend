using System.Data;
using DentalDashboard.ApplicationService.Contract.Secretary.Accountant.SecretarySales;
using DentalDashboard.ApplicationService.Contract.Secretary.Accountant.SecretarySales.Commands;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Domain.Secretary.Accountant.SecretarySales.Entities;
using DentalDashboard.Domain.Secretary.Accountant.SecretarySales.Enums;
using DentalDashboard.Domain.Secretary.Accountant.SecretarySales.IRepositories;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using DentalDashboard.Framwork.Domain;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.ApplicationService.Secretary.Accountant.SecretarySales.Handlers;

public sealed class ApproveSecretarySaleCommandHandler(ISecretarySalesRepository repository, IUnitOfWork unitOfWork)
    : ICommandHandler<ApproveSecretarySaleCommand>
{
    public async Task<Result> HandleAsync(ApproveSecretarySaleCommand command, CancellationToken cancellationToken = default)
    {
        if (!await SecretarySaleValidation.HasRole(repository, command.AdminUserId, "Admin", cancellationToken))
            return Result.Failure("ادمین معتبر نیست.");

        await unitOfWork.BeginTransactionAsync(cancellationToken, IsolationLevel.Serializable);
        try
        {
            var sale = await repository.Sales.Include(item => item.Service)
                .FirstOrDefaultAsync(item => item.Id == command.SaleId, cancellationToken);
            if (sale is null)
            {
                await unitOfWork.RollbackAsync(cancellationToken);
                return Result.Failure("فروش موردنظر یافت نشد.");
            }
            if (sale.Status != SecretarySaleStatus.PendingAdminApproval)
            {
                await unitOfWork.RollbackAsync(cancellationToken);
                return Result.Failure("این فروش قبلاً بررسی شده و قابل تأیید مجدد نیست.");
            }
            if (await repository.WalletTransactions.AnyAsync(item =>
                    item.SecretarySaleId == sale.Id && item.TransactionType == SecretaryWalletTransactionType.SaleReward,
                    cancellationToken))
            {
                await unitOfWork.RollbackAsync(cancellationToken);
                return Result.Failure("پاداش این فروش قبلاً به کیف پول اضافه شده است.");
            }

            var wallet = await repository.Wallets.FirstOrDefaultAsync(item => item.SecretaryUserId == sale.SecretaryUserId, cancellationToken);
            if (wallet is null)
            {
                wallet = new SecretaryWallet { SecretaryUserId = sale.SecretaryUserId, Balance = 0 };
                await repository.AddWalletAsync(wallet, cancellationToken);
            }

            sale.Status = SecretarySaleStatus.Approved;
            sale.ReviewedByAdminId = command.AdminUserId;
            sale.ReviewedAt = DateTime.UtcNow;
            sale.UpdatedAt = sale.ReviewedAt;
            wallet.Balance += sale.SecretaryReward;
            wallet.UpdatedAt = DateTime.UtcNow;
            await repository.AddWalletTransactionAsync(new SecretaryWalletTransaction
            {
                Wallet = wallet,
                SecretaryUserId = sale.SecretaryUserId,
                SecretarySaleId = sale.Id,
                Amount = sale.SecretaryReward,
                TransactionType = SecretaryWalletTransactionType.SaleReward,
                Description = $"پاداش فروش {sale.Service.Title}"
            }, cancellationToken);

            await unitOfWork.CommitAsync(cancellationToken);
            return Result.Success("فروش تأیید و پاداش آن به کیف پول منشی اضافه شد.");
        }
        catch (DbUpdateException)
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            return Result.Failure("این فروش هم‌زمان پردازش شده است؛ کیف پول دوباره شارژ نشد.");
        }
        catch
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
