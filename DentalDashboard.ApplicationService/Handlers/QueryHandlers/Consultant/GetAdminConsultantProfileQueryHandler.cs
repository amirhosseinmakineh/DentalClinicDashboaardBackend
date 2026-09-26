using DentalDashboard.ApplicationService.Contract.IServices;
using DentalDashboard.ApplicationService.Contract.Requests.Consultant.Queries;
using DentalDashboard.ApplicationService.Contract.Responses.ConsultantResponse;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.ApplicationService.Handlers.QueryHandlers.Consultant;

public class GetAdminConsultantProfileQueryHandler
    : IQueryHandler<GetAdminConsultantProfileQuery, AdminConsultantProfileResponse>
{
    private readonly IConsultantProfileRepository consultantProfileRepository;
    private readonly ILeadAssignmentLimitService leadAssignmentLimitService;

    public GetAdminConsultantProfileQueryHandler(
        IConsultantProfileRepository consultantProfileRepository,
        ILeadAssignmentLimitService leadAssignmentLimitService)
    {
        this.consultantProfileRepository = consultantProfileRepository;
        this.leadAssignmentLimitService = leadAssignmentLimitService;
    }

    public async Task<AdminConsultantProfileResponse> HandleAsync(
        GetAdminConsultantProfileQuery query,
        CancellationToken cancellationToken = default)
    {
        var profile = await consultantProfileRepository.GetAll()
            .AsNoTracking()
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.Id == query.ProfileId, cancellationToken);

        if (profile == null || profile.IsDeleted)
            throw new InvalidOperationException("مشاوری یافت نشد");

        var limitStatus = await leadAssignmentLimitService
            .GetDailyLimitStatusAsync(profile.Id);

        return new AdminConsultantProfileResponse
        {
            ProfileId = profile.Id,
            UserId = profile.UserId,
            FirstName = profile.User.FirstName,
            LastName = profile.User.LastName,
            PhoneNumber = profile.User.PhoneNumber,
            UserIsActive = profile.User.IsActive,
            UserIsCompleteProfile = profile.User.IsCompleteProfile,
            NationalCode = profile.NationalCode,
            Address = profile.Address,
            IsAvailable = profile.IsAvailable,
            IsOnline = profile.IsOnline,
            IsCompleteProfile = profile.IsCompleteProfile,
            WorkStartTime = profile.WorkStartTime,
            WorkEndTime = profile.WorkEndTime,
            Notes = profile.Notes,
            LastOnlineAt = profile.LastOnlineAt,
            LastOfflineAt = profile.LastOfflineAt,
            LimitNumber = profile.LimitNumber,
            EffectiveDailyLimit = limitStatus.EffectiveDailyLimit,
            TodayPickupCount = limitStatus.TodayPickupCount
        };
    }
}
