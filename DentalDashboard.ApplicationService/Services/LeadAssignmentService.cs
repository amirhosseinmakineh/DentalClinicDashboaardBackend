using DentalDashboard.ApplicationService.Contract.IServices;
using DentalDashboard.Domain.DomainServices;
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
        private readonly ILeadAssignmentStrategy leadAssignmentStrategy;
        private readonly IOfflineLeadAssignmentStrategy offlineLeadAssignmentStrategy;
        private readonly IPushNotificationService pushNotificationService;
        private readonly IConsultantScoreDomainService consultantScoreDomainService;
        private readonly IAttendanceService attendanceService;
        private readonly ILogger<LeadAssignmentService> logger;

        public LeadAssignmentService(HttpClient httpClient, ILeadAssignmentRepository leadAssignmentRepository, ILeadDomainService leadDomainService, IConsultantProfileRepository consultantProfileRepository, ILeadAssignmentStrategy leadAssignmentStrategy, IOfflineLeadAssignmentStrategy offlineLeadAssignmentStrategy, IPushNotificationService pushNotificationService, IConsultantScoreDomainService consultantScoreDomainService, IAttendanceService attendanceService, ILogger<LeadAssignmentService> logger)
        {
            this.httpClient = httpClient;
            this.leadAssignmentRepository = leadAssignmentRepository;
            this.leadDomainService = leadDomainService;
            this.consultantProfileRepository = consultantProfileRepository;
            this.leadAssignmentStrategy = leadAssignmentStrategy;
            this.offlineLeadAssignmentStrategy = offlineLeadAssignmentStrategy;
            this.pushNotificationService = pushNotificationService;
            this.consultantScoreDomainService = consultantScoreDomainService;
            this.attendanceService = attendanceService;
            this.logger = logger;
        }

        public async Task<LeadAssignment[]> LeadsListAsync()
        {
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0");

            var html = await httpClient.GetStringAsync(url);

            var document = new HtmlDocument();
            document.LoadHtml(html);

            var rows = document.DocumentNode.SelectNodes("//table//tr");

            if (rows == null)
                return Array.Empty<LeadAssignment>();

            var leads = new List<LeadAssignment>();

            foreach (var row in rows.Skip(1))
            {
                var cells = row.SelectNodes(".//td");

                if (cells == null || cells.Count < 4)
                    continue;

                string userName = Clean(cells[2].InnerText);
                string phoneNumber = Clean(cells[3].InnerText);

                leads.Add(new LeadAssignment
                {
                    UserName = userName,
                    PhoneNumber = phoneNumber,
                    CreatedAt = DateTime.Now
                });
            }

            return leads.ToArray();
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

            var isWorkingTime = leadDomainService.IsWorkingTime(now);

            foreach (var lead in newLeads)
            {
                lead.CreatedAt = now;

                if (isWorkingTime)
                {
                    lead.AssignmentType = LeadAssignmentType.RealTime;
                    lead.LeadAssignmentState = LeadAssignmentState.New;
                    lead.RequiresThreeMinuteCall = true;
                    lead.CallDeadlineAt = null;
                }
                else
                {
                    lead.AssignmentType = LeadAssignmentType.OfflineQueue;
                    lead.LeadAssignmentState = LeadAssignmentState.New;
                    lead.RequiresThreeMinuteCall = false;
                    lead.CallDeadlineAt = null;
                }
            }

            await leadAssignmentRepository.AddRangeAsync(newLeads);
            await leadAssignmentRepository.SaveChange();
            await ReconcileMisclassifiedLeadStatesAsync();
        }

        public async Task ReconcileMisclassifiedLeadStatesAsync()
        {
            var now = DateTime.Now;
            var changed = false;

            var expiredOfflineLeads = await leadAssignmentRepository.GetAll()
                .Where(x => !x.IsDeleted &&
                            x.AssignmentType == LeadAssignmentType.OfflineQueue &&
                            x.LeadAssignmentState == LeadAssignmentState.Expired)
                .ToListAsync();

            foreach (var lead in expiredOfflineLeads)
            {
                lead.LeadAssignmentState = lead.ConsultantProfileId.HasValue
                    ? LeadAssignmentState.Assigned
                    : LeadAssignmentState.New;
                lead.UpdatedAt = now;
                changed = true;
            }

            if (!leadDomainService.IsWorkingTime(now))
            {
                var unassignedRealtimeLeads = await leadAssignmentRepository.GetAll()
                    .Where(x => !x.IsDeleted &&
                                x.AssignmentType == LeadAssignmentType.RealTime &&
                                x.ConsultantProfileId == null &&
                                x.ReportSubmittedAt == null &&
                                (x.LeadAssignmentState == LeadAssignmentState.New ||
                                 x.LeadAssignmentState == LeadAssignmentState.Pending))
                    .ToListAsync();

                foreach (var lead in unassignedRealtimeLeads)
                {
                    lead.AssignmentType = LeadAssignmentType.OfflineQueue;
                    lead.LeadAssignmentState = LeadAssignmentState.New;
                    lead.RequiresThreeMinuteCall = false;
                    lead.CallDeadlineAt = null;
                    lead.UpdatedAt = now;
                    changed = true;
                }
            }

            if (changed)
                await leadAssignmentRepository.SaveChange();
        }

        public async Task PromoteUnassignedOfflineLeadsToRealtimeAsync()
        {
            if (!leadDomainService.IsWorkingTime(DateTime.Now))
                return;

            var leads = await leadAssignmentRepository.GetAll()
                .Where(x => !x.IsDeleted &&
                            x.AssignmentType == LeadAssignmentType.OfflineQueue &&
                            x.ConsultantProfileId == null &&
                            x.ReportSubmittedAt == null &&
                            (x.LeadAssignmentState == LeadAssignmentState.New ||
                             x.LeadAssignmentState == LeadAssignmentState.Pending))
                .ToListAsync();

            if (!leads.Any())
                return;

            var now = DateTime.Now;
            foreach (var lead in leads)
            {
                lead.AssignmentType = LeadAssignmentType.RealTime;
                lead.LeadAssignmentState = LeadAssignmentState.New;
                lead.RequiresThreeMinuteCall = true;
                lead.CallDeadlineAt = null;
                lead.UpdatedAt = now;
            }

            await leadAssignmentRepository.SaveChange();
            logger.LogInformation(
                "PromoteUnassignedOfflineLeadsToRealtimeAsync promoted {LeadCount} leads to realtime queue",
                leads.Count);
        }

        public async Task AssignOfflineLeadsToConsultantAsync(long consultantProfileId)
        {
            await AssignOfflineLeadsAsync(new[] { consultantProfileId });
        }

        public async Task AssignOfflineLeadsAsync(IReadOnlyCollection<long>? onlyConsultantIds = null)
        {
            var consultants = await consultantProfileRepository.GetAvailableConsultantsForOfflineAssignmentAsync();
            if (onlyConsultantIds is { Count: > 0 })
            {
                var consultantIds = onlyConsultantIds.ToHashSet();
                consultants = consultants
                    .Where(x => consultantIds.Contains(x.Id))
                    .ToList();

                if (!consultants.Any())
                {
                    consultants = await consultantProfileRepository.GetAll()
                        .Where(x => !x.IsDeleted &&
                                    x.IsCompleteProfile &&
                                    x.IsAvailable &&
                                    consultantIds.Contains(x.Id))
                        .OrderByDescending(x => x.CurrentScore)
                        .ThenBy(x => x.Id)
                        .ToListAsync();
                }
            }

            if (!consultants.Any())
            {
                logger.LogInformation(
                    "AssignOfflineLeadsAsync skipped: no available consultants for offline assignment");
                return;
            }

            var pendingOfflineCounts = await leadAssignmentRepository.GetPendingOfflineLeadCountsAsync(
                consultants.Select(x => x.Id));
            var totalRemainingCapacity = consultants
                .Sum(x => Math.Max(OfflineLeadAssignmentStrategy.OfflineBatchSize - pendingOfflineCounts.GetValueOrDefault(x.Id), 0));
            if (totalRemainingCapacity <= 0)
            {
                logger.LogInformation(
                    "AssignOfflineLeadsAsync skipped: consultants already have {BatchSize} unreported offline leads",
                    OfflineLeadAssignmentStrategy.OfflineBatchSize);
                return;
            }

            var leads = await leadAssignmentRepository.GetPendingOfflineLeadsAsync(totalRemainingCapacity);
            if (!leads.Any())
            {
                logger.LogInformation("AssignOfflineLeadsAsync skipped: offline queue is empty");
                return;
            }

            offlineLeadAssignmentStrategy.Assign(leads, consultants, pendingOfflineCounts);

            await leadAssignmentRepository.SaveChange();
            await SendAssignedLeadNotificationsAsync();
            logger.LogInformation(
                "AssignOfflineLeadsAsync assigned {LeadCount} offline leads to {ConsultantCount} consultants",
                leads.Count(l => l.ConsultantProfileId.HasValue),
                consultants.Count);
        }

        public async Task AssignRealTimeLeadsAsync(IReadOnlyCollection<long>? excludedConsultantIds = null)
        {
            if (!leadDomainService.IsWorkingTime(DateTime.Now))
            {
                logger.LogInformation(
                    "AssignRealTimeLeadsAsync skipped: outside consultant working hours");
                return;
            }

            await PromoteUnassignedOfflineLeadsToRealtimeAsync();

            var consultants = await consultantProfileRepository.GetOnlineConsultantsReadyForRealTimeAsync();
            if (excludedConsultantIds is { Count: > 0 })
            { 
                var excluded = excludedConsultantIds.ToHashSet();
                consultants = consultants
                    .Where(x => !excluded.Contains(x.Id))
                    .ToList();
            }

            if (!consultants.Any()) 
            {
                logger.LogInformation(
                    "AssignRealTimeLeadsAsync skipped: no online consultants ready (check IsOnline, pending offline/active realtime leads)");
                return;
            }

            var leads = await leadAssignmentRepository.GetUnassignedRealTimeLeadsAsync(consultants.Count);
            if (!leads.Any())
            {
                logger.LogInformation("AssignRealTimeLeadsAsync skipped: realtime queue is empty");
                return; 
            }

            leadAssignmentStrategy.Assign(leads, consultants);

            var assignedLeadTimes = leads
                .Where(l => l.ConsultantProfileId.HasValue)
                .GroupBy(l => l.ConsultantProfileId!.Value)
                .ToDictionary(g => g.Key, g => g.First().AssignedAt ?? DateTime.Now);

            foreach (var consultant in consultants.Where(x => assignedLeadTimes.ContainsKey(x.Id)))
            {
                consultant.IsOnline = false;
                consultant.LastOfflineAt = assignedLeadTimes[consultant.Id];
            }

            await leadAssignmentRepository.SaveChange();
            await SendAssignedLeadNotificationsAsync();
            logger.LogInformation(
                "AssignRealTimeLeadsAsync assigned {LeadCount} realtime leads",
                leads.Count(l => l.ConsultantProfileId.HasValue));
        }

        public async Task<ExpireLeadRequeueResult> ExpireAndRequeueRealTimeLeadAsync(
            LeadAssignment lead,
            ConsultantProfile consultant)
        {
            var eventScore = await ExpireAndRequeueRealTimeLeadInternalAsync(lead, consultant);

            return new ExpireLeadRequeueResult
            {
                LeadAssignmentId = lead.Id,
                ConsultantProfileId = consultant.Id,
                LeadAssignmentState = lead.LeadAssignmentState,
                EventScore = eventScore,
                DeductedScore = eventScore,
                CurrentScore = consultant.CurrentScore,
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

            var consultantIds = expiredLeads
                .Where(x => x.ConsultantProfileId.HasValue)
                .Select(x => x.ConsultantProfileId!.Value);
            var consultantIdsWithPendingOfflineLeads =
                await leadAssignmentRepository.GetConsultantIdsWithPendingOfflineLeadsAsync(consultantIds);
            var isWorkingTime = leadDomainService.IsWorkingTime(now);
            var failedConsultantIds = new HashSet<long>();

            foreach (var lead in expiredLeads)
            {
                if (lead.ConsultantProfile == null)
                {
                    ResetLeadToRealtimeQueue(lead);
                    continue;
                }

                var consultant = lead.ConsultantProfile;
                failedConsultantIds.Add(consultant.Id);

                ApplyLateCallScore(consultant, lead, now);
                ResetLeadToRealtimeQueue(lead);

                var hasPendingOfflineLeads =
                    consultantIdsWithPendingOfflineLeads.Contains(consultant.Id);
                if (hasPendingOfflineLeads || !isWorkingTime)
                {
                    consultant.IsOnline = false;
                    consultant.LastOfflineAt = now;
                }
                else
                {
                    consultant.IsOnline = true;
                    consultant.LastOnlineAt = now;
                }
            }

            await leadAssignmentRepository.SaveChange();

            if (isWorkingTime)
                await AssignRealTimeLeadsAsync(failedConsultantIds);
        }

        public async Task EnforceNightShiftClosureAsync()
        {
            if (leadDomainService.IsWorkingTime(DateTime.Now))
                return;

            var consultants = await consultantProfileRepository.GetAll()
                .Where(x => !x.IsDeleted &&
                            x.IsCompleteProfile &&
                            (x.IsOnline || x.IsAvailable))
                .ToListAsync();

            if (consultants.Any())
            {
                var now = DateTime.Now;
                foreach (var consultant in consultants)
                {
                    consultant.IsOnline = false;
                    consultant.IsAvailable = false;
                    consultant.LastOfflineAt = now;
                    consultant.WorkEndTime = now.TimeOfDay;
                    consultantProfileRepository.Update(consultant);
                }

                await consultantProfileRepository.SaveChange();
            }

            await ReconcileMisclassifiedLeadStatesAsync();
        }

        private static void ResetLeadToRealtimeQueue(LeadAssignment lead)
        {
            lead.ConsultantProfileId = null;
            lead.LeadAssignmentState = LeadAssignmentState.New;
            lead.AssignmentType = LeadAssignmentType.RealTime;
            lead.AssignedAt = null;
            lead.CallDeadlineAt = null;
            lead.CallInitiatedAt = null;
            lead.NotificationSent = false;
            lead.RequiresThreeMinuteCall = true;
        }

        private async Task<int> ExpireAndRequeueRealTimeLeadInternalAsync(
            LeadAssignment lead,
            ConsultantProfile consultant)
        {
            var now = DateTime.Now;
            var failedConsultantId = consultant.Id;
            var eventScore = ApplyLateCallScore(consultant, lead, now);

            ResetLeadToRealtimeQueue(lead);

            var hasPendingOfflineLeads =
                await leadAssignmentRepository.HasPendingOfflineLeadsAsync(consultant.Id);
            if (hasPendingOfflineLeads || !leadDomainService.IsWorkingTime(now))
            {
                consultant.IsOnline = false;
                consultant.LastOfflineAt = now;
            }
            else
            {
                consultant.IsOnline = true;
                consultant.LastOnlineAt = now;
            }

            leadAssignmentRepository.Update(lead);
            consultantProfileRepository.Update(consultant);
            await leadAssignmentRepository.SaveChange();

            if (leadDomainService.IsWorkingTime(now))
                await AssignRealTimeLeadsAsync(new[] { failedConsultantId });

            return eventScore;
        }

        private int ApplyLateCallScore(ConsultantProfile consultant, LeadAssignment lead, DateTime now)
        {
            var eventScore = consultantScoreDomainService.GetLateCallEventScore();
            consultantScoreDomainService.ApplyScoreEvent(consultant, new ScoreLog
            {
                ConsultantProfileId = consultant.Id,
                Source = ScoreSource.System,
                Reason = ScoreReason.LateCall,
                ScoreValue = eventScore,
                Description = "عدم تماس در بازه سه دقیقه‌ای",
                LeadAssignmentId = lead.Id,
                UserId = consultant.UserId,
                CreatedAt = now,
                IsDeleted = false
            });
            return eventScore;
        }

        private async Task SendAssignedLeadNotificationsAsync()
        {
            var assignedLeads = await leadAssignmentRepository.GetAssignedLeadsPendingNotificationAsync();
            if (!assignedLeads.Any())
                return;

            var anyNotificationSent = false;

            foreach (var group in assignedLeads.GroupBy(x => x.ConsultantProfile!))
            {
                var consultant = group.Key;
                var offlineLeads = group.Where(x => x.AssignmentType == LeadAssignmentType.OfflineQueue).ToList();
                var realTimeLeads = group.Where(x => x.AssignmentType == LeadAssignmentType.RealTime).ToList();

                if (offlineLeads.Count > 0)
                {
                    var sent = await pushNotificationService.SendAsync(
                        consultant.UserId,
                        "لیدهای آفلاین",
                        $"شما {offlineLeads.Count} لید آفلاین دارید.",
                        new Dictionary<string, string>
                        {
                            ["type"] = "offline_leads",
                            ["count"] = offlineLeads.Count.ToString()
                        });

                    if (sent)
                    {
                        foreach (var lead in offlineLeads)
                            lead.NotificationSent = true;

                        anyNotificationSent = true;
                    }
                }

                foreach (var lead in realTimeLeads)
                {
                    var sent = await pushNotificationService.SendAsync(
                        consultant.UserId,
                        "لید جدید",
                        "لید جدید داری — ۳ دقیقه وقت داری برای تماس.",
                        new Dictionary<string, string>
                        {
                            ["type"] = "realtime_lead",
                            ["leadAssignmentId"] = lead.Id.ToString(),
                            ["callDeadlineAt"] = lead.CallDeadlineAt?.ToString("O") ?? string.Empty
                        });

                    if (sent)
                    {
                        lead.NotificationSent = true;
                        anyNotificationSent = true;
                    }
                }
            }

            if (anyNotificationSent)
                await leadAssignmentRepository.SaveChange();
        }


    }
}
