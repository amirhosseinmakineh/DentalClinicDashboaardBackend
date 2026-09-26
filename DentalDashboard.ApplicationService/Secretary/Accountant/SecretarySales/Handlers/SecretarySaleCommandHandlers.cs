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

public sealed class CreateSecretarySaleServiceCommandHandler(ISecretarySalesRepository repository, IUnitOfWork unitOfWork)
    : ICommandHandler<CreateSecretarySaleServiceCommand, SecretarySaleServiceDto>
{
    public async Task<Result<SecretarySaleServiceDto>> HandleAsync(CreateSecretarySaleServiceCommand command, CancellationToken cancellationToken = default)
    {
        var validation = SecretarySaleValidation.ValidateService(command.Title, command.Price, command.SecretaryReward);
        if (validation is not null) return Result<SecretarySaleServiceDto>.Failure(validation);

        var title = command.Title.Trim();
        if (await repository.Services.AnyAsync(x => x.Title == title, cancellationToken))
            return Result<SecretarySaleServiceDto>.Failure("خدمتی با این عنوان قبلاً ثبت شده است.");

        var entity = new SecretarySaleService
        {
            Title = title,
            Price = command.Price,
            SecretaryReward = command.SecretaryReward,
            IsActive = command.IsActive
        };
        await repository.AddServiceAsync(entity, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<SecretarySaleServiceDto>.Success(entity.ToDto(), "خدمت فروش منشی با موفقیت ایجاد شد.");
    }
}

public sealed class UpdateSecretarySaleServiceCommandHandler(ISecretarySalesRepository repository, IUnitOfWork unitOfWork)
    : ICommandHandler<UpdateSecretarySaleServiceCommand, SecretarySaleServiceDto>
{
    public async Task<Result<SecretarySaleServiceDto>> HandleAsync(UpdateSecretarySaleServiceCommand command, CancellationToken cancellationToken = default)
    {
        var validation = SecretarySaleValidation.ValidateService(command.Title, command.Price, command.SecretaryReward);
        if (validation is not null) return Result<SecretarySaleServiceDto>.Failure(validation);

        var entity = await repository.Services.FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken);
        if (entity is null) return Result<SecretarySaleServiceDto>.Failure("خدمت فروش منشی یافت نشد.");
        var title = command.Title.Trim();
        if (await repository.Services.AnyAsync(x => x.Id != command.Id && x.Title == title, cancellationToken))
            return Result<SecretarySaleServiceDto>.Failure("خدمتی با این عنوان قبلاً ثبت شده است.");

        entity.Title = title;
        entity.Price = command.Price;
        entity.SecretaryReward = command.SecretaryReward;
        entity.IsActive = command.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<SecretarySaleServiceDto>.Success(entity.ToDto(), "خدمت فروش منشی با موفقیت ویرایش شد.");
    }
}

public sealed class SetSecretarySaleServiceStatusCommandHandler(ISecretarySalesRepository repository, IUnitOfWork unitOfWork)
    : ICommandHandler<SetSecretarySaleServiceStatusCommand>
{
    public async Task<Result> HandleAsync(SetSecretarySaleServiceStatusCommand command, CancellationToken cancellationToken = default)
    {
        var entity = await repository.Services.FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken);
        if (entity is null) return Result.Failure("خدمت فروش منشی یافت نشد.");
        entity.IsActive = command.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(command.IsActive ? "خدمت فعال شد." : "خدمت غیرفعال شد.");
    }
}

public sealed class CreateSecretarySaleCommandHandler(ISecretarySalesRepository repository, IUnitOfWork unitOfWork)
    : ICommandHandler<CreateSecretarySaleCommand, SecretarySaleCreatedDto>
{
    public async Task<Result<SecretarySaleCreatedDto>> HandleAsync(CreateSecretarySaleCommand command, CancellationToken cancellationToken = default)
    {
        if (command.SecretaryUserId == Guid.Empty)
            return Result<SecretarySaleCreatedDto>.Failure("منشی معتبر نیست.");
        if (command.PatientUserId == Guid.Empty)
            return Result<SecretarySaleCreatedDto>.Failure("بیمار معتبر نیست.");
        if (command.ServiceId <= 0)
            return Result<SecretarySaleCreatedDto>.Failure("خدمت انتخاب‌شده معتبر نیست.");

        if (!await SecretarySaleValidation.HasRole(repository, command.SecretaryUserId, "Secretary", cancellationToken))
            return Result<SecretarySaleCreatedDto>.Failure("کاربر جاری منشی معتبر و فعال نیست.");
        if (!await SecretarySaleValidation.HasRole(repository, command.PatientUserId, "Patient", cancellationToken))
            return Result<SecretarySaleCreatedDto>.Failure("بیمار انتخاب‌شده معتبر و فعال نیست.");

        var service = await repository.Services.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == command.ServiceId && x.IsActive, cancellationToken);
        if (service is null)
            return Result<SecretarySaleCreatedDto>.Failure("خدمت انتخاب‌شده یافت نشد یا غیرفعال است.");

        var sale = new SecretarySale
        {
            SecretaryUserId = command.SecretaryUserId,
            PatientUserId = command.PatientUserId,
            ServiceId = service.Id,
            SalePrice = service.Price,
            SecretaryReward = service.SecretaryReward,
            Status = SecretarySaleStatus.PendingAdminApproval
        };
        await repository.AddSaleAsync(sale, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<SecretarySaleCreatedDto>.Success(new SecretarySaleCreatedDto(sale.Id), "فروش شما ثبت شد و در انتظار تأیید ادمین است.");
    }
}

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
            var sale = await repository.Sales.Include(x => x.Service)
                .FirstOrDefaultAsync(x => x.Id == command.SaleId, cancellationToken);
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
            if (await repository.WalletTransactions.AnyAsync(x =>
                    x.SecretarySaleId == sale.Id && x.TransactionType == SecretaryWalletTransactionType.SaleReward,
                    cancellationToken))
            {
                await unitOfWork.RollbackAsync(cancellationToken);
                return Result.Failure("پاداش این فروش قبلاً به کیف پول اضافه شده است.");
            }

            var wallet = await repository.Wallets.FirstOrDefaultAsync(x => x.SecretaryUserId == sale.SecretaryUserId, cancellationToken);
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
            var sale = await repository.Sales.FirstOrDefaultAsync(x => x.Id == command.SaleId, cancellationToken);
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

internal static class SecretarySaleValidation
{
    public static string? ValidateService(string title, decimal price, decimal reward)
    {
        if (string.IsNullOrWhiteSpace(title)) return "عنوان خدمت الزامی است.";
        if (title.Trim().Length > 150) return "عنوان خدمت نباید بیشتر از ۱۵۰ کاراکتر باشد.";
        if (price <= 0) return "قیمت خدمت باید بیشتر از صفر باشد.";
        if (reward <= 0) return "پاداش منشی باید بیشتر از صفر باشد.";
        return null;
    }

    public static Task<bool> HasRole(ISecretarySalesRepository repository, Guid userId, string roleName, CancellationToken cancellationToken) =>
        repository.Users.AsNoTracking().AnyAsync(user =>
            user.Id == userId && user.IsActive && !user.IsDeleted &&
            user.UserRoles.Any(userRole => !userRole.IsDeleted && !userRole.Role.IsDeleted && userRole.Role.RoleName == roleName),
            cancellationToken);

    public static SecretarySaleServiceDto ToDto(this SecretarySaleService entity) =>
        new(entity.Id, entity.Title, entity.Price, entity.SecretaryReward, entity.IsActive, entity.CreatedAt, entity.UpdatedAt);
}
