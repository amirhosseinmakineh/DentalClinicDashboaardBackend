using DentalDashboard.ApplicationService.Contract.IServices;
using DentalDashboard.Domain.Enums;
using DentalDashboard.Domain.IDomainService;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Domain.Models;
using DentalDashboard.Infrastracture.Repository;
using HtmlAgilityPack;
using Microsoft.EntityFrameworkCore;
using System.Net;

namespace DentalDashboard.ApplicationService.Services
{
    public class LeadAssignmentService : ILeadAssignmentService
    {
        private readonly HttpClient httpClient;
        private static readonly TimeSpan RealtimeLeadRedispatchInterval = TimeSpan.FromSeconds(6);
        private const string url = "https://landing.yektanet.com/form/report/vSjrtffitGUytcOHgpLvEzttHcMQiELTANXzyAxTIywCuhjUaBzbMSTNFpZpxKuv";
        private readonly ILeadAssignmentRepository leadAssignmentRepository;
        private readonly ILeadDomainService leadDomainService;
        private readonly IConsultantProfileRepository consultantProfileRepository;
        private readonly ILeadAssignmentLimitService leadAssignmentLimitService;
        private readonly IPushNotificationService pushNotificationService;
        private readonly IServiceLogRepository serviceLogRepository;
        private readonly ILeadAssignmentCandidateProvider candidateProvider;
        private readonly Microsoft.Extensions.Logging.ILogger<LeadAssignmentService> logger;

        public LeadAssignmentService(
            HttpClient httpClient,
            ILeadAssignmentRepository leadAssignmentRepository,
            ILeadDomainService leadDomainService,
            IConsultantProfileRepository consultantProfileRepository,
            ILeadAssignmentLimitService leadAssignmentLimitService,
            IPushNotificationService pushNotificationService,
            IServiceLogRepository serviceLogRepository,
            ILeadAssignmentCandidateProvider candidateProvider,
            Microsoft.Extensions.Logging.ILogger<LeadAssignmentService> logger)
        {
            this.httpClient = httpClient;
            this.leadAssignmentRepository = leadAssignmentRepository;
            this.leadDomainService = leadDomainService;
            this.consultantProfileRepository = consultantProfileRepository;
            this.leadAssignmentLimitService = leadAssignmentLimitService;
            this.pushNotificationService = pushNotificationService;
            this.serviceLogRepository = serviceLogRepository;
            this.candidateProvider = candidateProvider;
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

                var log = new ServiceLog()
                {
                    CreatedAt = DateTime.UtcNow,
                    DeletedAt = null,
                    LogName = "Yektanet",
                    ResponseLog = response.ReasonPhrase
                };
                await serviceLogRepository.AddAsync(log);
                await serviceLogRepository.SaveChange();

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

                return leads.ToArray();
            }
            catch (TaskCanceledException)
            {
                return Array.Empty<LeadAssignment>();
            }
            catch (HttpRequestException)
            {
                return Array.Empty<LeadAssignment>();
            }
            catch (Exception)
            {
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
                return;
            }

            var candidateBatch = await candidateProvider
                .GetCurrentForDispatchAsync(RealtimeLeadRedispatchInterval);
            LogCandidateBatch(candidateBatch);
            var lead = candidateBatch.Lead;

            if (lead == null)
            {
                return;
            }

            var isReminder = lead.NotificationSent && lead.LastDispatchAt.HasValue;

            if (!await NotifyConsultantsForRealtimeLeadAsync(
                    lead,
                    availableConsultants,
                    candidateBatch.SourceType,
                    isReminder))
                return;

            lead.NotificationSent = true;
            lead.LastDispatchAt = DateTime.UtcNow;

            await leadAssignmentRepository.SaveChange();

        }

