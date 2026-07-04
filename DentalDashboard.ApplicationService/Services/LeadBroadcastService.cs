using DentalDashboard.ApplicationService.Contract.IServices;
using DentalDashboard.ApplicationService.Hubs;
using DentalDashboard.Domain.Enums;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Domain.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DentalDashboard.ApplicationService.Services;

public sealed class LeadBroadcastService : ILeadBroadcastService
{
    private readonly ILeadAssignmentRepository leadAssignmentRepository;
    private readonly IConsultantProfileRepository consultantProfileRepository;
    private readonly IHubContext<LeadBroadcastHub> hub;
    private readonly IPushNotificationService pushNotificationService;
    private readonly IConfiguration configuration;
    private readonly ILogger<LeadBroadcastService> logger;
    private readonly LeadBroadcastTestFilter broadcastTestFilter;
    private readonly TimeSpan broadcastTimeout;

    public LeadBroadcastService(
        ILeadAssignmentRepository leadAssignmentRepository,
        IConsultantProfileRepository consultantProfileRepository,
        IHubContext<LeadBroadcastHub> hub,
        IPushNotificationService pushNotificationService,
        IConfiguration configuration,
        ILogger<LeadBroadcastService> logger,
        LeadBroadcastTestFilter broadcastTestFilter)
    {
        this.leadAssignmentRepository = leadAssignmentRepository;
        this.consultantProfileRepository = consultantProfileRepository;
        this.hub = hub;
        this.pushNotificationService = pushNotificationService;
        this.configuration = configuration;
        this.logger = logger;
        this.broadcastTestFilter = broadcastTestFilter;
        broadcastTimeout = TimeSpan.FromMinutes(configuration.GetValue("LeadBroadcast:TimeoutMinutes", 10));
    }

    public async Task NotifyBroadcastAsync(long leadAssignmentId, CancellationToken cancellationToken = default)
    {
        var assignment = await leadAssignmentRepository.GetAll()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => !x.IsDeleted && x.Id == leadAssignmentId, cancellationToken);

        if (assignment is null || assignment.LeadAssignmentState != LeadAssignmentState.Broadcasting)
            return;

        var message = new LeadBroadcastStartedMessage
        {
            LeadAssignmentId = assignment.Id,
            CreatedAt = assignment.CreatedAt,
            BroadcastStartedAt = assignment.BroadcastStartedAt,
        };

        await hub.Clients
            .Group(LeadBroadcastHub.OnlineConsultantsGroup)
            .SendAsync(LeadBroadcastHubEvents.LeadBroadcastStarted, message, cancellationToken);

        var onlineConsultants = await consultantProfileRepository.GetOnlineConsultantsReadyForRealTimeAsync();
        var pushTargets = broadcastTestFilter
            .FilterForTestLeadPush(onlineConsultants, assignment)
            .ToList();

