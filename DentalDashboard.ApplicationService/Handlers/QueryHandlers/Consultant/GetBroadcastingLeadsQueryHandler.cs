using DentalDashboard.ApplicationService.Contract.IServices;
using DentalDashboard.ApplicationService.Contract.Requests.Consultant.Queries;
using DentalDashboard.ApplicationService.Contract.Responses;
using DentalDashboard.ApplicationService.Contract.Responses.LeadResponse;
using DentalDashboard.ApplicationService.Services;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;

namespace DentalDashboard.ApplicationService.Handlers.QueryHandlers.Consultant;

public class GetBroadcastingLeadsQueryHandler
    : IQueryHandler<GetBroadcastingLeadsQuery, PaginatedResult<BroadcastingLeadResponse>>
{
    private readonly ILeadAssignmentRepository leadAssignmentRepository;
    private readonly ILeadBroadcastService leadBroadcastService;
    private readonly IConsultantProfileRepository consultantProfileRepository;
    private readonly LeadBroadcastTestFilter broadcastTestFilter;

    public GetBroadcastingLeadsQueryHandler(
        ILeadAssignmentRepository leadAssignmentRepository,
        ILeadBroadcastService leadBroadcastService,
        IConsultantProfileRepository consultantProfileRepository,
        LeadBroadcastTestFilter broadcastTestFilter)
    {
        this.leadAssignmentRepository = leadAssignmentRepository;
        this.leadBroadcastService = leadBroadcastService;
        this.consultantProfileRepository = consultantProfileRepository;
        this.broadcastTestFilter = broadcastTestFilter;
    }

    public async Task<PaginatedResult<BroadcastingLeadResponse>> HandleAsync(
        GetBroadcastingLeadsQuery query,
        CancellationToken cancellationToken = default)
    {
        var profile = await consultantProfileRepository.GetByIdAsync(query.ProfileId);

        await leadBroadcastService.ExpireStaleBroadcastsAsync(cancellationToken);

        var leads = await leadAssignmentRepository.GetBroadcastingLeadsAsync(query.ProfileId);
        var items = leads
            .Where(x => profile is null || broadcastTestFilter.CanAccessLead(profile.UserId, x))
            .Select(x =>
            {
                var (firstName, lastName) = LeadBroadcastService.SplitUserName(x.UserName);
                return new BroadcastingLeadResponse
                {
                    LeadAssignmentId = x.Id,
                    FirstName = LeadBroadcastService.MaskName(firstName),
                    LastName = LeadBroadcastService.MaskName(lastName),
                    CreatedAt = x.CreatedAt,
                    BroadcastStartedAt = x.BroadcastStartedAt,
                    LeadAssignmentType = (int)x.AssignmentType,
                };
            }).ToList();

        return new PaginatedResult<BroadcastingLeadResponse>
        {
            Items = items,
            TotalCount = items.Count,
            PageNumber = 1,
            PageSize = Math.Max(items.Count, 1),
        };
    }
}
