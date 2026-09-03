using DentalDashboard.ApplicationService.Contract.IServices;
using DentalDashboard.Domain.Enums;
using DentalDashboard.Domain.IDomainService;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Domain.Models;
using DentalDashboard.Infrastracture.Repository;
using HtmlAgilityPack;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;

namespace DentalDashboard.ApplicationService.Services
{
    public class LeadAssignmentService : ILeadAssignmentService
    {
        private readonly HttpClient httpClient;
        private static readonly TimeSpan RealtimeLeadRedispatchInterval = TimeSpan.FromSeconds(6);
        private const string YektanetLeadReportUrlKey = "Yektanet:LeadReportUrl";
        private readonly Uri? yektanetLeadReportUri;
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
            IConfiguration configuration,
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

            var configuredUrl = configuration[YektanetLeadReportUrlKey];
            if (Uri.TryCreate(configuredUrl, UriKind.Absolute, out var reportUri) &&
                reportUri.Scheme == Uri.UriSchemeHttps)
            {
                yektanetLeadReportUri = reportUri;
            }
        }

        public async Task<LeadAssignment[]> LeadsListAsync(
          CancellationToken cancellationToken = default)
        {
            try
            {
                if (yektanetLeadReportUri is null)
                {
                    logger.LogWarning(
                        "Yektanet lead report URL is missing or invalid. Configure {ConfigurationKey}; assignment will use available database candidates",
                        YektanetLeadReportUrlKey);
                    return Array.Empty<LeadAssignment>();
                }

                if (!httpClient.DefaultRequestHeaders.UserAgent.Any())
                {
                    httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(
                        "Mozilla/5.0");
                }

                using var response = await httpClient.GetAsync(
                    yektanetLeadReportUri,
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
            catch (TaskCanceledException ex)
            {
                logger.LogWarning(ex, "Yektanet lead request timed out; assignment will use available database candidates");
                return Array.Empty<LeadAssignment>();
            }
            catch (HttpRequestException ex)
            {
                logger.LogWarning(ex, "Yektanet lead request failed; assignment will use available database candidates");
                return Array.Empty<LeadAssignment>();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected Yektanet lead import failure; assignment will use available database candidates");
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

        private async Task<int> ImportNewYektanetLeadsAsync(
            CancellationToken cancellationToken = default)
        {
            var now = DateTime.Now;
            var yektanetLeads = await LeadsListAsync(cancellationToken);

            // LeadsListAsync returns an empty array when Yektanet has no rows,
            // times out, or cannot be reached. In all of those cases the caller
            // continues with the database fallback instead of stopping assignment.
            if (yektanetLeads.Length == 0)
                return 0;

            var phoneNumbers = yektanetLeads
                .Where(x => !string.IsNullOrWhiteSpace(x.PhoneNumber))
                .Select(x => x.PhoneNumber.Trim())
                .Distinct()
                .ToList();

            if (phoneNumbers.Count == 0)
                return 0;

            var existingPhoneNumbers = await leadAssignmentRepository
                .GetExistingPhoneNumbersAsync(phoneNumbers);

            var newLeads = yektanetLeads
                .Where(x =>
                    !string.IsNullOrWhiteSpace(x.PhoneNumber) &&
                    !existingPhoneNumbers.Contains(x.PhoneNumber.Trim()))
                .GroupBy(x => x.PhoneNumber.Trim())
                .Select(x => x.First())
                .ToList();

            if (newLeads.Count == 0)
                return 0;

            foreach (var lead in newLeads)
            {
                lead.PhoneNumber = lead.PhoneNumber?.Trim();
                lead.CreatedAt = now;
                lead.CallDeadlineAt = null;
                lead.AssignmentType = LeadAssignmentType.RealTime;
                lead.RequiresThreeMinuteCall = true;
                lead.LeadAssignmentState = LeadAssignmentState.New;
            }

            await leadAssignmentRepository.AddRangeAsync(newLeads);
            await leadAssignmentRepository.SaveChange();

            logger.LogInformation(
                "Imported {LeadCount} new Yektanet leads for realtime assignment",
                newLeads.Count);

            return newLeads.Count;
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

            var candidate = await GetDispatchCandidateAsync();
            LogCandidateBatch(candidate);
            var lead = candidate.Lead;

            if (lead == null)
            {
                return;
            }

            var isReminder = lead.NotificationSent && lead.LastDispatchAt.HasValue;

            if (!await NotifyConsultantsForRealtimeLeadAsync(
                    lead,
                    availableConsultants,
                    candidate.SourceType,
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
            var leadLimitType = sourceType == LeadAssignmentSourceType.BurnedLeads
                ? "Burnt"
                : "Realtime";
            var (title, body) = BuildRealtimeLeadNotificationContent(lead, sourceType, isReminder);
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
                            ["leadLimitType"] = leadLimitType,
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

        private async Task<DispatchCandidate> GetDispatchCandidateAsync()
        {
            var candidateBatch = await candidateProvider
                .GetCurrentForDispatchAsync(RealtimeLeadRedispatchInterval);

            if (candidateBatch.Lead != null)
            {
                return new DispatchCandidate(
                    candidateBatch.Lead,
                    candidateBatch.SourceType,
                    candidateBatch.CandidateCount,
                    candidateBatch.UsedFallback);
            }

            // Burned-lead mode must not depend on Yektanet.
            // Its existing database rules are used directly.
            if (candidateBatch.SourceType == LeadAssignmentSourceType.BurnedLeads)
            {
                var burnedLead = await GetFallbackLeadAsync(candidateBatch.SourceType);

                return new DispatchCandidate(
                    burnedLead,
                    candidateBatch.SourceType,
                    burnedLead == null ? 0 : 1,
                    burnedLead != null);
            }

            // Realtime/source mode:
            // If no current candidate exists, try importing fresh Yektanet leads.
            // LeadsListAsync safely returns an empty array on timeout/failure, so
            // assignment never stops merely because Yektanet is unavailable.
            var importedLeadCount = await ImportNewYektanetLeadsAsync();

            if (importedLeadCount > 0)
            {
                candidateBatch = await candidateProvider
                    .GetCurrentForDispatchAsync(RealtimeLeadRedispatchInterval);

                if (candidateBatch.Lead != null)
                {
                    return new DispatchCandidate(
                        candidateBatch.Lead,
                        candidateBatch.SourceType,
                        candidateBatch.CandidateCount,
                        candidateBatch.UsedFallback);
                }
            }

            // Yektanet timed out, failed, contained no leads, contained only leads
            // already stored in the database, or the imported leads still produced
            // no dispatch candidate. Use an existing unassigned realtime lead.
            var fallbackLead = await GetFallbackLeadAsync(candidateBatch.SourceType);

            return new DispatchCandidate(
                fallbackLead,
                candidateBatch.SourceType,
                fallbackLead == null ? 0 : 1,
                fallbackLead != null);
        }

        private async Task<LeadAssignment?> GetFallbackLeadAsync(
            LeadAssignmentSourceType sourceType)
        {
            var redispatchBefore = DateTime.UtcNow.Subtract(RealtimeLeadRedispatchInterval);

            if (sourceType == LeadAssignmentSourceType.BurnedLeads)
            {
                return await leadAssignmentRepository
                    .GetAll()
                    .IgnoreQueryFilters()
                    .Where(x =>
                        (
                            x.IsDeleted &&
                            x.ConsultantProfileId == null
                        )
                        ||
                        (
                            !x.IsDeleted &&
                            x.LeadAssignmentState == LeadAssignmentState.Pending &&
                            x.ConsultantProfileId != null
                        ))
                    .Where(x =>
                        !x.NotificationSent ||
                        !x.LastDispatchAt.HasValue ||
                        x.LastDispatchAt <= redispatchBefore)
                    .OrderBy(x => x.CreatedAt)
                    .FirstOrDefaultAsync();
            }

            // Source/new-lead mode:
            // If the current source has no new candidate, use an existing lead
            // that has never been assigned to any consultant.
            return await leadAssignmentRepository
                .GetAll()
                .Where(x =>
                    !x.IsDeleted &&
                    x.ConsultantProfileId == null &&
                    x.AssignmentType == LeadAssignmentType.RealTime)
                .Where(x =>
                    !x.NotificationSent ||
                    !x.LastDispatchAt.HasValue ||
                    x.LastDispatchAt <= redispatchBefore)
                .OrderBy(x => x.CreatedAt)
                .FirstOrDefaultAsync();
        }

        private void LogCandidateBatch(DispatchCandidate candidate)
        {
            logger.LogInformation(
                "Lead assignment candidates selected. AssignmentSourceType: {AssignmentSourceType}, CandidateCount: {CandidateCount}, LeadId: {LeadId}, UsedFallback: {UsedFallback}",
                candidate.SourceType,
                candidate.CandidateCount,
                candidate.Lead?.Id,
                candidate.UsedFallback);
        }

        private static (string Title, string Body) BuildRealtimeLeadNotificationContent(
            LeadAssignment lead,
            LeadAssignmentSourceType sourceType,
            bool isReminder)
        {
            var name = string.IsNullOrWhiteSpace(lead.UserName)
                ? "نامشخص"
                : lead.UserName.Trim();
            var phone = string.IsNullOrWhiteSpace(lead.PhoneNumber)
                ? "نامشخص"
                : lead.PhoneNumber.Trim();

            var leadTypeTitle = sourceType == LeadAssignmentSourceType.BurnedLeads
                ? "لید سوخته"
                : "لید جدید";
            var title = isReminder
                ? $"یادآوری {leadTypeTitle}: {name}"
                : $"{leadTypeTitle}: {name}";
            var body = $"شماره تماس: {phone} — جهت دریافت روی اعلان کلیک کنید.";

            return (title, body);
        }

        public async Task NotifyRealtimeLeadTakenAsync(
            long leadAssignmentId,
            long pickedByConsultantProfileId)
        {
            var lead = await leadAssignmentRepository
                .GetAll()
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.Id == leadAssignmentId);

            if (lead != null)
            {
                var now = DateTime.Now;

                // IMPORTANT: the same LeadAssignment row is always updated on pickup.
                // For a burned Pending lead ConsultantProfileId may already contain
                // the previous consultant. Pickup intentionally overwrites that value
                // with the consultant who has just picked the lead.
                // Deleted/unassigned burned leads are restored and assigned as well.
                lead.ConsultantProfileId = pickedByConsultantProfileId;
                lead.IsDeleted = false;
                lead.LeadAssignmentState = LeadAssignmentState.Assigned;
                lead.AssignedAt = now;
                lead.UpdatedAt = now;
                lead.PickUp = true;
                lead.NotificationSent = false;
                lead.LastDispatchAt = null;

                leadAssignmentRepository.Update(lead);
                await leadAssignmentRepository.SaveChange();
            }

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

            var candidate = await GetDispatchCandidateAsync();
            LogCandidateBatch(candidate);
            var lead = candidate.Lead;

            if (lead == null)
            {
                return;
            }

            var isReminder = lead.NotificationSent && lead.LastDispatchAt.HasValue;

            if (!await NotifyConsultantsForRealtimeLeadAsync(
                    lead,
                    availableConsultants,
                    candidate.SourceType,
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

            var candidate = await GetDispatchCandidateAsync();
            LogCandidateBatch(candidate);
            var lead = candidate.Lead;

            if (lead == null)
            {
                return;
            }

            var isReminder = lead.NotificationSent && lead.LastDispatchAt.HasValue;

            if (!await NotifyConsultantsForRealtimeLeadAsync(
                    lead,
                    availableConsultants,
                    candidate.SourceType,
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

            var candidate = await GetDispatchCandidateAsync();
            LogCandidateBatch(candidate);
            var lead = candidate.Lead;

            if (lead == null)
            {
                return;
            }

            var isReminder = lead.NotificationSent && lead.LastDispatchAt.HasValue;

            if (!await NotifyConsultantsForRealtimeLeadAsync(
                    lead,
                    availableConsultants,
                    candidate.SourceType,
                    isReminder))
                return;

            lead.NotificationSent = true;
            lead.LastDispatchAt = DateTime.UtcNow;

            await leadAssignmentRepository.SaveChange();

        }
        private sealed record DispatchCandidate(
            LeadAssignment? Lead,
            LeadAssignmentSourceType SourceType,
            int CandidateCount,
            bool UsedFallback);

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
