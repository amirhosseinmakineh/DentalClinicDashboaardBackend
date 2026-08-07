using DentalDashboard.ApplicationService.Contract.IServices;
using DentalDashboard.ApplicationService.Contract.Requests.Consultant.Commands;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Domain.Strategies;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using DentalDashboard.Framwork.Domain;
using DentalDashboard.Utilities.Time;

namespace DentalDashboard.ApplicationService.Handlers.CommandHandlers.Consultant;

public sealed class DistributeSellerLeadsCommandHandler : ICommandHandler<DistributeSellerLeadsCommand>
{
    private static readonly TimeSpan RedispatchInterval = TimeSpan.FromSeconds(6);
    private readonly IConsultantProfileRepository consultants;
    private readonly ILeadAssignmentRepository leads;
    private readonly ILeadAssignmentService assignmentService;
    private readonly SellerDistributionStrategy strategy = new(SellerConsultantPolicy.Default);

    public DistributeSellerLeadsCommandHandler(IConsultantProfileRepository consultants,
        ILeadAssignmentRepository leads, ILeadAssignmentService assignmentService)
    {
        this.consultants = consultants; this.leads = leads; this.assignmentService = assignmentService;
    }

    public async Task<Result> HandleAsync(DistributeSellerLeadsCommand command,
        CancellationToken cancellationToken = default)
    {
        var seller = (await consultants.GetActiveSellerConsultantsAsync())
            .SingleOrDefault(x => x.Id == command.ConsultantId);
        if (seller == null)
            return Result.Success("Seller is no longer eligible.");
        var allocation = await leads.GetSellerDailyAllocationCountAsync(seller.Id, cancellationToken);
        var decision = strategy.Decide(new SellerConsultantContext
        {
            SellerStartedAt = IranTimeHelper.ToIranLocalTime(seller.SellerStartedAt!.Value),
            CurrentTime = IranTimeHelper.IranLocalNow,
            AssignedNewLeadToday = allocation.NewLeadCount,
            AssignedBurnedLeadToday = allocation.BurnedLeadCount,
            IsActive = seller.User.IsActive, IsAvailable = seller.IsAvailable, IsOnline = seller.IsOnline
        });
        if (!decision.CanReceiveBurnedLead)
            return Result.Success("Seller burned-lead quota is unavailable.");

        var lead = await leads.GetCurrentBurnedLeadForDispatchAsync(RedispatchInterval);
        if (lead == null)
            return Result.Success("No burned lead is ready for Seller distribution.");
        await assignmentService.BroadcastTestLeadAsync(lead, new[] { seller },
            lead.NotificationSent && lead.LastDispatchAt.HasValue, cancellationToken);
        return Result.Success("Burned lead broadcast through the existing assignment pipeline.");
    }
}