        foreach (var consultant in pushTargets)
        {
            await pushNotificationService.SendAsync(
                consultant.UserId,
                "لید جدید!",
                "یک لید جدید منتظر پذیرش است — سریع بردارید.",
                new Dictionary<string, string>
                {
                    ["type"] = "lead_broadcast",
                    ["leadAssignmentId"] = assignment.Id.ToString(),
                },
                cancellationToken);
        }
    }

    public async Task BroadcastPendingRealTimeLeadsAsync(CancellationToken cancellationToken = default)
    {
        await ExpireStaleBroadcastsAsync(cancellationToken);

        var onlineConsultants =
            await consultantProfileRepository.GetOnlineConsultantsReadyForRealTimeAsync();
        if (!onlineConsultants.Any())
            return;

        var leads = await leadAssignmentRepository.GetPendingBroadcastRealTimeLeadsAsync(onlineConsultants.Count);
        if (!leads.Any())
            return;

        var now = DateTime.Now;
        foreach (var lead in leads)
        {
            lead.LeadAssignmentState = LeadAssignmentState.Broadcasting;
            lead.BroadcastStartedAt = now;
            lead.BroadcastExpiresAt = now.Add(broadcastTimeout);
            lead.RequiresThreeMinuteCall = false;
            lead.CallDeadlineAt = null;
            leadAssignmentRepository.Update(lead);
        }

        await leadAssignmentRepository.SaveChange();

        foreach (var lead in leads)
            await NotifyBroadcastAsync(lead.Id, cancellationToken);
    }

    public async Task ExpireStaleBroadcastsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.Now;
        var expired = await leadAssignmentRepository.GetStaleBroadcastingLeadsAsync(now);
        if (!expired.Any())
            return;

        foreach (var assignment in expired)
        {
            assignment.LeadAssignmentState = LeadAssignmentState.Pending;
            assignment.AssignmentType = LeadAssignmentType.OfflineQueue;
            assignment.BroadcastStartedAt = null;
            assignment.BroadcastExpiresAt = null;
            assignment.RequiresThreeMinuteCall = false;
            assignment.CallDeadlineAt = null;
            leadAssignmentRepository.Update(assignment);

            await hub.Clients
                .Group(LeadBroadcastHub.OnlineConsultantsGroup)
                .SendAsync(
                    LeadBroadcastHubEvents.LeadBroadcastExpired,
                    new LeadBroadcastExpiredMessage { LeadAssignmentId = assignment.Id },
                    cancellationToken);
        }

        await leadAssignmentRepository.SaveChange();
        logger.LogInformation("Moved {Count} stale lead broadcasts to the offline queue.", expired.Count);
    }

    public async Task<IReadOnlyList<long>> SeedTestBroadcastLeadsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.Now;
        var existingPhones = await leadAssignmentRepository.GetExistingPhoneNumbersAsync(
            LeadBroadcastTestData.TestBroadcastLeads.Select(x => x.PhoneNumber));

        var createdIds = new List<long>();
        foreach (var (userName, phoneNumber) in LeadBroadcastTestData.TestBroadcastLeads)
        {
            if (existingPhones.Contains(phoneNumber))
            {
                var existing = await leadAssignmentRepository.GetAll()
                    .FirstOrDefaultAsync(
                        x => !x.IsDeleted &&
                             x.PhoneNumber == phoneNumber &&
                             x.LeadAssignmentState == LeadAssignmentState.Broadcasting,
                        cancellationToken);

                if (existing is not null)
                {
                    createdIds.Add(existing.Id);
                    await NotifyBroadcastAsync(existing.Id, cancellationToken);
                    continue;
                }
            }

            var lead = new LeadAssignment
            {
                UserName = userName,
                PhoneNumber = phoneNumber,
                AssignmentType = LeadAssignmentType.RealTime,
                LeadAssignmentState = LeadAssignmentState.Broadcasting,
                BroadcastStartedAt = now,
                BroadcastExpiresAt = now.Add(broadcastTimeout),
                CreatedAt = now,
                RequiresThreeMinuteCall = false,
                CallDeadlineAt = null,
                IsDeleted = false,
            };

            await leadAssignmentRepository.AddAsync(lead);
            await leadAssignmentRepository.SaveChange();
            createdIds.Add(lead.Id);
            await NotifyBroadcastAsync(lead.Id, cancellationToken);
        }

        return createdIds;
    }

    public async Task<long> SeedTestBroadcastLeadAsync(CancellationToken cancellationToken = default)
    {
        var ids = await SeedTestBroadcastLeadsAsync(cancellationToken);
        return ids.FirstOrDefault();
    }

    internal static (string FirstName, string LastName) SplitUserName(string userName)
    {
        var trimmed = userName.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
            return (string.Empty, string.Empty);

        var spaceIndex = trimmed.IndexOf(' ');
        if (spaceIndex < 0)
            return (trimmed, string.Empty);

        return (trimmed[..spaceIndex], trimmed[(spaceIndex + 1)..].Trim());
    }

    internal static string MaskName(string value)
    {
        var trimmed = value.Trim();
        if (trimmed.Length <= 1) return "*";
        return $"{trimmed[0]}***";
    }
}
