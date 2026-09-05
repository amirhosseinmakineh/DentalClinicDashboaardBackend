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
