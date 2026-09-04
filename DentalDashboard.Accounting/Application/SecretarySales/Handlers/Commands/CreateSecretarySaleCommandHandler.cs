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

public sealed class CreateSecretarySaleCommandHandler(
    ISecretarySalesRepository repository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<CreateSecretarySaleCommand, SecretarySaleCreatedDto>
{
    public async Task<Result<SecretarySaleCreatedDto>> HandleAsync(
        CreateSecretarySaleCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.SecretaryUserId == Guid.Empty)
        {
            return Result<SecretarySaleCreatedDto>.Failure("منشی معتبر نیست.");
        }

        if (command.PatientUserId == Guid.Empty)
        {
            return Result<SecretarySaleCreatedDto>.Failure("بیمار معتبر نیست.");
        }

        if (command.ServiceId <= 0)
        {
            return Result<SecretarySaleCreatedDto>.Failure("خدمت انتخاب‌شده معتبر نیست.");
        }

        if (!await SecretarySaleValidation.HasRole(
                repository,
                command.SecretaryUserId,
                "Secretary",
                cancellationToken))
        {
            return Result<SecretarySaleCreatedDto>.Failure("کاربر جاری منشی معتبر و فعال نیست.");
        }

        if (!await SecretarySaleValidation.HasRole(
                repository,
                command.PatientUserId,
                "Patient",
                cancellationToken))
        {
            return Result<SecretarySaleCreatedDto>.Failure("بیمار انتخاب‌شده معتبر و فعال نیست.");
        }

        var service = await repository.Services
            .AsNoTracking()
            .FirstOrDefaultAsync(
                item => item.Id == command.ServiceId && item.IsActive,
                cancellationToken);

        if (service is null)
        {
            return Result<SecretarySaleCreatedDto>.Failure("خدمت انتخاب‌شده یافت نشد یا غیرفعال است.");
        }

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

        return Result<SecretarySaleCreatedDto>.Success(
            new SecretarySaleCreatedDto(sale.Id),
            "فروش شما ثبت شد و در انتظار تأیید ادمین است.");
    }
}
