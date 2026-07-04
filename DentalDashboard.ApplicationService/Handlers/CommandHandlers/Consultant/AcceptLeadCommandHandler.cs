using DentalDashboard.ApplicationService.Contract.IServices;
using DentalDashboard.ApplicationService.Contract.Requests.Consultant.Commands;
using DentalDashboard.ApplicationService.Contract.Responses.LeadResponse;
using DentalDashboard.ApplicationService.Hubs;
using DentalDashboard.ApplicationService.Services;
using DentalDashboard.Domain.Enums;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Domain.Models;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using DentalDashboard.Framwork.Domain;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.ApplicationService.Handlers.CommandHandlers.Consultant;

public class AcceptLeadCommandHandler : ICommandHandler<AcceptLeadCommand, AcceptLeadResponse>
{
    private readonly ILeadAssignmentRepository leadAssignmentRepository;
    private readonly IConsultantProfileRepository consultantProfileRepository;
    private readonly ILeadBroadcastService leadBroadcastService;
    private readonly IHubContext<LeadBroadcastHub> hub;

    public AcceptLeadCommandHandler(
        ILeadAssignmentRepository leadAssignmentRepository,
        IConsultantProfileRepository consultantProfileRepository,
        ILeadBroadcastService leadBroadcastService,
        IHubContext<LeadBroadcastHub> hub)
    {
        this.leadAssignmentRepository = leadAssignmentRepository;
        this.consultantProfileRepository = consultantProfileRepository;
        this.leadBroadcastService = leadBroadcastService;
        this.hub = hub;
    }

    public async Task<Result<AcceptLeadResponse>> HandleAsync(
        AcceptLeadCommand command,
        CancellationToken cancellationToken = default)
    {
        await leadBroadcastService.ExpireStaleBroadcastsAsync(cancellationToken);

        var assignment = await leadAssignmentRepository.GetAll()
            .FirstOrDefaultAsync(
                x => !x.IsDeleted && x.Id == command.LeadAssignmentId,
                cancellationToken);

        if (assignment is null)
            return Result<AcceptLeadResponse>.Failure("لید یافت نشد");

        if (assignment.LeadAssignmentState != LeadAssignmentState.Broadcasting)
            return Result<AcceptLeadResponse>.Failure("این لید قبلاً توسط مشاور دیگری برداشته شده یا دیگر در دسترس نیست");

        var consultant = await consultantProfileRepository.GetAll()
            .FirstOrDefaultAsync(x => !x.IsDeleted && x.Id == command.ConsultantProfileId, cancellationToken);

        if (consultant is null)
            return Result<AcceptLeadResponse>.Failure("مشاوری یافت نشد");

        if (!consultant.IsOnline || !consultant.IsAvailable)
            return Result<AcceptLeadResponse>.Failure("برای پذیرش لید باید آنلاین و حاضر باشید");

        if (!LeadBroadcastTestConsultants.IsAllowed(consultant.UserId))
            return Result<AcceptLeadResponse>.Failure("این لید فقط برای مشاوران تست در دسترس است");

        var now = DateTime.Now;
        assignment.ConsultantProfileId = consultant.Id;
        assignment.LeadAssignmentState = LeadAssignmentState.Claimed;
        assignment.ClaimedAt = now;
        assignment.AssignedAt = now;
        assignment.RequiresThreeMinuteCall = false;
        assignment.CallDeadlineAt = null;

        leadAssignmentRepository.Update(assignment);
        await leadAssignmentRepository.SaveChange();

        await hub.Clients
            .Group(LeadBroadcastHub.OnlineConsultantsGroup)
            .SendAsync(
                LeadBroadcastHubEvents.LeadClaimed,
                new LeadClaimedMessage
                {
                    LeadAssignmentId = assignment.Id,
                    ClaimedByConsultantProfileId = consultant.Id,
                },
                cancellationToken);

        var (firstName, lastName) = LeadBroadcastService.SplitUserName(assignment.UserName);
        return Result<AcceptLeadResponse>.Success(
            new AcceptLeadResponse
            {
                LeadAssignmentId = assignment.Id,
                ConsultantProfileId = consultant.Id,
                LeadAssignmentState = (int)assignment.LeadAssignmentState,
                PhoneNumber = assignment.PhoneNumber,
                FirstName = firstName,
                LastName = lastName,
                FullName = assignment.UserName.Trim(),
            },
            "لید با موفقیت برداشته شد");
    }
}
