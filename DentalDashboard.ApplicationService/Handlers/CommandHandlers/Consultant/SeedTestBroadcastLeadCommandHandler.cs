using DentalDashboard.ApplicationService.Contract.IServices;
using DentalDashboard.ApplicationService.Contract.Requests.Consultant.Commands;
using DentalDashboard.ApplicationService.Contract.Responses.LeadResponse;
using DentalDashboard.ApplicationService.Services;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using DentalDashboard.Framwork.Domain;

namespace DentalDashboard.ApplicationService.Handlers.CommandHandlers.Consultant;

public class SeedTestBroadcastLeadCommandHandler
    : ICommandHandler<SeedTestBroadcastLeadCommand, SeedTestBroadcastLeadResponse>
{
    private readonly ILeadBroadcastService leadBroadcastService;
    private readonly IConsultantProfileRepository consultantProfileRepository;
    private readonly LeadBroadcastTestFilter broadcastTestFilter;

    public SeedTestBroadcastLeadCommandHandler(
        ILeadBroadcastService leadBroadcastService,
        IConsultantProfileRepository consultantProfileRepository,
        LeadBroadcastTestFilter broadcastTestFilter)
    {
        this.leadBroadcastService = leadBroadcastService;
        this.consultantProfileRepository = consultantProfileRepository;
        this.broadcastTestFilter = broadcastTestFilter;
    }

    public async Task<Result<SeedTestBroadcastLeadResponse>> HandleAsync(
        SeedTestBroadcastLeadCommand command,
        CancellationToken cancellationToken = default)
    {
        var onlineConsultants = broadcastTestFilter.FilterEligibleForBroadcast(
            await consultantProfileRepository.GetOnlineConsultantsReadyForRealTimeAsync()).ToList();

        var leadId = await leadBroadcastService.SeedTestBroadcastLeadAsync(cancellationToken);

        return Result<SeedTestBroadcastLeadResponse>.Success(
            new SeedTestBroadcastLeadResponse
            {
                LeadAssignmentId = leadId,
                TestConsultantCount = onlineConsultants.Count,
            },
            onlineConsultants.Count > 0
                ? $"لید تست #{leadId} برای {onlineConsultants.Count} مشاور تست آنلاین پخش شد"
                : $"لید تست #{leadId} ساخته شد؛ هیچ‌کدام از ۲ مشاور تست آنلاین و واجد شرایط نیستند");
    }
}