        private async Task<bool> NotifyConsultantsForRealtimeLeadAsync(
            LeadAssignment lead,
            IReadOnlyList<ConsultantProfile> consultants,
            LeadAssignmentSourceType sourceType,
            bool isReminder = false)
        {
            var (title, body) = BuildRealtimeLeadNotificationContent(lead, isReminder);
            var notificationSent = false;

            foreach (var consultant in consultants)
            {
                if (sourceType == LeadAssignmentSourceType.BurnedLeads &&
                    lead.ConsultantProfileId == consultant.Id)
                    continue;

                try
                {
                    await pushNotificationService.SendAsync(
                        consultant.UserId,
                        title,
                        body,
                        new Dictionary<string, string>
                        {
                            ["leadId"] = lead.Id.ToString(),
                            ["type"] = "RealtimeLead",
                            ["userName"] = lead.UserName ?? string.Empty,
                            ["phoneNumber"] = lead.PhoneNumber ?? string.Empty,
                            ["isReminder"] = isReminder ? "true" : "false",
                        });
                    logger.LogInformation(
                        "Lead assignment broadcast succeeded. LeadId: {LeadId}, ConsultantId: {ConsultantId}",
                        lead.Id,
                        consultant.Id);
                    notificationSent = true;
                }
                catch (Exception ex)
                {
                    logger.LogError(
                        ex,
                        "Lead assignment broadcast failed. LeadId: {LeadId}, ConsultantId: {ConsultantId}",
                        lead.Id,
                        consultant.Id);
                }
            }

            return notificationSent;
        }

        private void LogCandidateBatch(LeadAssignmentCandidateBatch batch)
        {
            logger.LogInformation(
                "Lead assignment candidates selected. AssignmentSourceType: {AssignmentSourceType}, CandidateCount: {CandidateCount}, LeadId: {LeadId}",
                batch.SourceType,
                batch.CandidateCount,
                batch.Lead?.Id);
        }

