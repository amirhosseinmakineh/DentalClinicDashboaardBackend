using DentalDashboard.ApplicationService.Contract.IServices;
using DentalDashboard.Domain.Enums;
using DentalDashboard.Domain.IDomainService;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Domain.Models;
using HtmlAgilityPack;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Net;

namespace DentalDashboard.ApplicationService.Services
{
    public class LeadAssignmentService : ILeadAssignmentService
    {
        private readonly HttpClient httpClient;
        private const string url = "https://landing.yektanet.com/form/report/vSjrtffitGUytcOHgpLvEzttHcMQiELTANXzyAxTIywCuhjUaBzbMSTNFpZpxKuv";
        private readonly ILeadAssignmentRepository leadAssignmentRepository;
        private readonly ILeadDomainService leadDomainService;
        private readonly IConsultantProfileRepository consultantProfileRepository;
        private readonly ILeadAssignmentLimitService leadAssignmentLimitService;
        private readonly IPushNotificationService pushNotificationService;
        private readonly ILogger<LeadAssignmentService> logger;

        public LeadAssignmentService(
            HttpClient httpClient,
            ILeadAssignmentRepository leadAssignmentRepository,
            ILeadDomainService leadDomainService,
            IConsultantProfileRepository consultantProfileRepository,
            ILeadAssignmentLimitService leadAssignmentLimitService,
            IPushNotificationService pushNotificationService,
            ILogger<LeadAssignmentService> logger)
        {
            this.httpClient = httpClient;
            this.leadAssignmentRepository = leadAssignmentRepository;
            this.leadDomainService = leadDomainService;
            this.consultantProfileRepository = consultantProfileRepository;
            this.leadAssignmentLimitService = leadAssignmentLimitService;
            this.pushNotificationService = pushNotificationService;
            this.logger = logger;
        }

        public async Task<LeadAssignment[]> LeadsListAsync(
          CancellationToken cancellationToken = default)
        {
            try
            {
                if (!httpClient.DefaultRequestHeaders.UserAgent.Any())
                {
                    httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(
                        "Mozilla/5.0");
                }

                using var response = await httpClient.GetAsync(
                    url,
                    HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken);

                response.EnsureSuccessStatusCode();

                var html = await response.Content.ReadAsStringAsync(
                    cancellationToken);

                var document = new HtmlDocument();
                document.LoadHtml(html);

                var rows = document.DocumentNode
                    .SelectNodes("//table//tr");

                if (rows == null || rows.Count <= 1)
                    return Array.Empty<LeadAssignment>();

                var leads = new List<LeadAssignment>();

                foreach (var row in rows.Skip(1))
                {
                    var cells = row.SelectNodes(".//td");

                    if (cells == null || cells.Count < 10)
                        continue;

                    var userName = Clean(cells[2].InnerText);
                    var phoneNumber = Clean(cells[3].InnerText);
                    var createAtText = Clean(cells[9].InnerText);

                    DateTime.TryParse(
                        createAtText,
                        out var createdAt);

                    leads.Add(new LeadAssignment
                    {
                        UserName = userName,
                        PhoneNumber = phoneNumber,
                        CreatedAt = createdAt
                    });
                }

                logger.LogInformation(
                    "Fetched {Count} leads from landing page",
                    leads.Count);

                return leads.ToArray();
            }
            catch (TaskCanceledException ex)
            {
                logger.LogWarning(
                    ex,
                    "Timeout while fetching leads from {Url}",
                    url);

                return Array.Empty<LeadAssignment>();
            }
            catch (HttpRequestException ex)
            {
                logger.LogWarning(
                    ex,
                    "HTTP error while fetching leads from {Url}",
                    url);

                return Array.Empty<LeadAssignment>();
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Unexpected error while parsing leads");

                return Array.Empty<LeadAssignment>();
            }
        }

        private static string Clean(string value)
        {
            return WebUtility.HtmlDecode(value)
                .Replace("\n", "")
                .Replace("\r", "")
                .Replace("\t", "")
                .Trim();
        }

        public async Task AddLeadsAsync()
        {
            var now = DateTime.Now;
            var updatedLeads = await LeadsListAsync();

            var existingPhoneNumbers = await leadAssignmentRepository.GetExistingPhoneNumbersAsync(
                updatedLeads.Select(x => x.PhoneNumber));

            var newLeads = updatedLeads
                .Where(x => !existingPhoneNumbers.Contains(x.PhoneNumber))
                .ToList();

            if (!newLeads.Any())
            {
                logger.LogDebug("AddLeadsAsync skipped: no new phone numbers from landing page");
                return;
            }

            foreach (var lead in newLeads)
            {
                lead.CreatedAt = now;
                lead.CallDeadlineAt = null;
                lead.AssignmentType = LeadAssignmentType.RealTime;
                lead.RequiresThreeMinuteCall = true;
                lead.LeadAssignmentState = LeadAssignmentState.New;
            }

            await leadAssignmentRepository.AddRangeAsync(newLeads);
            await leadAssignmentRepository.SaveChange();
        }

        public async Task ReconcileMisclassifiedLeadStatesAsync()
        {
            var now = DateTime.Now;
            var changed = false;

            var pendingWithoutReport = await leadAssignmentRepository.GetAll()
                .Where(x => !x.IsDeleted &&
                            x.LeadAssignmentState == LeadAssignmentState.Pending &&
                            x.ReportSubmittedAt == null)
                .ToListAsync();

            foreach (var lead in pendingWithoutReport)
            {
                lead.LeadAssignmentState = lead.ConsultantProfileId.HasValue
                    ? LeadAssignmentState.Assigned
                    : LeadAssignmentState.New;
                lead.UpdatedAt = now;
                changed = true;
            }

            if (changed)
                await leadAssignmentRepository.SaveChange();
        }

