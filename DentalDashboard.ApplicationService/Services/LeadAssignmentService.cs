using DentalDashboard.ApplicationService.Contract.IServices;
using DentalDashboard.Domain.Enums;
using DentalDashboard.Domain.IDomainService;
using DentalDashboard.Domain.IRepositories;
using HtmlAgilityPack;
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
        public LeadAssignmentService(HttpClient httpClient, ILeadAssignmentRepository leadAssignmentRepository, ILeadDomainService leadDomainService, IConsultantProfileRepository consultantProfileRepository, ILeadAssignmentStrategy leadAssignmentStrategy, IOfflineLeadAssignmentStrategy offlineLeadAssignmentStrategy)
        {
            this.httpClient = httpClient;
            this.leadAssignmentRepository = leadAssignmentRepository;
            this.leadDomainService = leadDomainService;
            this.consultantProfileRepository = consultantProfileRepository;
            this.leadAssignmentStrategy = leadAssignmentStrategy;
            this.offlineLeadAssignmentStrategy = offlineLeadAssignmentStrategy;
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

            var eligibleOnlineConsultants = await consultantProfileRepository.GetOnlineConsultantsReadyForRealTimeAsync();
            var realTimeCapacity = leadDomainService.IsWorkingTime(now)
                ? eligibleOnlineConsultants.Count
                : 0;

            for (var i = 0; i < newLeads.Count; i++)
            {
                var lead = newLeads[i];
                lead.CreatedAt = now;

                if (i < realTimeCapacity)
                {
                    lead.AssignmentType = LeadAssignmentType.RealTime;
                    lead.LeadAssignmentState = LeadAssignmentState.New;
                    lead.RequiresThreeMinuteCall = true;
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
        }

        public async Task AssignPendingOfflineLeadsAsync()
        {
            var consultants = await consultantProfileRepository.GetAvailableConsultantsForOfflineAssignmentAsync();
            if (!consultants.Any())
                return;

            var pendingLeads = await leadAssignmentRepository.GetPendingOfflineLeadsAsync(consultants.Count * 5);
            if (!pendingLeads.Any())
                return;

            offlineLeadAssignmentStrategy.Assign(pendingLeads, consultants);
            await leadAssignmentRepository.SaveChange();
        }

        public async Task AssignRealTimeLeadsAsync()
        {
            var consultants = await consultantProfileRepository.GetOnlineConsultantsReadyForRealTimeAsync();
            if (!consultants.Any())
                return;

            var leads = await leadAssignmentRepository.GetUnassignedRealTimeLeadsAsync(consultants.Count);
            if (!leads.Any())
                return;

            leadAssignmentStrategy.Assign(leads, consultants);

            foreach (var consultant in consultants.Where(x => leads.Any(l => l.ConsultantProfileId == x.Id)))
            {
                consultant.IsOnline = false;
                consultant.LastOfflineAt = leads
                    .Where(l => l.ConsultantProfileId == consultant.Id)
                    .Select(l => l.AssignedAt)
                    .FirstOrDefault() ?? DateTime.Now;
            }

            await leadAssignmentRepository.SaveChange();
        }

        public async Task ExpireOverdueRealTimeLeadsAsync()
        {
            var now = DateTime.Now;
            var expiredLeads = await leadAssignmentRepository.GetExpiredRealTimeLeadsAsync(now);

            foreach (var lead in expiredLeads)
            {
                lead.LeadAssignmentState = LeadAssignmentState.Expired;

                if (lead.ConsultantProfile != null)
                {
                    lead.ConsultantProfile.ScoreLogs.Add(new ScoreLog
                    {
                        ConsultantProfileId = lead.ConsultantProfile.Id,
                        ScoreType = ScoreType.CallNotCompleted,
                        ScoreValue = -10,
                        Description = "عدم تماس در بازه سه دقیقه‌ای"
                    });
                    lead.ConsultantProfile.CurrentScore += -10;

                    var hasPendingOfflineLeads = await leadAssignmentRepository.HasPendingOfflineLeadsAsync(lead.ConsultantProfile.Id);
                    if (hasPendingOfflineLeads || !leadDomainService.IsWorkingTime(now))
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
                await leadAssignmentRepository.SaveChange();
        }

    }
}
