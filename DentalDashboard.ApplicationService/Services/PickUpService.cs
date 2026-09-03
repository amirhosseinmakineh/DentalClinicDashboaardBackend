using DentalDashboard.ApplicationService.Contract.IServices;
using DentalDashboard.ApplicationService.Contract.Responses.LeadResponse;
using DentalDashboard.Domain.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.ApplicationService.Services;

public class PickUpService : IPickupService
{
    private readonly ILeadAssignmentRepository leadAssignmentRepository;
    private readonly IConsultantProfileRepository consultantProfileRepository;
    private readonly ILeadAssignmentLimitService leadAssignmentLimitService;
    private readonly IPushNotificationService pushNotificationService;
    private readonly IUnitOfWork unitOfWork;
    private readonly ILeadAssignmentSettingRepository leadAssignmentSettingRepository;
    private readonly Microsoft.Extensions.Logging.ILogger<PickUpService> logger;

    public PickUpService(
        ILeadAssignmentRepository leadAssignmentRepository,
        IConsultantProfileRepository consultantProfileRepository,
        ILeadAssignmentLimitService leadAssignmentLimitService,
        IPushNotificationService pushNotificationService,
        IUnitOfWork unitOfWork,
        ILeadAssignmentSettingRepository leadAssignmentSettingRepository,
        Microsoft.Extensions.Logging.ILogger<PickUpService> logger)
    {
        this.leadAssignmentRepository = leadAssignmentRepository;
        this.consultantProfileRepository = consultantProfileRepository;
        this.leadAssignmentLimitService = leadAssignmentLimitService;
        this.pushNotificationService = pushNotificationService;
        this.unitOfWork = unitOfWork;
        this.leadAssignmentSettingRepository = leadAssignmentSettingRepository;
        this.logger = logger;
    }

    public async Task<PickupLeadResult> PickupLeadAsync(
        long leadAssignmentId,
        long consultantProfileId,
        CancellationToken cancellationToken)
    {
        var sourceType = (await leadAssignmentSettingRepository.GetCurrentAsync(cancellationToken))
            ?.AssignmentSourceType ?? DentalDashboard.Domain.Enums.LeadAssignmentSourceType.NewLeads;

        if (!await leadAssignmentLimitService.CanPickupLeadAsync(consultantProfileId))
        {
            logger.LogWarning(
                "Lead assignment failed because consultant limit was reached. AssignmentSourceType: {AssignmentSourceType}, LeadId: {LeadId}, ConsultantId: {ConsultantId}",
                sourceType,
                leadAssignmentId,
                consultantProfileId);
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
            logger.LogWarning(
                "Lead assignment failed because candidate was no longer eligible. AssignmentSourceType: {AssignmentSourceType}, LeadId: {LeadId}, ConsultantId: {ConsultantId}",
                sourceType,
                leadAssignmentId,
                consultantProfileId);
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

        await unitOfWork.SaveChangesAsync();

        var lead = await leadAssignmentRepository.GetByIdAsync(leadAssignmentId);

        await NotifyRealtimeLeadTakenAsync(leadAssignmentId, consultantProfileId);

        logger.LogInformation(
            "Lead assignment succeeded. AssignmentSourceType: {AssignmentSourceType}, LeadId: {LeadId}, ConsultantId: {ConsultantId}",
            sourceType,
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

    private async Task NotifyRealtimeLeadTakenAsync(
        long leadAssignmentId,
        long pickedByConsultantProfileId)
    {
        var consultants = await consultantProfileRepository.GetAll()
            .Where(x => !x.IsDeleted && x.IsCompleteProfile)
            .ToListAsync();

        foreach (var consultant in consultants)
        {
            await pushNotificationService.SendAsync(
                consultant.UserId,
                string.Empty,
                string.Empty,
                new Dictionary<string, string>
                {
                    ["type"] = "RealtimeLeadTaken",
                    ["leadId"] = leadAssignmentId.ToString(),
                    ["pickedByConsultantId"] = pickedByConsultantProfileId.ToString(),
                    ["silent"] = "true"
                });
        }
    }
}
