using DentalDashboard.ApplicationService.Contract.IServices;
using DentalDashboard.ApplicationService.Contract.Requests.Consultant.Commands;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using DentalDashboard.Framwork.Domain;

namespace DentalDashboard.ApplicationService.Handlers.CommandHandlers.Consultant;

public sealed class ProcessRoleBasedRealtimeLeadsCommandHandler :
    ICommandHandler<ProcessRoleBasedRealtimeLeadsCommand>
{
    private readonly ILeadAssignmentService assignmentService;

    public ProcessRoleBasedRealtimeLeadsCommandHandler(ILeadAssignmentService assignmentService) =>
        this.assignmentService = assignmentService;

    public async Task<Result> HandleAsync(ProcessRoleBasedRealtimeLeadsCommand command,
        CancellationToken cancellationToken = default)
    {
        await assignmentService.AssignRealTimeLeadsAsync();
        return Result.Success("Role-based realtime distribution cycle completed.");
    }
}
