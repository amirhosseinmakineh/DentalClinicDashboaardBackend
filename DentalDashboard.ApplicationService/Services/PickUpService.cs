using DentalDashboard.ApplicationService.Contract.IServices;
using DentalDashboard.ApplicationService.Contract.Responses.LeadResponse;
using DentalDashboard.Domain.IRepositories;

namespace DentalDashboard.ApplicationService.Services;

public class PickUpService : IPickupService
{
    private readonly ILeadAssignmentRepository leadAssignmentRepository;
    private readonly IConsultantProfileRepository consultantProfileRepository;
    private readonly ILeadAssignmentService leadAssignmentService;
    private readonly IUnitOfWork unitOfWork;

    public PickUpService(
        ILeadAssignmentRepository leadAssignmentRepository,
        IConsultantProfileRepository consultantProfileRepository,
        ILeadAssignmentService leadAssignmentService,
        IUnitOfWork unitOfWork)
    {
        this.leadAssignmentRepository = leadAssignmentRepository;
        this.consultantProfileRepository = consultantProfileRepository;
        this.leadAssignmentService = leadAssignmentService;
        this.unitOfWork = unitOfWork;
    }

    public async Task<PickupLeadResult> PickupLeadAsync(
        long leadAssignmentId,
        long consultantProfileId,
        CancellationToken cancellationToken)
    {
        var todayPickupCount = await leadAssignmentRepository
            .GetTodayPickupCountAsync(consultantProfileId);

        if (todayPickupCount >= 10)
        {
            return new PickupLeadResult
            {
                Status = PickupLeadStatus.DailyLimitReached,
                ConsultantProfileId = consultantProfileId
            };
        }

        var pickedUp = await leadAssignmentRepository
            .TryPickupLeadAsync(
                leadAssignmentId,
                consultantProfileId,
                cancellationToken);

        if (!pickedUp)
        {
            return new PickupLeadResult
            {
                Status = PickupLeadStatus.AlreadyTaken,
                LeadAssignmentId = leadAssignmentId
            };
        }

        var consultant = await consultantProfileRepository
            .GetByIdAsync(consultantProfileId);

        if (consultant != null)
        {
            consultant.IsOnline = false;
            consultant.LastOfflineAt = DateTime.UtcNow;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var lead = await leadAssignmentRepository.GetByIdAsync(leadAssignmentId);

        await leadAssignmentService.NotifyRealtimeLeadTakenAsync(
            leadAssignmentId,
            consultantProfileId);

        return new PickupLeadResult
        {
            Status = PickupLeadStatus.Success,
            LeadAssignmentId = leadAssignmentId,
            ConsultantProfileId = consultantProfileId,
            CallDeadlineAt = lead?.CallDeadlineAt
        };
    }
}