        public async Task AssignRealTimeLeadsAsync(
            IReadOnlyCollection<long>? excludedConsultantIds = null)
        {
            if (!leadDomainService.IsWorkingTime(DateTime.Now))
            {
                logger.LogInformation("Realtime dispatch skipped: outside working hours");
                return;
            }

            var consultants = await consultantProfileRepository
                .GetOnlineConsultantsReadyForRealTimeAsync();

            if (excludedConsultantIds is { Count: > 0 })
            {
                var excluded = excludedConsultantIds.ToHashSet();
                consultants = consultants
                    .Where(x => !excluded.Contains(x.Id))
                    .ToList();
            }

            if (!consultants.Any())
            {
                logger.LogInformation("Realtime dispatch skipped: no online consultants");
                return;
            }

            var availableConsultants = new List<ConsultantProfile>();

            foreach (var consultant in consultants)
            {
                if (await leadAssignmentLimitService.CanPickupLeadAsync(consultant.Id))
                    availableConsultants.Add(consultant);
            }

            if (!availableConsultants.Any())
            {
                logger.LogInformation("Realtime dispatch skipped: no consultant capacity");
                return;
            }

            var leads = await leadAssignmentRepository
                .GetRealtimeLeadsForDispatchAsync(1, TimeSpan.FromSeconds(10));

            if (!leads.Any())
            {
                logger.LogInformation("Realtime dispatch skipped: no pending realtime leads");
                return;
            }

            foreach (var lead in leads)
            {
                foreach (var consultant in availableConsultants)
                {
                    await pushNotificationService.SendAsync(
                        consultant.UserId,
                        "لید جدید",
                        "یک لید جدید برای دریافت وجود دارد",
                        new Dictionary<string, string>
                        {
                            ["leadId"] = lead.Id.ToString(),
                            ["type"] = "RealtimeLead"
                        });
                }

                lead.NotificationSent = true;
                lead.LastDispatchAt = DateTime.UtcNow;
            }

            await leadAssignmentRepository.SaveChange();

            logger.LogInformation(
                "Realtime dispatch completed. Leads: {count}",
                leads.Count);
        }

        public async Task NotifyRealtimeLeadTakenAsync(
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

            logger.LogInformation(
                "Realtime lead taken notification sent for lead {LeadId} to {ConsultantCount} consultants",
                leadAssignmentId,
                consultants.Count);
        }

        public async Task<ExpireLeadRequeueResult> ExpireAndRequeueRealTimeLeadAsync(
            LeadAssignment lead,
            ConsultantProfile consultant)
        {
            await ExpireAndRequeueRealTimeLeadInternalAsync(lead, consultant);

            return new ExpireLeadRequeueResult
            {
                LeadAssignmentId = lead.Id,
                ConsultantProfileId = consultant.Id,
                LeadAssignmentState = lead.LeadAssignmentState,
                IsConsultantOnline = consultant.IsOnline,
                WasRequeued = true
            };
        }

        public async Task ExpireOverdueRealTimeLeadsAsync()
        {
            var now = DateTime.Now;
            var expiredLeads = await leadAssignmentRepository.GetExpiredRealTimeLeadsAsync(now);

            if (!expiredLeads.Any())
                return;

            var failedConsultantIds = new HashSet<long>();

            foreach (var lead in expiredLeads)
            {
                if (lead.ConsultantProfile == null)
                {
                    ResetLeadQueue(lead);
                    continue;
                }

                var consultant = lead.ConsultantProfile;
                failedConsultantIds.Add(consultant.Id);

                ResetLeadQueue(lead);

                if (leadDomainService.IsWorkingTime(now))
                {
                    consultant.IsOnline = true;
                    consultant.LastOnlineAt = now;
                }
                else
                {
                    consultant.IsOnline = false;
                    consultant.LastOfflineAt = now;
                }
            }

            await leadAssignmentRepository.SaveChange();

            if (leadDomainService.IsWorkingTime(now))
                await AssignRealTimeLeadsAsync(failedConsultantIds);
        }

        private void ResetLeadQueue(LeadAssignment lead)
        {
            lead.ConsultantProfileId = null;
            lead.LeadAssignmentState = LeadAssignmentState.New;
            lead.AssignedAt = null;
            lead.CallDeadlineAt = null;
            lead.CallInitiatedAt = null;
            lead.NotificationSent = false;
            lead.PickUp = false;
            lead.DispatchLevel = 0;
            lead.LastDispatchAt = null;
            lead.AssignmentType = LeadAssignmentType.RealTime;
            lead.RequiresThreeMinuteCall = true;
        }

        private async Task ExpireAndRequeueRealTimeLeadInternalAsync(
            LeadAssignment lead,
            ConsultantProfile consultant)
        {
            var now = DateTime.Now;
            var failedConsultantId = consultant.Id;

            ResetLeadQueue(lead);

            if (leadDomainService.IsWorkingTime(now))
            {
                consultant.IsOnline = true;
                consultant.LastOnlineAt = now;
            }
            else
            {
                consultant.IsOnline = false;
                consultant.LastOfflineAt = now;
            }

            leadAssignmentRepository.Update(lead);
            consultantProfileRepository.Update(consultant);
            await leadAssignmentRepository.SaveChange();

            if (leadDomainService.IsWorkingTime(now))
                await AssignRealTimeLeadsAsync(new[] { failedConsultantId });
        }
    }
}
