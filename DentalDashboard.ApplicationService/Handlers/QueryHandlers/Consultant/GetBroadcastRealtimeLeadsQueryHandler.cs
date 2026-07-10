using DentalDashboard.ApplicationService.Contract.IServices;
using DentalDashboard.ApplicationService.Contract.Requests.Consultant.Queries;
using DentalDashboard.ApplicationService.Contract.Responses.ConsultantResponse;
using DentalDashboard.Domain.Enums;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.ApplicationService.Handlers.QueryHandlers.Consultant;

public class GetBroadcastRealtimeLeadsQueryHandler
    : IQueryHandler<GetBroadcastRealtimeLeadsQuery, BroadcastRealtimeLeadsResponse>
{
    private readonly IConsultantProfileRepository consultantProfileRepository;
    private readonly ILeadAssignmentRepository leadAssignmentRepository;
    private readonly ILeadAssignmentLimitService leadAssignmentLimitService;

    public GetBroadcastRealtimeLeadsQueryHandler(
        IConsultantProfileRepository consultantProfileRepository,
        ILeadAssignmentRepository leadAssignmentRepository,
        ILeadAssignmentLimitService leadAssignmentLimitService)
    {
        this.consultantProfileRepository = consultantProfileRepository;
        this.leadAssignmentRepository = leadAssignmentRepository;
        this.leadAssignmentLimitService = leadAssignmentLimitService;
    }

    public async Task<BroadcastRealtimeLeadsResponse> HandleAsync(
        GetBroadcastRealtimeLeadsQuery query,
        CancellationToken cancellationToken = default)
    {
        var profile = await consultantProfileRepository.GetAll()
            .FirstOrDefaultAsync(x => x.Id == query.ProfileId, cancellationToken);

        if (profile == null || profile.IsDeleted)
        {
            return new BroadcastRealtimeLeadsResponse
            {
                CanReceive = false,
                BlockReason = "مشاوری یافت نشد",
            };
        }

        if (!profile.IsOnline)
        {
            return new BroadcastRealtimeLeadsResponse
            {
                CanReceive = false,
                BlockReason = "برای دریافت لید لحظه‌ای باید آنلاین باشید",
            };
        }

        if (!profile.IsAvailable || !profile.IsCompleteProfile)
        {
            return new BroadcastRealtimeLeadsResponse
            {
                CanReceive = false,
                BlockReason = "پروفایل یا وضعیت حضور شما برای دریافت لید لحظه‌ای آماده نیست",
            };
        }

        if (await leadAssignmentRepository.HasActiveRealTimeLeadAsync(profile.Id))
        {
            return new BroadcastRealtimeLeadsResponse
            {
                CanReceive = false,
                BlockReason = "شما یک لید لحظه‌ای فعال دارید",
            };
        }

        if (!await leadAssignmentLimitService.CanPickupLeadAsync(profile.Id))
        {
            var limitStatus = await leadAssignmentLimitService
                .GetDailyLimitStatusAsync(profile.Id);

            return new BroadcastRealtimeLeadsResponse
            {
                CanReceive = false,
                BlockReason = limitStatus.DailyLimitReachedMessage,
            };
        }

        var leads = await leadAssignmentRepository.GetAll()
            .Where(x => !x.IsDeleted &&
                        x.AssignmentType == LeadAssignmentType.RealTime &&
                        x.ConsultantProfileId == null &&
                        x.ReportSubmittedAt == null &&
                        x.LeadAssignmentState == LeadAssignmentState.New &&
                        !x.PickUp)
            .OrderBy(x => x.CreatedAt)
            .ThenBy(x => x.Id)
            .Select(x => new BroadcastRealtimeLeadItemResponse
            {
                LeadAssignmentId = x.Id,
                UserName = x.UserName,
                CreatedAt = x.CreatedAt,
            })
            .ToListAsync(cancellationToken);

        return new BroadcastRealtimeLeadsResponse
        {
            CanReceive = true,
            Leads = leads,
        };
    }
}
