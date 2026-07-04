using DentalDashboard.ApplicationService.Contract.IServices;
using DentalDashboard.Domain.Enums;
using DentalDashboard.Domain.IDomainService;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Domain.Models;
using HtmlAgilityPack;
using Microsoft.Extensions.Configuration;
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
        private readonly ILeadBroadcastService leadBroadcastService;
        private readonly IConfiguration configuration;

        public LeadAssignmentService(
            HttpClient httpClient,
            ILeadAssignmentRepository leadAssignmentRepository,
            ILeadDomainService leadDomainService,
            IConsultantProfileRepository consultantProfileRepository,
            ILeadAssignmentStrategy leadAssignmentStrategy,
            IOfflineLeadAssignmentStrategy offlineLeadAssignmentStrategy,
            IPushNotificationService pushNotificationService,
            ILeadBroadcastService leadBroadcastService,
            IConfiguration configuration)
        {
            this.httpClient = httpClient;
            this.leadAssignmentRepository = leadAssignmentRepository;
            this.leadDomainService = leadDomainService;
            this.consultantProfileRepository = consultantProfileRepository;
            this.leadAssignmentStrategy = leadAssignmentStrategy;
            this.offlineLeadAssignmentStrategy = offlineLeadAssignmentStrategy;
            this.pushNotificationService = pushNotificationService;
            this.leadBroadcastService = leadBroadcastService;
            this.configuration = configuration;
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
                return;

            var realTimeCapacity = 0;
            if (leadDomainService.IsWorkingTime(now))
            {
                var eligibleOnlineConsultants = await consultantProfileRepository.GetOnlineConsultantsReadyForRealTimeAsync();
                var unassignedRealTimeLeads = await leadAssignmentRepository.CountUnassignedRealTimeLeadsAsync();
                realTimeCapacity = Math.Max(
                    eligibleOnlineConsultants.Count - unassignedRealTimeLeads,
                    0);
            }

            var broadcastTimeout = TimeSpan.FromMinutes(
                configuration.GetValue("LeadBroadcast:TimeoutMinutes", 10));

            for (var i = 0; i < newLeads.Count; i++)
            {
                var lead = newLeads[i];
                lead.CreatedAt = now;

                if (i < realTimeCapacity)
                {
                    lead.AssignmentType = LeadAssignmentType.RealTime;
                    lead.LeadAssignmentState = LeadAssignmentState.Broadcasting;
                    lead.BroadcastStartedAt = now;
                    lead.BroadcastExpiresAt = now.Add(broadcastTimeout);
                    lead.RequiresThreeMinuteCall = false;
                    lead.CallDeadlineAt = null;
                }
                else
                {
                    lead.AssignmentType = LeadAssignmentType.OfflineQueue;
                    lead.LeadAssignmentState = LeadAssignmentState.Pending;
                    lead.RequiresThreeMinuteCall = false;
                    lead.CallDeadlineAt = null;
                }
            }

            await leadAssignmentRepository.AddRangeAsync(newLeads);
            await leadAssignmentRepository.SaveChange();

            foreach (var lead in newLeads.Where(x => x.LeadAssignmentState == LeadAssignmentState.Broadcasting))
                await leadBroadcastService.NotifyBroadcastAsync(lead.Id);
        }

        public async Task AssignPendingOfflineLeadsAsync()
        {
            var consultants = await consultantProfileRepository.GetAvailableConsultantsForOfflineAssignmentAsync();
            if (leadDomainService.IsWorkingTime(DateTime.Now))
                consultants = consultants.Where(x => !x.IsOnline).ToList();

            if (!consultants.Any())
                return;

            var dailyAssignedCounts = await leadAssignmentRepository.GetDailyAssignedOfflineLeadCountsAsync(
                consultants.Select(x => x.Id),
                DateTime.Now);
            var totalRemainingDailyCapacity = consultants
                .Sum(x => Math.Max(5 - dailyAssignedCounts.GetValueOrDefault(x.Id), 0));
            if (totalRemainingDailyCapacity <= 0)
                return;

            var pendingLeads = await leadAssignmentRepository.GetPendingOfflineLeadsAsync(totalRemainingDailyCapacity);
            if (!pendingLeads.Any())
                return;

            offlineLeadAssignmentStrategy.Assign(pendingLeads, consultants, dailyAssignedCounts);
            await leadAssignmentRepository.SaveChange();
            await SendAssignedLeadNotificationsAsync();
        }

        public Task BroadcastRealTimeLeadsAsync() =>
            leadBroadcastService.BroadcastPendingRealTimeLeadsAsync();

        public Task ExpireStaleBroadcastsAsync() =>
            leadBroadcastService.ExpireStaleBroadcastsAsync();

        public async Task AssignRealTimeLeadsAsync()
        {
            await BroadcastRealTimeLeadsAsync();
        }

        public async Task ExpireOverdueRealTimeLeadsAsync()
        {
            var now = DateTime.Now;
            var expiredLeads = await leadAssignmentRepository.GetExpiredRealTimeLeadsAsync(now);

            var consultantIds = expiredLeads
                .Where(x => x.ConsultantProfileId.HasValue)
                .Select(x => x.ConsultantProfileId!.Value);
            var consultantIdsWithPendingOfflineLeads =
                await leadAssignmentRepository.GetConsultantIdsWithPendingOfflineLeadsAsync(consultantIds);
            var isWorkingTime = leadDomainService.IsWorkingTime(now);

            foreach (var lead in expiredLeads)
            {
                lead.LeadAssignmentState = LeadAssignmentState.Expired;

                if (lead.ConsultantProfile != null)
                {
                    lead.ConsultantProfile.ScoreLogs.Add(new ScoreLog
                    {
                        ConsultantProfileId = lead.ConsultantProfile.Id,
                        Source = ScoreSource.System,
                        Reason = ScoreReason.LateCall,
                        ScoreValue = -10,
                        Description = "عدم تماس در بازه سه دقیقه‌ای",
                        LeadAssignmentId = lead.Id,
                        UserId = lead.ConsultantProfile.UserId,
                        CreatedAt = now
                    });
                    lead.ConsultantProfile.CurrentScore += -10;

                    var hasPendingOfflineLeads = consultantIdsWithPendingOfflineLeads.Contains(lead.ConsultantProfile.Id);
                    if (hasPendingOfflineLeads || !isWorkingTime)
                    {
                        lead.ConsultantProfile.IsOnline = false;
                        lead.ConsultantProfile.LastOfflineAt = now;
                    }
                    else
                    {
                        lead.ConsultantProfile.IsOnline = true;
                        lead.ConsultantProfile.LastOnlineAt = now;
                    }
                }
            }

            if (expiredLeads.Any())
            {
                await leadAssignmentRepository.SaveChange();

                var anyConsultantBackOnline = expiredLeads.Any(x =>
                    x.ConsultantProfile != null &&
                    x.ConsultantProfile.IsOnline);

                if (anyConsultantBackOnline)
                    await BroadcastRealTimeLeadsAsync();
            }
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
                        "لید جدید داری — سریع بپذیر و تماس بگیر.",
                        new Dictionary<string, string>
                        {
                            ["type"] = "lead_broadcast",
                            ["leadAssignmentId"] = lead.Id.ToString(),
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
