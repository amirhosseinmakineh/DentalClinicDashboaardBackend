using DentalDashboard.ApplicationService.Contract.IServices;
using DentalDashboard.ApplicationService.Contract.Requests.Consultant.Queries;
using DentalDashboard.ApplicationService.Contract.Responses.ConsultantResponse;
using DentalDashboard.Domain.Enums;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using DentalDashboard.Domain.Strategies;
using DentalDashboard.Domain.Models;
using DentalDashboard.Utilities.Time;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.ApplicationService.Handlers.QueryHandlers.Consultant;

public class GetBroadcastRealtimeLeadsQueryHandler
    : IQueryHandler<GetBroadcastRealtimeLeadsQuery, BroadcastRealtimeLeadsResponse>
{
    private readonly IConsultantProfileRepository consultantProfileRepository;
    private readonly ILeadAssignmentRepository leadAssignmentRepository;
    private readonly ILeadAssignmentLimitService leadAssignmentLimitService;
    private readonly IConsultantLeadWorkloadService workloadService;

    public GetBroadcastRealtimeLeadsQueryHandler(
        IConsultantProfileRepository consultantProfileRepository,
        ILeadAssignmentRepository leadAssignmentRepository,
        ILeadAssignmentLimitService leadAssignmentLimitService,
        IConsultantLeadWorkloadService workloadService)
    {
        this.consultantProfileRepository = consultantProfileRepository;
        this.leadAssignmentRepository = leadAssignmentRepository;
        this.leadAssignmentLimitService = leadAssignmentLimitService;
        this.workloadService = workloadService;
    }

    public async Task<BroadcastRealtimeLeadsResponse> HandleAsync(
        GetBroadcastRealtimeLeadsQuery query,
        CancellationToken cancellationToken = default)
    {
        var profile = await consultantProfileRepository.GetAll().Include(x => x.User)
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

        if (!profile.User.IsActive)
        {
            return new BroadcastRealtimeLeadsResponse
            {
                CanReceive = false,
                BlockReason = "حساب کاربری مشاور فعال نیست",
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

        var workload = await workloadService.GetStatusAsync(profile.Id, cancellationToken);
        if (workload.BlocksNewLeads)
        {
            return new BroadcastRealtimeLeadsResponse
            {
                CanReceive = false,
                BlockReason = workload.BlockMessage,
            };
        }

        if (profile.ConsultantLevel != ConsultantLevel.Test &&
            !await leadAssignmentLimitService.CanPickupLeadAsync(profile.Id))
        {
            var limitStatus = await leadAssignmentLimitService
                .GetDailyLimitStatusAsync(profile.Id);

            return new BroadcastRealtimeLeadsResponse
            {
                CanReceive = false,
                BlockReason = limitStatus.DailyLimitReachedMessage,
            };
        }

        LeadAssignment? lead;
        if (profile.ConsultantLevel == ConsultantLevel.Test)
        {
            if (!profile.TestStartedAt.HasValue)
            {
                return new BroadcastRealtimeLeadsResponse
                {
                    CanReceive = false,
                    BlockReason = "دوره آزمایشی مشاور آغاز نشده است",
                };
            }

            var assignedToday = await leadAssignmentRepository.GetTodayAssignmentCountAsync(
                profile.Id, burned: true, cancellationToken);
            var decision = new TestConsultantStrategy(TestConsultantPolicy.Default).Decide(new TestConsultantContext
            {
                TestStartedAt = IranTimeHelper.ToIranLocalTime(profile.TestStartedAt.Value),
                CurrentTime = IranTimeHelper.IranLocalNow,
                AssignedTodayCount = assignedToday,
                IsActive = profile.User.IsActive,
                IsAvailable = profile.IsAvailable,
                IsOnline = profile.IsOnline
            });
            if (!decision.CanReceiveNewLead)
            {
                return new BroadcastRealtimeLeadsResponse
                {
                    CanReceive = false,
                    BlockReason = decision.IsFollowUpPhase
                        ? "در روزهای ششم تا دهم فقط پیگیری لیدهای قبلی مجاز است"
                        : "در حال حاضر امکان دریافت لید جدید برای دوره آزمایشی وجود ندارد",
                };
            }

            lead = await leadAssignmentRepository.GetCurrentBurnedLeadForDispatchAsync(TimeSpan.Zero);
        }
        else
        {
            lead = await leadAssignmentRepository.GetActiveRealtimeBroadcastLeadAsync();
        }

        var leads = lead == null
            ? Array.Empty<BroadcastRealtimeLeadItemResponse>()
            : new[]
            {
                new BroadcastRealtimeLeadItemResponse
                {
                    LeadAssignmentId = lead.Id,
                    UserName = lead.UserName,
                    PhoneNumber = lead.PhoneNumber,
                    CreatedAt = lead.CreatedAt,
                },
            };

        return new BroadcastRealtimeLeadsResponse
        {
            CanReceive = true,
            Leads = leads,
        };
    }
}
