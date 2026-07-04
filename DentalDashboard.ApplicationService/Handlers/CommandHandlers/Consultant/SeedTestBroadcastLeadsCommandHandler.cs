using DentalDashboard.ApplicationService.Contract.IServices;
using DentalDashboard.ApplicationService.Contract.Requests.Consultant.Commands;
using DentalDashboard.ApplicationService.Contract.Responses.LeadResponse;
using DentalDashboard.ApplicationService.Services;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using DentalDashboard.Framwork.Domain;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.ApplicationService.Handlers.CommandHandlers.Consultant;

public class SeedTestBroadcastLeadsCommandHandler
    : ICommandHandler<SeedTestBroadcastLeadsCommand, SeedTestBroadcastLeadsResponse>
{
    private readonly ILeadBroadcastService leadBroadcastService;
    private readonly IConsultantProfileRepository consultantProfileRepository;
    private readonly ILeadAssignmentRepository leadAssignmentRepository;

    public SeedTestBroadcastLeadsCommandHandler(
        ILeadBroadcastService leadBroadcastService,
        IConsultantProfileRepository consultantProfileRepository,
        ILeadAssignmentRepository leadAssignmentRepository)
    {
        this.leadBroadcastService = leadBroadcastService;
        this.consultantProfileRepository = consultantProfileRepository;
        this.leadAssignmentRepository = leadAssignmentRepository;
    }

    public async Task<Result<SeedTestBroadcastLeadsResponse>> HandleAsync(
        SeedTestBroadcastLeadsCommand command,
        CancellationToken cancellationToken = default)
    {
        var onlineTestConsultants = await consultantProfileRepository.GetAll()
            .AsNoTracking()
            .Where(x => !x.IsDeleted &&
                        x.IsCompleteProfile &&
                        x.IsAvailable &&
                        x.IsOnline &&
                        LeadBroadcastTestData.TestUserIds.Contains(x.UserId))
            .ToListAsync(cancellationToken);

        var leadIds = await leadBroadcastService.SeedTestBroadcastLeadsAsync(cancellationToken);
        var leads = await leadAssignmentRepository.GetAll()
            .AsNoTracking()
            .Where(x => leadIds.Contains(x.Id))
            .Select(x => new SeedTestBroadcastLeadItem
            {
                LeadAssignmentId = x.Id,
                UserName = x.UserName,
                PhoneNumber = x.PhoneNumber,
            })
            .ToListAsync(cancellationToken);

        return Result<SeedTestBroadcastLeadsResponse>.Success(
            new SeedTestBroadcastLeadsResponse
            {
                Leads = leads,
                OnlineTestConsultantCount = onlineTestConsultants.Count,
            },
            onlineTestConsultants.Count > 0
                ? $"{leads.Count} لید تست لحظه‌ای برای {onlineTestConsultants.Count} مشاور تست آنلاین پخش شد"
                : $"{leads.Count} لید تست ساخته شد؛ هیچ‌کدام از ۲ مشاور تست آنلاین نیستند — ابتدا آنلاین شوید");
    }
}
