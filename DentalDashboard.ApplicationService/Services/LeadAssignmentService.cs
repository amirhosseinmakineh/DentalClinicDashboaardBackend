using DentalDashboard.ApplicationService.Contract.IServices;
using DentalDashboard.Domain.DomainServices;
using DentalDashboard.Domain.Enums;
using DentalDashboard.Domain.IDomainService;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Utilities.Convertor;
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

                var now = DateTime.Now.TimeOfDay;

                bool isNightTime = now >= TimeSpan.FromHours(21) || now < TimeSpan.FromHours(9);


                leads.Add(new LeadAssignment
                {
                    UserName = userName,
                    PhoneNumber = phoneNumber,
                    CreatedAt = DateTime.Now,

                    LeadAssignmentState = isNightTime
                    ? LeadAssignmentState.Pending
                    : LeadAssignmentState.New
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
            var newLeads = leadDomainService.GetNewLeads(oldLeads, updatedLeads).ToList();
            var consultants = await consultantProfileRepository.GetAvailableConsultantsAsync();
            var presentConsultants = consultants
                .Where(x => !x.IsDeleted &&
                            x.IsCompleteProfile &&
                            x.IsAvailable)
                .OrderBy(x => x.Id)
                .ToList();

            // 3️⃣ لیدهای OfflineQueue که از شب قبل تا الان اومدن
            var pendingOfflineLeads = (await leadAssignmentRepository.GetAllAsync())
                .Where(x => x.AssignmentType == LeadAssignmentType.OfflineQueue &&
                            x.LeadAssignmentState == LeadAssignmentState.Pending &&
                            x.CreatedAt >= DateTime.Today.AddHours(-12)) 
                .ToList();

            // 4️⃣ تقسیم OfflineQueue بین مشاوران حاضر
            if (pendingOfflineLeads.Any() && presentConsultants.Any())
            {
                offlineLeadAssignmentStrategy.Assign(pendingOfflineLeads, presentConsultants);
                await leadAssignmentRepository.SaveChange();
            }

            // 5️⃣ پیدا کردن مشاوران آنلاین که تکلیف OfflineQueueهاشون مشخص شده
            var onlineConsultants = consultants
                .Where(x => x.IsOnline && !pendingOfflineLeads.Any(l => l.ConsultantProfileId == x.Id && l.LeadAssignmentState == LeadAssignmentState.Pending))
                .ToList();

            // 6️⃣ تعیین AssignmentType برای لیدهای جدید
            var assignmentType = leadDomainService.DetermineAssignmentType(now, onlineConsultants.Any());

            // ست کردن فیلدهای عمومی لیدهای جدید
            foreach (var lead in newLeads)
            {
                lead.CreatedAt = now;
                lead.AssignmentType = assignmentType;
                lead.LeadAssignmentState = LeadAssignmentState.Pending;
                lead.RequiresThreeMinuteCall = assignmentType == LeadAssignmentType.RealTime;
                lead.CallDeadlineAt = assignmentType == LeadAssignmentType.RealTime ? now.AddMinutes(3) : null;
            }

            // 7️⃣ تقسیم RealTime به مشاوران آنلاین آماده
            if (assignmentType == LeadAssignmentType.RealTime && onlineConsultants.Any())
            {
                leadAssignmentStrategy.Assign(newLeads, onlineConsultants);
                foreach (var lead in newLeads)
                {
                    lead.LeadAssignmentState = LeadAssignmentState.Assigned;
                    lead.AssignedAt = now;
                }
            }

            // 8️⃣ ذخیره همه لیدها
            await leadAssignmentRepository.AddRangeAsync(newLeads);
            await leadAssignmentRepository.SaveChange();
        }

    }
}
