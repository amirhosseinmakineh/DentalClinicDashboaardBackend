using DentalDashboard.ApplicationService.Contract.Requests.Admin.LeadAssignmentSettings;
using DentalDashboard.Domain.Enums;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.ApplicationService.Handlers.QueryHandlers.Admin.LeadAssignmentSettings;

public sealed class GetConsultantLeadAssignmentSettingsQueryHandler(
    IConsultantProfileRepository consultants,
    ILeadAssignmentSettingRepository globalSettings)
    : IQueryHandler<GetConsultantLeadAssignmentSettingsQuery,
        IReadOnlyList<ConsultantLeadAssignmentSettingResponse>>
{
    public async Task<IReadOnlyList<ConsultantLeadAssignmentSettingResponse>> HandleAsync(
        GetConsultantLeadAssignmentSettingsQuery query,
        CancellationToken cancellationToken = default)
    {
        var global = await globalSettings.GetCurrentAsync(cancellationToken);
        var fallback = global?.AssignmentSourceType ?? LeadAssignmentSourceType.NewLeads;

        return await consultants.GetAll()
            .AsNoTracking()
            .Where(profile => !profile.IsDeleted && !profile.User.IsDeleted)
            .OrderBy(profile => profile.User.FirstName)
            .ThenBy(profile => profile.User.LastName)
            .Select(profile => new ConsultantLeadAssignmentSettingResponse
            {
                ConsultantProfileId = profile.Id,
                FullName = (profile.User.FirstName + " " + profile.User.LastName).Trim(),
                PhoneNumber = profile.User.PhoneNumber,
                IsActive = profile.User.IsActive,
                IsOnline = profile.IsOnline,
                PreferredLeadSourceType = profile.PreferredLeadSourceType,
                EffectiveLeadSourceType = profile.PreferredLeadSourceType ?? fallback
            })
            .ToListAsync(cancellationToken);
    }
}
