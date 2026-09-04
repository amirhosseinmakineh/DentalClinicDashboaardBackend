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

public sealed class SetSecretarySaleServiceStatusCommandHandler(
    ISecretarySalesRepository repository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<SetSecretarySaleServiceStatusCommand>
{
    public async Task<Result> HandleAsync(
        SetSecretarySaleServiceStatusCommand command,
        CancellationToken cancellationToken = default)
    {
        var service = await repository.Services.FirstOrDefaultAsync(
            item => item.Id == command.Id,
            cancellationToken);

        if (service is null)
        {
            return Result.Failure("خدمت فروش منشی یافت نشد.");
        }

        service.IsActive = command.IsActive;
        service.UpdatedAt = DateTime.UtcNow;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(command.IsActive ? "خدمت فعال شد." : "خدمت غیرفعال شد.");
    }
}
