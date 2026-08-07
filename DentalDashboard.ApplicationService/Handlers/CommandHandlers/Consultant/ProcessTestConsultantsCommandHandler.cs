using DentalDashboard.ApplicationService.Contract.IServices;
using DentalDashboard.ApplicationService.Contract.Requests.Consultant.Commands;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Domain.IDomainService;
using DentalDashboard.Domain.Models;
using DentalDashboard.Domain.Strategies;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using DentalDashboard.Framwork.Domain;
using DentalDashboard.Utilities.Time;

namespace DentalDashboard.ApplicationService.Handlers.CommandHandlers.Consultant;

public sealed class ProcessTestConsultantsCommandHandler : ICommandHandler<ProcessTestConsultantsCommand>
{
    private static readonly TimeSpan RedispatchInterval = TimeSpan.FromSeconds(6);
    private readonly IConsultantProfileRepository consultantRepository;
    private readonly ILeadAssignmentRepository leadRepository;
    private readonly ILeadAssignmentService leadService;
    private readonly ILeadDomainService leadDomainService;
    private readonly TestConsultantStrategy strategy = new(TestConsultantPolicy.Default);

    public ProcessTestConsultantsCommandHandler(
        IConsultantProfileRepository consultantRepository,
        ILeadAssignmentRepository leadRepository,
        ILeadAssignmentService leadService,
        ILeadDomainService leadDomainService)
    {
        this.consultantRepository = consultantRepository;
        this.leadRepository = leadRepository;
        this.leadService = leadService;
        this.leadDomainService = leadDomainService;
    }

    public async Task<Result> HandleAsync(
        ProcessTestConsultantsCommand command,
        CancellationToken cancellationToken = default)
    {
        if (!leadDomainService.IsWorkingTime(DateTime.UtcNow))
            return Result.Success("TEST distribution skipped outside working hours.");

        var now = IranTimeHelper.IranLocalNow;
        var consultants = await consultantRepository.GetTestConsultantsReadyForDistributionAsync();
        var eligible = new List<ConsultantProfile>();

        foreach (var consultant in consultants)
        {
            var assignedToday = await leadRepository.GetTodayAssignmentCountAsync(consultant.Id, burned: true, cancellationToken);
            var decision = strategy.Decide(new TestConsultantContext
            {
                TestStartedAt = IranTimeHelper.ToIranLocalTime(consultant.TestStartedAt!.Value),
                CurrentTime = now,
                AssignedTodayCount = assignedToday,
                IsActive = consultant.User.IsActive,
                IsAvailable = consultant.IsAvailable,
                IsOnline = consultant.IsOnline
            });

            if (decision.CanReceiveNewLead)
                eligible.Add(consultant);
        }

        if (eligible.Count == 0)
            return Result.Success("No TEST consultant is currently eligible for distribution.");

        var lead = await leadRepository.GetCurrentBurnedLeadForDispatchAsync(RedispatchInterval);
        if (lead == null)
            return Result.Success("No burned lead is ready for TEST distribution.");

        var reminder = lead.NotificationSent && lead.LastDispatchAt.HasValue;
        await leadService.BroadcastBurnedLeadAsync(lead, eligible, "TEST", reminder, cancellationToken);
        return Result.Success("Burned lead broadcast to eligible TEST consultants.");
    }
}
