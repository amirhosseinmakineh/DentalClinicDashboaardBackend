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

public sealed class CreateSecretarySaleServiceCommandHandler(
    ISecretarySalesRepository repository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<CreateSecretarySaleServiceCommand, SecretarySaleServiceDto>
{
    public async Task<Result<SecretarySaleServiceDto>> HandleAsync(
        CreateSecretarySaleServiceCommand command,
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

        var title = command.Title.Trim();
        if (await repository.Services.AnyAsync(item => item.Title == title, cancellationToken))
        {
            return Result<SecretarySaleServiceDto>.Failure("خدمتی با این عنوان قبلاً ثبت شده است.");
        }

        var service = new SecretarySaleService
        {
            Title = title,
            Price = command.Price,
            SecretaryReward = command.SecretaryReward,
            IsActive = command.IsActive
        };

        await repository.AddServiceAsync(service, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<SecretarySaleServiceDto>.Success(
            service.ToDto(),
            "خدمت فروش منشی با موفقیت ایجاد شد.");
    }
}
