using DentalDashboard.ApplicationService.Contract.IServices;
using DentalDashboard.Domain.Enums;
using DentalDashboard.Domain.IDomainService;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Domain.Models;
using DentalDashboard.Infrastracture.Repository;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Text.RegularExpressions;

namespace DentalDashboard.ApplicationService.Services
{
    public class LeadAssignmentService : ILeadAssignmentService
    {
        private readonly HttpClient httpClient;
        private static readonly TimeSpan RealtimeLeadRedispatchInterval = TimeSpan.FromSeconds(6);
        private const string GoogleSheetUrl =
            "https://docs.google.com/spreadsheets/d/1VvgKqW-53obpDHR-b1bHRVvW2VXjHj0cjXcDsxve2w8/export?format=xlsx&gid=1527887863";
        private const string FullNameHeader = "نام و نام خانوادگی";
        private const string PhoneNumberHeader = "شماره تماس";
        private static readonly Regex IranianMobileRegex =
            new("^09\\d{9}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        private readonly ILeadAssignmentRepository leadAssignmentRepository;
        private readonly ILeadDomainService leadDomainService;
        private readonly IConsultantProfileRepository consultantProfileRepository;
        private readonly ILeadAssignmentLimitService leadAssignmentLimitService;
        private readonly IPushNotificationService pushNotificationService;
        private readonly IServiceLogRepository serviceLogRepository;
        private readonly ILeadAssignmentCandidateProvider candidateProvider;

        public LeadAssignmentService(
            HttpClient httpClient,
            ILeadAssignmentRepository leadAssignmentRepository,
            ILeadDomainService leadDomainService,
            IConsultantProfileRepository consultantProfileRepository,
            ILeadAssignmentLimitService leadAssignmentLimitService,
            IPushNotificationService pushNotificationService,
            IServiceLogRepository serviceLogRepository,
            ILeadAssignmentCandidateProvider candidateProvider)
        {
            this.httpClient = httpClient;
            this.leadAssignmentRepository = leadAssignmentRepository;
            this.leadDomainService = leadDomainService;
            this.consultantProfileRepository = consultantProfileRepository;
            this.leadAssignmentLimitService = leadAssignmentLimitService;
            this.pushNotificationService = pushNotificationService;
            this.serviceLogRepository = serviceLogRepository;
            this.candidateProvider = candidateProvider;
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
                    GoogleSheetUrl,
                    HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken);

                var log = new ServiceLog()
                {
                    CreatedAt = DateTime.UtcNow,
                    DeletedAt = null,
                    LogName = "GoogleSheetsLeadCapture",
                    ResponseLog = response.ReasonPhrase
                };
                await serviceLogRepository.AddAsync(log);
                await serviceLogRepository.SaveChange();

                response.EnsureSuccessStatusCode();

                await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                using var workbook = new XLWorkbook(stream);
                var worksheet = workbook.Worksheets.FirstOrDefault();
                if (worksheet is null)
                    return Array.Empty<LeadAssignment>();

                const int headerRowNumber = 2;
                var headerColumns = worksheet.Row(headerRowNumber)
                    .CellsUsed()
                    .Where(cell => !string.IsNullOrWhiteSpace(cell.GetString()))
                    .ToDictionary(
                        cell => Clean(cell.GetString()),
                        cell => cell.Address.ColumnNumber,
                        StringComparer.Ordinal);

                if (!headerColumns.TryGetValue(FullNameHeader, out var fullNameColumn) ||
                    !headerColumns.TryGetValue(PhoneNumberHeader, out var phoneNumberColumn))
                {
                    throw new InvalidDataException(
                        $"Google Sheet must contain '{FullNameHeader}' and '{PhoneNumberHeader}' headers in row {headerRowNumber}.");
                }

                var lastRowNumber = worksheet.LastRowUsed()?.RowNumber() ?? headerRowNumber;
                var leadsByPhoneNumber = new Dictionary<string, LeadAssignment>(StringComparer.Ordinal);

                for (var rowNumber = headerRowNumber + 1; rowNumber <= lastRowNumber; rowNumber++)
                {
                    var userName = Clean(worksheet.Cell(rowNumber, fullNameColumn).GetString());
                    var phoneNumber = NormalizePhoneNumber(
                        worksheet.Cell(rowNumber, phoneNumberColumn).GetFormattedString());

                    if (string.IsNullOrWhiteSpace(userName) ||
                        !IranianMobileRegex.IsMatch(phoneNumber) ||
                        leadsByPhoneNumber.ContainsKey(phoneNumber))
                    {
                        continue;
                    }

                    leadsByPhoneNumber.Add(phoneNumber, new LeadAssignment
                    {
                        UserName = userName,
                        PhoneNumber = phoneNumber,
                        CreatedAt = DateTime.Now
                    });
                }

                return leadsByPhoneNumber.Values.ToArray();
            }
            catch (TaskCanceledException)
            {
                return Array.Empty<LeadAssignment>();
            }
            catch (HttpRequestException)
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

