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

    public SeedTestBroadcastLeadCommandHandler(
        ILeadBroadcastService leadBroadcastService,
        IConsultantProfileRepository consultantProfileRepository)
    {
        this.leadBroadcastService = leadBroadcastService;
        this.consultantProfileRepository = consultantProfileRepository;
    }

    public async Task<Result<SeedTestBroadcastLeadResponse>> HandleAsync(
        SeedTestBroadcastLeadCommand command,
        CancellationToken cancellationToken = default)
    {
        if (!LeadBroadcastTestConsultants.IsEnabled)
            return Result<SeedTestBroadcastLeadResponse>.Failure("حالت تست broadcast فعال نیست");

        var onlineTestConsultants = LeadBroadcastTestConsultants.Filter(
            await consultantProfileRepository.GetOnlineConsultantsReadyForRealTimeAsync()).ToList();

        var leadId = await leadBroadcastService.SeedTestBroadcastLeadAsync(cancellationToken);

        return Result<SeedTestBroadcastLeadResponse>.Success(
            new SeedTestBroadcastLeadResponse
            {
                LeadAssignmentId = leadId,
                TestConsultantCount = onlineTestConsultants.Count,
            },
            onlineTestConsultants.Count > 0
                ? $"لید تست #{leadId} برای {onlineTestConsultants.Count} مشاور آنلاین پخش شد"
                : $"لید تست #{leadId} ساخته شد؛ هیچ‌کدام از مشاوران تست آنلاین نیستند");
    }
}
