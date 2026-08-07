using DentalDashboard.ApplicationService.Contract.Requests.Consultant.Queries;
using DentalDashboard.ApplicationService.Contract.Responses.ConsultantResponse;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Domain.Strategies;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using DentalDashboard.Utilities.Time;

namespace DentalDashboard.ApplicationService.Handlers.QueryHandlers.Consultant;

public sealed class GetActiveSellerConsultantsQueryHandler :
    IQueryHandler<GetActiveSellerConsultantsQuery, IReadOnlyList<ActiveSellerConsultantResponse>>
{
    private readonly IConsultantProfileRepository consultants;
    private readonly ILeadAssignmentRepository leads;
    private readonly SellerDistributionStrategy strategy = new(SellerConsultantPolicy.Default);

    public GetActiveSellerConsultantsQueryHandler(
        IConsultantProfileRepository consultants, ILeadAssignmentRepository leads)
    {
        this.consultants = consultants;
        this.leads = leads;
    }

    public async Task<IReadOnlyList<ActiveSellerConsultantResponse>> HandleAsync(
        GetActiveSellerConsultantsQuery query, CancellationToken cancellationToken = default)
    {
        var now = IranTimeHelper.IranLocalNow;
        var result = new List<ActiveSellerConsultantResponse>();
        foreach (var consultant in await consultants.GetActiveSellerConsultantsAsync())
        {
            var allocation = await leads.GetSellerDailyAllocationCountAsync(consultant.Id, cancellationToken);
            var started = IranTimeHelper.ToIranLocalTime(consultant.SellerStartedAt!.Value);
            var decision = strategy.Decide(new SellerConsultantContext
            {
                SellerStartedAt = started, CurrentTime = now,
                AssignedNewLeadToday = allocation.NewLeadCount,
                AssignedBurnedLeadToday = allocation.BurnedLeadCount,
                IsActive = consultant.User.IsActive, IsAvailable = consultant.IsAvailable,
                IsOnline = consultant.IsOnline
            });
            result.Add(new(consultant.Id, consultant.ConsultantLevel, started,
                decision.CurrentSellerDay, allocation.NewLeadCount, allocation.BurnedLeadCount));
        }
        return result;
    }
}
