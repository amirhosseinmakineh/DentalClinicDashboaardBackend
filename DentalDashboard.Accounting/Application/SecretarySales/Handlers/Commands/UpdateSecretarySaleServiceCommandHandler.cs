using System.Data;
using DentalDashboard.Accounting.Contracts.SecretarySales;
using DentalDashboard.Accounting.Contracts.SecretarySales.Commands;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Accounting.Domain.SecretarySales.Entities;
using DentalDashboard.Accounting.Domain.SecretarySales.Enums;
using DentalDashboard.Accounting.Domain.SecretarySales.IRepositories;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using DentalDashboard.Framwork.Domain;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.Accounting.Application.SecretarySales.Handlers;

public sealed class UpdateSecretarySaleServiceCommandHandler(
    ISecretarySalesRepository repository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<UpdateSecretarySaleServiceCommand, SecretarySaleServiceDto>
{
    public async Task<Result<SecretarySaleServiceDto>> HandleAsync(
        UpdateSecretarySaleServiceCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationMessage = SecretarySaleValidation.ValidateService(
            command.Title,
            command.Price,
            command.SecretaryReward);

        if (validationMessage is not null)
        {
            return Result<SecretarySaleServiceDto>.Failure(validationMessage);
        }

        var service = await repository.Services.FirstOrDefaultAsync(
            item => item.Id == command.Id,
            cancellationToken);

        if (service is null)
        {
            return Result<SecretarySaleServiceDto>.Failure("خدمت فروش منشی یافت نشد.");
        }

        var title = command.Title.Trim();
        if (await repository.Services.AnyAsync(
                item => item.Id != command.Id && item.Title == title,
                cancellationToken))
        {
            return Result<SecretarySaleServiceDto>.Failure("خدمتی با این عنوان قبلاً ثبت شده است.");
        }

        service.Title = title;
        service.Price = command.Price;
        service.SecretaryReward = command.SecretaryReward;
        service.IsActive = command.IsActive;
        service.UpdatedAt = DateTime.UtcNow;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<SecretarySaleServiceDto>.Success(
            service.ToDto(),
            "خدمت فروش منشی با موفقیت ویرایش شد.");
    }
}
