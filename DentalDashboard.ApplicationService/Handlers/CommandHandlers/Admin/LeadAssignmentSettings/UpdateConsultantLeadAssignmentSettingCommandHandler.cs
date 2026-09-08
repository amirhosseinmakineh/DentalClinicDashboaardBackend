using DentalDashboard.ApplicationService.Contract.Requests.Admin.LeadAssignmentSettings;
using DentalDashboard.Domain.Enums;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using DentalDashboard.Framwork.Domain;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.ApplicationService.Handlers.CommandHandlers.Admin.LeadAssignmentSettings;

public sealed class UpdateConsultantLeadAssignmentSettingCommandHandler(
    IConsultantProfileRepository consultants,
    ILeadAssignmentSettingRepository globalSettings,
    IUserRepository users)
    : ICommandHandler<UpdateConsultantLeadAssignmentSettingCommand,
        ConsultantLeadAssignmentSettingResponse>
{
    public async Task<Result<ConsultantLeadAssignmentSettingResponse>> HandleAsync(
        UpdateConsultantLeadAssignmentSettingCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.PreferredLeadSourceType.HasValue &&
            !Enum.IsDefined(command.PreferredLeadSourceType.Value))
            return Result<ConsultantLeadAssignmentSettingResponse>.Failure(
                "نوع لید انتخاب‌شده معتبر نیست");

        var isAdmin = await users.GetAll().AnyAsync(user =>
            user.Id == command.AdminUserId && user.IsActive && !user.IsDeleted &&
            user.UserRoles.Any(userRole => !userRole.IsDeleted &&
                !userRole.Role.IsDeleted && userRole.Role.RoleName == "Admin"),
            cancellationToken);
        if (!isAdmin)
            return Result<ConsultantLeadAssignmentSettingResponse>.Failure("ادمین معتبر نیست");

        var profile = await consultants.GetAll()
            .Include(item => item.User)
            .FirstOrDefaultAsync(item =>
                item.Id == command.ConsultantProfileId && !item.IsDeleted,
                cancellationToken);
        if (profile is null)
            return Result<ConsultantLeadAssignmentSettingResponse>.Failure("مشاور یافت نشد");

        profile.PreferredLeadSourceType = command.PreferredLeadSourceType;
        profile.UpdatedAt = DateTime.UtcNow;
        consultants.Update(profile);
        await consultants.SaveChange();

        var global = await globalSettings.GetCurrentAsync(cancellationToken);
        var effective = profile.PreferredLeadSourceType ??
            global?.AssignmentSourceType ?? LeadAssignmentSourceType.NewLeads;

        return Result<ConsultantLeadAssignmentSettingResponse>.Success(
            new ConsultantLeadAssignmentSettingResponse
            {
                ConsultantProfileId = profile.Id,
                FullName = $"{profile.User.FirstName} {profile.User.LastName}".Trim(),
                PhoneNumber = profile.User.PhoneNumber,
                IsActive = profile.User.IsActive,
                IsOnline = profile.IsOnline,
                PreferredLeadSourceType = profile.PreferredLeadSourceType,
                EffectiveLeadSourceType = effective
            },
            "نوع لید مشاور با موفقیت ذخیره شد");
    }
}