        private static string NormalizePhoneNumber(string value)
        {
            var normalized = Clean(value)
                .Replace('۰', '0').Replace('۱', '1').Replace('۲', '2')
                .Replace('۳', '3').Replace('۴', '4').Replace('۵', '5')
                .Replace('۶', '6').Replace('۷', '7').Replace('۸', '8').Replace('۹', '9')
                .Replace('٠', '0').Replace('١', '1').Replace('٢', '2')
                .Replace('٣', '3').Replace('٤', '4').Replace('٥', '5')
                .Replace('٦', '6').Replace('٧', '7').Replace('٨', '8').Replace('٩', '9')
                .Replace(" ", string.Empty)
                .Replace("-", string.Empty)
                .Replace("(", string.Empty)
                .Replace(")", string.Empty);

            if (normalized.StartsWith("+98", StringComparison.Ordinal))
                normalized = $"0{normalized[3..]}";
            else if (normalized.StartsWith("0098", StringComparison.Ordinal))
                normalized = $"0{normalized[4..]}";
            else if (normalized.StartsWith("98", StringComparison.Ordinal) && normalized.Length == 12)
                normalized = $"0{normalized[2..]}";

            return normalized;
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

            var candidate = await candidateProvider.GetCurrentForDispatchAsync(RealtimeLeadRedispatchInterval);
            var lead = candidate.Lead;

            if (lead == null)
            {
                return;
            }

            var isReminder = lead.NotificationSent && lead.LastDispatchAt.HasValue;

            await NotifyConsultantsForRealtimeLeadAsync(lead, availableConsultants, candidate.SourceType, isReminder);

            lead.NotificationSent = true;
            lead.LastDispatchAt = DateTime.UtcNow;

            await leadAssignmentRepository.SaveChange();

        }

        private async Task NotifyConsultantsForRealtimeLeadAsync(
            LeadAssignment lead,
            IReadOnlyList<ConsultantProfile> consultants,
            LeadAssignmentSourceType sourceType,
            bool isReminder = false)
        {
            var (title, body) = BuildRealtimeLeadNotificationContent(lead, sourceType, isReminder);

            foreach (var consultant in consultants)
            {
                await pushNotificationService.SendAsync(
                    consultant.UserId,
                    title,
                    body,
                    new Dictionary<string, string>
                    {
                        ["leadId"] = lead.Id.ToString(),
                        ["type"] = "RealtimeLead",
                        ["leadLimitType"] = sourceType == LeadAssignmentSourceType.BurnedLeads ? "Burnt" : "Realtime",
                        ["userName"] = lead.UserName ?? string.Empty,
                        ["phoneNumber"] = lead.PhoneNumber ?? string.Empty,
                        ["isReminder"] = isReminder ? "true" : "false",
                    });
            }

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

            var leadTitle = sourceType == LeadAssignmentSourceType.BurnedLeads ? "شماره سوخته" : "لید جدید";
            var title = isReminder ? $"یادآوری {leadTitle}: {name}" : $"{leadTitle}: {name}";
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

            var candidate = await candidateProvider.GetCurrentForDispatchAsync(RealtimeLeadRedispatchInterval);
            var lead = candidate.Lead;

            if (lead == null)
            {
                return;
            }

            var isReminder = lead.NotificationSent && lead.LastDispatchAt.HasValue;

            await NotifyConsultantsForRealtimeLeadAsync(lead, availableConsultants, candidate.SourceType, isReminder);

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

            var candidate = await candidateProvider.GetCurrentForDispatchAsync(RealtimeLeadRedispatchInterval);
            var lead = candidate.Lead;

            if (lead == null)
            {
                return;
            }

            var isReminder = lead.NotificationSent && lead.LastDispatchAt.HasValue;

            await NotifyConsultantsForRealtimeLeadAsync(lead, availableConsultants, candidate.SourceType, isReminder);

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

            var candidate = await candidateProvider.GetCurrentForDispatchAsync(RealtimeLeadRedispatchInterval);
            var lead = candidate.Lead;

            if (lead == null)
            {
                return;
            }

            var isReminder = lead.NotificationSent && lead.LastDispatchAt.HasValue;

            await NotifyConsultantsForRealtimeLeadAsync(lead, availableConsultants, candidate.SourceType, isReminder);

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
