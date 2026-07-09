using DentalDashboard.ApplicationService.Contract.IServices;
using DentalDashboard.Domain.DomainServices;
using DentalDashboard.Domain.Enums;
using DentalDashboard.Domain.IDomainService;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Domain.Models;
using DentalDashboard.Utilities.Convertor;
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
                string createAt = Clean(cells[9].InnerText);

                DateTime createdAt;

                DateTime.TryParse(createAt, out createdAt);

                leads.Add(new LeadAssignment
                {
                    UserName = userName,
                    PhoneNumber = phoneNumber,
                    CreatedAt = createdAt
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
                lead.CallDeadlineAt = null;

                if (isWorkingTime)
                {
                    // ساعت کاری (09:00 تا قبل از 21:00)
                    lead.AssignmentType = LeadAssignmentType.RealTime;
                    lead.RequiresThreeMinuteCall = true;
                    lead.LeadAssignmentState = LeadAssignmentState.New;
                }
                else
                {
                    // خارج از ساعت کاری
                    lead.AssignmentType = LeadAssignmentType.OfflineQueue;
                    lead.LeadAssignmentState = LeadAssignmentState.New;
                    lead.RequiresThreeMinuteCall = false;
                }
            }

            await leadAssignmentRepository.AddRangeAsync(newLeads);
            await leadAssignmentRepository.SaveChange();

            //await ReconcileMisclassifiedLeadStatesAsync();
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

            var followUpContactedOfflineLeads = await leadAssignmentRepository.GetAll()
                .Where(x => !x.IsDeleted &&
                            x.AssignmentType == LeadAssignmentType.OfflineQueue &&
                            x.ReportSubmittedAt != null &&
                            x.LeadAssignmentState == LeadAssignmentState.Contacted &&
                            x.CallResult != null &&
                            (x.CallResult == LeadCallResult.NoAnswer ||
                             x.CallResult == LeadCallResult.NeedFollowUp ||
                             x.CallResult == LeadCallResult.Busy ||
                             x.CallResult == LeadCallResult.PatientHungUp))
                .ToListAsync();

            foreach (var lead in followUpContactedOfflineLeads)
            {
                lead.LeadAssignmentState = LeadAssignmentState.Pending;
                lead.UpdatedAt = now;
                changed = true;
            }

            var rejectedContactedOfflineLeads = await leadAssignmentRepository.GetAll()
                .Where(x => !x.IsDeleted &&
                            x.AssignmentType == LeadAssignmentType.OfflineQueue &&
                            x.ReportSubmittedAt != null &&
                            x.LeadAssignmentState == LeadAssignmentState.Contacted &&
                            x.CallResult != null &&
                            (x.CallResult == LeadCallResult.Rejected ||
                             x.CallResult == LeadCallResult.WrongNumber))
                .ToListAsync();

            foreach (var lead in rejectedContactedOfflineLeads)
            {
                lead.LeadAssignmentState = LeadAssignmentState.Rejected;
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
                                x.LeadAssignmentState == LeadAssignmentState.New)
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

            var leads = await leadAssignmentRepository.GetAll()
                .Where(x => !x.IsDeleted &&
                            x.AssignmentType == LeadAssignmentType.OfflineQueue &&
                            x.ConsultantProfileId == null &&
                            x.ReportSubmittedAt == null &&
                            x.LeadAssignmentState == LeadAssignmentState.New)
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

        public async Task AssignOfflineLeadsAsync(
      IReadOnlyCollection<long>? onlyConsultantIds = null)
        {
            var consultants = await consultantProfileRepository
                .GetAvailableConsultantsForOfflineAssignmentAsync();

            if (onlyConsultantIds is { Count: > 0 })
            {
                var consultantIds = onlyConsultantIds.ToHashSet();

                consultants = consultants
                    .Where(x => consultantIds.Contains(x.Id))
                    .ToList();

                if (!consultants.Any())
                {
                    consultants = await consultantProfileRepository.GetAll()
                        .Where(x =>
                            !x.IsDeleted &&
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
                    "AssignOfflineLeadsAsync skipped: no available consultants");

                return;
            }


            var consultantIdsForAssignment = consultants
                .Select(x => x.Id);


            var currentOfflineCounts = await leadAssignmentRepository
                .GetPendingOfflineLeadCountsAsync(consultantIdsForAssignment);


            var totalRemainingCapacity = consultants.Sum(x =>
                Math.Max(
                    OfflineLeadAssignmentStrategy.OfflineBatchSize -
                    currentOfflineCounts.GetValueOrDefault(x.Id),
                    0));


            if (totalRemainingCapacity <= 0)
            {
                logger.LogInformation(
                    "AssignOfflineLeadsAsync skipped: consultants reached offline lead capacity");

                return;
            }


            var leads = await leadAssignmentRepository
                .GetPendingOfflineLeadsAsync(totalRemainingCapacity);


            if (!leads.Any())
            {
                logger.LogInformation(
                    "AssignOfflineLeadsAsync skipped: no pending offline leads");

                return;
            }


            offlineLeadAssignmentStrategy.Assign(
                leads,
                consultants,
                currentOfflineCounts);


            await leadAssignmentRepository.SaveChange();

            var updatedOfflineCounts = await leadAssignmentRepository
                .GetPendingOfflineLeadCountsAsync(consultantIdsForAssignment);


            await SendAssignedOfflieLeadNotificationsAsync(
                consultants,
                updatedOfflineCounts);


            logger.LogInformation(
                "AssignOfflineLeadsAsync completed. Assigned {LeadCount} offline leads to {ConsultantCount} consultants",
                leads.Count(x => x.ConsultantProfileId.HasValue),
                consultants.Count);
        }

        public async Task AssignRealTimeLeadsAsync(
            IReadOnlyCollection<long>? excludedConsultantIds = null)
        {
            // 1- مشاورهای آنلاین و آماده
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
                logger.LogInformation(
                    "Realtime dispatch skipped: no online consultants");

                return;
            }


            // 2- حذف مشاورهایی که سقف 10 Pickup روزانه را پر کرده‌اند
            var availableConsultants = new List<ConsultantProfile>();

            foreach (var consultant in consultants)
            {
                var pickupCount = await leadAssignmentRepository
                    .GetTodayPickupCountAsync(consultant.Id);


                if (pickupCount < 10)
                {
                    availableConsultants.Add(consultant);
                }
            }


            if (!availableConsultants.Any())
            {
                logger.LogInformation(
                    "Realtime dispatch skipped: no consultant capacity");

                return;
            }


            // 3- گروه بندی بر اساس Score
            var consultantGroups =
                GroupConsultantsByScore(availableConsultants);


            // 4- گرفتن لیدهایی که آماده Dispatch هستند
            var leads = await leadAssignmentRepository
                .GetUnassignedRealTimeLeadsAsync(1);


            if (!leads.Any())
            {
                logger.LogInformation(
                    "Realtime dispatch skipped: no pending realtime leads");

                return;
            }


            foreach (var lead in leads)
            {
                // اگر همه گروه‌ها تست شده باشند دیگر ارسال نکن
                if (lead.DispatchLevel >= consultantGroups.Count)
                    continue;


                var targetGroup = consultantGroups[lead.DispatchLevel];


                if (!targetGroup.Any())
                {
                    lead.DispatchLevel++;
                    continue;
                }


                foreach (var consultant in targetGroup)
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


                // رفتن برای گروه بعدی بعد از 10 ثانیه
                lead.DispatchLevel++;

                lead.LastDispatchAt = DateTime.UtcNow;

                lead.NotificationSent = true;
            }


            await leadAssignmentRepository.SaveChange();


            logger.LogInformation(
                "Realtime dispatch completed. Leads: {count}",
                leads.Count);
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
                    ResetLeadQueue(lead);
                    continue;
                }

                var consultant = lead.ConsultantProfile;
                failedConsultantIds.Add(consultant.Id);

                ApplyLateCallScore(consultant, lead, now);
                ResetLeadQueue(lead);

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

        private void ResetLeadQueue(LeadAssignment lead)
        {
            var now = DateTime.Now;

            lead.ConsultantProfileId = null;
            lead.LeadAssignmentState = LeadAssignmentState.New;
            lead.AssignedAt = null;
            lead.CallDeadlineAt = null;
            lead.CallInitiatedAt = null;
            lead.NotificationSent = false;

            if (leadDomainService.IsWorkingTime(now))
            {
                lead.AssignmentType = LeadAssignmentType.RealTime;
                lead.RequiresThreeMinuteCall = true;
            }
            else
            {
                lead.AssignmentType = LeadAssignmentType.OfflineQueue;
                lead.RequiresThreeMinuteCall = false;
            }
        }
        private async Task<int> ExpireAndRequeueRealTimeLeadInternalAsync(
            LeadAssignment lead,
            ConsultantProfile consultant)
        {
            var now = DateTime.Now;
            var failedConsultantId = consultant.Id;
            var eventScore = ApplyLateCallScore(consultant, lead, now);

            ResetLeadQueue(lead);

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

        private async Task SendAssignedOfflieLeadNotificationsAsync(IReadOnlyCollection<ConsultantProfile> consultants,IReadOnlyDictionary<long, int> offlineCounts)
        {
            foreach (var consultant in consultants)
            {
                if (consultant.UserId == Guid.Empty)
                    continue;


                var count = offlineCounts
                    .GetValueOrDefault(consultant.Id);


                if (count == 0)
                    continue;


                await pushNotificationService.SendAsync(
                    consultant.UserId,
                    "لید آفلاین جدید!",
                    $"{count} لید آفلاین داری، بیا اینارو تعیین تکلیف کن",
                    new Dictionary<string, string>
                    {
                        ["type"] = "offline_leads",
                        ["count"] = count.ToString(),
                    });
            }
        }
        private List<List<ConsultantProfile>> GroupConsultantsByScore(
    List<ConsultantProfile> consultants)
        {
            return new List<List<ConsultantProfile>>
    {
        consultants
            .Where(x => x.CurrentScore > 75)
            .OrderByDescending(x => x.CurrentScore)
            .ToList(),

        consultants
            .Where(x => x.CurrentScore > 50 && x.CurrentScore <= 75)
            .OrderByDescending(x => x.CurrentScore)
            .ToList(),

        consultants
            .Where(x => x.CurrentScore >= 25 && x.CurrentScore <= 50)
            .OrderByDescending(x => x.CurrentScore)
            .ToList(),

        consultants
            .Where(x => x.CurrentScore < 25)
            .OrderByDescending(x => x.CurrentScore)
            .ToList()
    };
        }


    }
}
