using DentalDashboard.ApplicationService.Contract.IServices;
using DentalDashboard.ApplicationService.Contract.Requests.Consultant.Commands;
using DentalDashboard.Domain.Models;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using DentalDashboard.Framwork.Domain;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.ApplicationService.Handlers.CommandHandlers.Consultant;

public class RejectBroadcastCommandHandler : ICommandHandler<RejectBroadcastCommand>
{
    private readonly ILeadAssignmentRepository leadAssignmentRepository;
    private readonly ILeadBroadcastService leadBroadcastService;

    public RejectBroadcastCommandHandler(
        ILeadAssignmentRepository leadAssignmentRepository,
        ILeadBroadcastService leadBroadcastService)
    {
        this.leadAssignmentRepository = leadAssignmentRepository;
        this.leadBroadcastService = leadBroadcastService;
    }

    public async Task<Result> HandleAsync(
        RejectBroadcastCommand command,
        CancellationToken cancellationToken = default)
    {
        await leadBroadcastService.ExpireStaleBroadcastsAsync(cancellationToken);

        var exists = await leadAssignmentRepository.GetAll()
            .AsNoTracking()
            .AnyAsync(
                x => !x.IsDeleted &&
                     x.Id == command.LeadAssignmentId &&
                     x.LeadAssignmentState == LeadAssignmentState.Broadcasting,
                cancellationToken);

        if (!exists)
            return Result.Failure("لید در حال پخش یافت نشد");

        var alreadyDismissed = await leadAssignmentRepository.IsBroadcastDismissedAsync(
            command.LeadAssignmentId,
            command.ConsultantProfileId);

        if (!alreadyDismissed)
        {
            await leadAssignmentRepository.AddBroadcastDismissalAsync(
                command.LeadAssignmentId,
                command.ConsultantProfileId);
            await leadAssignmentRepository.SaveChange();
        }

        return Result.Success("لید رد شد");
    }
}
