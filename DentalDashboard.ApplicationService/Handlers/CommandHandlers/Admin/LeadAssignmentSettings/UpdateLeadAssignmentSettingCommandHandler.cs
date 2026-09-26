using DentalDashboard.ApplicationService.Contract.Requests.Admin.LeadAssignmentSettings;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Domain.Models;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using DentalDashboard.Framwork.Domain;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.ApplicationService.Handlers.CommandHandlers.Admin.LeadAssignmentSettings;

public sealed class UpdateLeadAssignmentSettingCommandHandler(
    ILeadAssignmentSettingRepository settings,
    IUserRepository users)
    : ICommandHandler<UpdateLeadAssignmentSettingCommand, LeadAssignmentSettingResponse>
{
    public async Task<Result<LeadAssignmentSettingResponse>> HandleAsync(
        UpdateLeadAssignmentSettingCommand command,
        CancellationToken cancellationToken = default)
    {
        if (!Enum.IsDefined(command.AssignmentSourceType))
            return Result<LeadAssignmentSettingResponse>.Failure("نوع لید قابل تخصیص معتبر نیست");

        var isAdmin = await users.GetAll()
            .AnyAsync(user => user.Id == command.AdminUserId && user.IsActive && !user.IsDeleted &&
                user.UserRoles.Any(userRole => !userRole.IsDeleted &&
                    !userRole.Role.IsDeleted && userRole.Role.RoleName == "Admin"), cancellationToken);
        if (!isAdmin)
            return Result<LeadAssignmentSettingResponse>.Failure("ادمین معتبر نیست");

        var setting = await settings.GetCurrentAsync(cancellationToken);
        var now = DateTime.UtcNow;
        if (setting == null)
        {
            setting = new LeadAssignmentSetting
            {
                Id = LeadAssignmentSetting.SingletonId,
                AssignmentSourceType = command.AssignmentSourceType,
                UpdatedByAdminId = command.AdminUserId,
                CreatedAt = now,
                UpdatedAt = now
            };
            await settings.AddAsync(setting);
        }
        else
        {
            setting.AssignmentSourceType = command.AssignmentSourceType;
            setting.UpdatedByAdminId = command.AdminUserId;
            setting.UpdatedAt = now;
            setting.IsDeleted = false;
            setting.DeletedAt = null;
            settings.Update(setting);
        }

        await settings.SaveChange();

        return Result<LeadAssignmentSettingResponse>.Success(
            new LeadAssignmentSettingResponse
            {
                AssignmentSourceType = setting.AssignmentSourceType,
                UpdatedAt = setting.UpdatedAt
            },
            "تنظیمات تخصیص لید با موفقیت ذخیره شد");
    }
}
