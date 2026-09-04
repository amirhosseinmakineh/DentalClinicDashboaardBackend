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

public sealed class RejectSecretarySaleCommandHandler(ISecretarySalesRepository repository, IUnitOfWork unitOfWork)
    : ICommandHandler<RejectSecretarySaleCommand>
{
    public async Task<Result> HandleAsync(RejectSecretarySaleCommand command, CancellationToken cancellationToken = default)
    {
        if (!await SecretarySaleValidation.HasRole(repository, command.AdminUserId, "Admin", cancellationToken))
            return Result.Failure("ادمین معتبر نیست.");
        await unitOfWork.BeginTransactionAsync(cancellationToken, IsolationLevel.Serializable);
        try
        {
            var sale = await repository.Sales.FirstOrDefaultAsync(item => item.Id == command.SaleId, cancellationToken);
            if (sale is null)
            {
                await unitOfWork.RollbackAsync(cancellationToken);
                return Result.Failure("فروش موردنظر یافت نشد.");
            }
            if (sale.Status != SecretarySaleStatus.PendingAdminApproval)
            {
                await unitOfWork.RollbackAsync(cancellationToken);
                return Result.Failure("فقط فروش در انتظار تأیید قابل رد کردن است.");
            }

            sale.Status = SecretarySaleStatus.Rejected;
            sale.ReviewedByAdminId = command.AdminUserId;
            sale.ReviewedAt = DateTime.UtcNow;
            sale.UpdatedAt = sale.ReviewedAt;
            await unitOfWork.CommitAsync(cancellationToken);
            return Result.Success("فروش رد شد و تغییری در کیف پول ایجاد نشد.");
        }
        catch
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