        private static (string Title, string Body) BuildRealtimeLeadNotificationContent(
            LeadAssignment lead,
            bool isReminder)
        {
            var name = string.IsNullOrWhiteSpace(lead.UserName)
                ? "نامشخص"
                : lead.UserName.Trim();
            var phone = string.IsNullOrWhiteSpace(lead.PhoneNumber)
                ? "نامشخص"
                : lead.PhoneNumber.Trim();

            var title = isReminder
                ? $"یادآوری لید: {name}"
                : $"لید جدید: {name}";
            var body = $"شماره تماس: {phone} — جهت دریافت روی اعلان کلیک کنید.";

            return (title, body);
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

        public async Task AssignLeadToTestConsultant(IReadOnlyCollection<long>? excludedConsultantIds = null)
        {
            if (!leadDomainService.IsWorkingTime(DateTime.Now))
            {
                return;
            }

            var consultants = await consultantProfileRepository
                .GetAvailableAndOnnlineTestConsultant();

            excludedConsultantIds = await ManageExcludeConsultants();

            if (excludedConsultantIds is { Count: > 0 })
            {
                var excluded = excludedConsultantIds.ToHashSet();
                consultants = consultants
                    .Where(x => !excluded.Contains(x.Id))
                    .ToList();
            }
            if (!consultants.Any())
            {
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
                return;
            }

            var candidateBatch = await candidateProvider
                .GetCurrentForDispatchAsync(RealtimeLeadRedispatchInterval);
            LogCandidateBatch(candidateBatch);
            var lead = candidateBatch.Lead;

            if (lead == null)
            {
                return;
            }

            var isReminder = lead.NotificationSent && lead.LastDispatchAt.HasValue;

            if (!await NotifyConsultantsForRealtimeLeadAsync(
                    lead,
                    availableConsultants,
                    candidateBatch.SourceType,
                    isReminder))
                return;

            lead.NotificationSent = true;
            lead.LastDispatchAt = DateTime.UtcNow;

            await leadAssignmentRepository.SaveChange();

        }

        public async Task AssignLeadToSellerConsultant(IReadOnlyCollection<long>? excludedConsultantIds = null)
        {
            if (!leadDomainService.IsWorkingTime(DateTime.Now))
            {
                return;
            }

            var consultants = await consultantProfileRepository
                .GetAvailableAndOnnlineSellerConsultant();

            excludedConsultantIds = await ManageExcludeConsultants();

            if (excludedConsultantIds is { Count: > 0 })
            {
                var excluded = excludedConsultantIds.ToHashSet();
                consultants = consultants
                    .Where(x => !excluded.Contains(x.Id))
                    .ToList();
            }
            if (!consultants.Any())
            {
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
                return;
            }

            var candidateBatch = await candidateProvider
                .GetCurrentForDispatchAsync(RealtimeLeadRedispatchInterval);
            LogCandidateBatch(candidateBatch);
            var lead = candidateBatch.Lead;

            if (lead == null)
            {
                return;
            }

            var isReminder = lead.NotificationSent && lead.LastDispatchAt.HasValue;

            if (!await NotifyConsultantsForRealtimeLeadAsync(
                    lead,
                    availableConsultants,
                    candidateBatch.SourceType,
                    isReminder))
                return;

            lead.NotificationSent = true;
            lead.LastDispatchAt = DateTime.UtcNow;

            await leadAssignmentRepository.SaveChange();

        }

        public async Task AssignLeadToTopSellertConsultant(IReadOnlyCollection<long>? excludedConsultantIds = null)
        {
            if (!leadDomainService.IsWorkingTime(DateTime.Now))
            {
                return;
            }


            var consultants = await consultantProfileRepository
                .GetAvailableAndOnnlineTopSellerConsultant();

            excludedConsultantIds = await ManageExcludeConsultants();

            if (excludedConsultantIds is { Count: > 0 })
            {
                var excluded = excludedConsultantIds.ToHashSet();
                consultants = consultants
                    .Where(x => !excluded.Contains(x.Id))
                    .ToList();

            }
            if (!consultants.Any())
            {
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
                return;
            }

            var candidateBatch = await candidateProvider
                .GetCurrentForDispatchAsync(RealtimeLeadRedispatchInterval);
            LogCandidateBatch(candidateBatch);
            var lead = candidateBatch.Lead;

            if (lead == null)
            {
                return;
            }

            var isReminder = lead.NotificationSent && lead.LastDispatchAt.HasValue;

            if (!await NotifyConsultantsForRealtimeLeadAsync(
                    lead,
                    availableConsultants,
                    candidateBatch.SourceType,
                    isReminder))
                return;

            lead.NotificationSent = true;
            lead.LastDispatchAt = DateTime.UtcNow;

            await leadAssignmentRepository.SaveChange();

        }
        private async Task<IReadOnlyCollection<long>> ManageExcludeConsultants()
        {
            var excludeConsultants = new List<long>();

            var consultants = await consultantProfileRepository
                .GetAll()
                .Include(x => x.CallAssignments)
                .ToListAsync();

            foreach (var consultant in consultants)
            {
                var pendingLeadsCount = consultant.CallAssignments.Count(x =>
                    x.LeadAssignmentState == LeadAssignmentState.Pending);
                var unSubmitReportLead = consultant.CallAssignments
                    .Count(x => x.ConsultantProfileId == consultant.Id &&
                                x.ReportSubmittedAt == null);

                if (pendingLeadsCount >= 20)
                {
                    excludeConsultants.Add(consultant.Id);

                    await pushNotificationService.SendAsync(
                        consultant.UserId,
                        "خطا در گرفتن شماره جدید",
                        "شما 20 شماره در حال پیگیری دارید. لطفاً ابتدا پیگیری شماره‌های فعلی را انجام دهید؛ تا آن زمان شماره جدیدی برای شما ارسال نمی‌شود.",
                        new Dictionary<string, string>
                        {
                            ["type"] = "PendingLeadLimit",
                            ["pendingCount"] = pendingLeadsCount.ToString()
                        });
                }
                if (unSubmitReportLead >= 1)
                {
                    excludeConsultants.Add(consultant.Id);
                    await pushNotificationService.SendAsync(
                       consultant.UserId,
                       "خطا در گرفتن شماره جدید",
                       "شما 1 شماره گزارش ثبت نکرده دارید. لطفاً ابتدا شماره را تماس گرفته و گزارش ثبت کنید.  تا آن زمان شماره جدیدی برای شما ارسال نمی‌شود.",
                       new Dictionary<string, string>
                       {
                           ["type"] = "UnSubmitReportLeadLimit",
                           ["ubSubmitCount"] = unSubmitReportLead.ToString()
                       });
                }
            }

            return excludeConsultants;
        }
    }
}
