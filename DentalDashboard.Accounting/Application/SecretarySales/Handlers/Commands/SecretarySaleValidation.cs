using DentalDashboard.ApplicationService.Contract.Secretary.Accountant.SecretarySales;
using DentalDashboard.Domain.Secretary.Accountant.SecretarySales.Entities;
using DentalDashboard.Domain.Secretary.Accountant.SecretarySales.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.ApplicationService.Secretary.Accountant.SecretarySales.Handlers;

internal static class SecretarySaleValidation
{
    public static string? ValidateService(string title, decimal price, decimal reward)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return "عنوان خدمت الزامی است.";
        }

        if (title.Trim().Length > 150)
        {
            return "عنوان خدمت نباید بیشتر از ۱۵۰ کاراکتر باشد.";
        }

        if (price <= 0)
        {
            return "قیمت خدمت باید بیشتر از صفر باشد.";
        }

        if (reward <= 0)
        {
            return "پاداش منشی باید بیشتر از صفر باشد.";
        }

        return null;
    }

    public static Task<bool> HasRole(
        ISecretarySalesRepository repository,
        Guid userId,
        string roleName,
        CancellationToken cancellationToken)
    {
        return repository.Users
            .AsNoTracking()
            .AnyAsync(user =>
                user.Id == userId &&
                user.IsActive &&
                !user.IsDeleted &&
                user.UserRoles.Any(userRole =>
                    !userRole.IsDeleted &&
                    !userRole.Role.IsDeleted &&
                    userRole.Role.RoleName == roleName),
                cancellationToken);
    }

    public static SecretarySaleServiceDto ToDto(this SecretarySaleService service)
    {
        return new SecretarySaleServiceDto(
            service.Id,
            service.Title,
            service.Price,
            service.SecretaryReward,
            service.IsActive,
            service.CreatedAt,
            service.UpdatedAt);
    }
}
