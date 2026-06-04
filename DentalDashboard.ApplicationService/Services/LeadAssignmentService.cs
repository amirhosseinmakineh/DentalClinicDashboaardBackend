using DentalDashboard.ApplicationService.Contract.IServices;
using DentalDashboard.Domain.DomainServices;
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
        private readonly LeadDomainService leadDomainService;
        private readonly IConsultantProfileRepository consultantProfileRepository;
        private readonly ILeadAssignmentStrategy leadAssignmentStrategy;
        public LeadAssignmentService(HttpClient httpClient, ILeadAssignmentRepository leadAssignmentRepository, LeadDomainService leadDomainService, IConsultantProfileRepository consultantProfileRepository, ILeadAssignmentStrategy leadAssignmentStrategy)
        {
            this.httpClient = httpClient;
            this.leadAssignmentRepository = leadAssignmentRepository;
            this.leadDomainService = leadDomainService;
            this.consultantProfileRepository = consultantProfileRepository;
            this.leadAssignmentStrategy = leadAssignmentStrategy;
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

                if (cells == null || cells.Count < 2)
                    continue;

                string userName = Clean(cells[2].InnerText);
                string phoneNumber = Clean(cells[3].InnerText);
                DateTime createDate = DateTime.UtcNow;

                leads.Add(new LeadAssignment
                {
                    UserName = userName,
                    CreatedAt = createDate,
                    LeadAssignmentState = LeadAssignmentState.New,
                    PhoneNumber = phoneNumber
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

            var oldLeads = await leadAssignmentRepository.GetAllAsync();

            var updatedLeads = await LeadsListAsync();

            var newLeads = leadDomainService
                .GetNewLeads(oldLeads, updatedLeads)
                .ToList();

            if (!newLeads.Any())
                return;

            var consultants = await consultantProfileRepository
                .GetAvailableConsultantsAsync();

            var onlineConsultants = consultants
                .Where(x => x.IsAvailable && x.IsOnline)
                .ToList();

            var assignmentType = leadDomainService.DetermineAssignmentType(
                now,
                onlineConsultants.Any()
            );

            foreach (var lead in newLeads)
            {
                lead.CreatedAt = now;
                lead.AssignmentType = assignmentType;
                lead.LeadAssignmentState = LeadAssignmentState.Pending;

                lead.RequiresThreeMinuteCall =
                    assignmentType == LeadAssignmentType.RealTime;

                lead.CallDeadlineAt =
                    assignmentType == LeadAssignmentType.RealTime
                        ? now.AddMinutes(3)
                        : null;
            }

            if (assignmentType == LeadAssignmentType.RealTime)
            {
                leadAssignmentStrategy.Assign(newLeads, onlineConsultants);

                foreach (var lead in newLeads)
                {
                    lead.LeadAssignmentState = LeadAssignmentState.Assigned;
                    lead.AssignedAt = now;
                }

                // TODO: SMS + Notification فقط برای RealTime
            }

            await leadAssignmentRepository.AddRangeAsync(newLeads);
            await leadAssignmentRepository.SaveChange();
        }

    }
}
