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
    private readonly IConsultantLeadWorkloadService workloadService;

    public PickUpService(
        ILeadAssignmentRepository leadAssignmentRepository,
        IConsultantProfileRepository consultantProfileRepository,
        ILeadAssignmentLimitService leadAssignmentLimitService,
        IPushNotificationService pushNotificationService,
        IUnitOfWork unitOfWork,
        IConsultantLeadWorkloadService workloadService)
    {
        this.leadAssignmentRepository = leadAssignmentRepository;
        this.consultantProfileRepository = consultantProfileRepository;
        this.leadAssignmentLimitService = leadAssignmentLimitService;
        this.pushNotificationService = pushNotificationService;
        this.unitOfWork = unitOfWork;
        this.workloadService = workloadService;
    }

    public async Task<PickupLeadResult> PickupLeadAsync(
        long leadAssignmentId,
        long consultantProfileId,
        CancellationToken cancellationToken)
    {
        var consultant = await consultantProfileRepository.GetAll()
            .FirstOrDefaultAsync(
                x => x.Id == consultantProfileId && !x.IsDeleted,
                cancellationToken);

        if (consultant == null ||
            !consultant.IsCompleteProfile ||
            !consultant.IsAvailable ||
            !consultant.IsOnline)
        {
            return new PickupLeadResult
            {
                Status = PickupLeadStatus.WorkloadBlocked,
                ConsultantProfileId = consultantProfileId,
                Message = "برای دریافت لید باید حاضر و آنلاین باشید"
            };
        }

        var workload = await workloadService.GetStatusAsync(consultantProfileId, cancellationToken);
        if (workload.BlocksNewLeads)
        {
            return new PickupLeadResult
            {
                Status = PickupLeadStatus.WorkloadBlocked,
                ConsultantProfileId = consultantProfileId,
                Message = workload.BlockMessage
            };
        }

        if (await leadAssignmentRepository.HasUnreportedLeadAsync(
                consultantProfileId, cancellationToken))
        {
            return new PickupLeadResult
            {
                Status = PickupLeadStatus.WorkloadBlocked,
                ConsultantProfileId = consultantProfileId,
                Message = "ابتدا گزارش شماره قبلی را ثبت کنید"
            };
        }

        var requestedLead = await leadAssignmentRepository.GetByIdAsync(leadAssignmentId);
        if (requestedLead == null ||
            !await leadAssignmentLimitService.CanPickupLeadAsync(consultantProfileId, requestedLead.IsDeleted))
        {
            return new PickupLeadResult
            {
                Status = PickupLeadStatus.DailyLimitReached,
                ConsultantProfileId = consultantProfileId
            };
        }

        await unitOfWork.BeginTransactionAsync(cancellationToken);
        bool pickedUp;
        try
        {
            // Keep the write-lock order consistent with report submission:
            // ConsultantProfiles first, then LeadAssignments. The previous
            // reverse order (atomic pickup SQL first, consultant SaveChanges
            // second) could block/deadlock with EF batches that update the
            // consultant before the lead.
            consultant.IsOnline = false;
            consultant.LastOfflineAt = DateTime.UtcNow;
            await unitOfWork.SaveChangesAsync(cancellationToken);

            pickedUp = await leadAssignmentRepository.TryPickupLeadAsync(
                leadAssignmentId, consultantProfileId, cancellationToken);

            if (!pickedUp)
            {
                await unitOfWork.RollbackAsync(CancellationToken.None);
                return new PickupLeadResult
                {
                    Status = PickupLeadStatus.AlreadyTaken,
                    LeadAssignmentId = leadAssignmentId
                };
            }

            await unitOfWork.CommitAsync(cancellationToken);
        }
        catch
        {
            await unitOfWork.RollbackAsync(CancellationToken.None);
            throw;
        }

        var lead = await leadAssignmentRepository.GetByIdAsync(leadAssignmentId);

        await NotifyRealtimeLeadTakenAsync(leadAssignmentId, consultantProfileId);

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
