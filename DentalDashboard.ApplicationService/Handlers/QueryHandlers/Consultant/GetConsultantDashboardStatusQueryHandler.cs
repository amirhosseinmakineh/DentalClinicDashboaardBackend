using DentalDashboard.ApplicationService.Contract.Requests.Consultant.Queries;
using DentalDashboard.ApplicationService.Contract.Responses.ConsultantResponse;
using DentalDashboard.ApplicationService.Contract.IServices;
using DentalDashboard.Domain.IDomainService;
using DentalDashboard.Domain.Enums;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using DentalDashboard.Utilities.Time;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.ApplicationService.Handlers.QueryHandlers.Consultant
{
    public class GetConsultantDashboardStatusQueryHandler : IQueryHandler<GetConsultantDashboardStatusQuery, ConsultantDashboardStatusResponse>
    {
        private readonly IConsultantProfileRepository consultantProfileRepository;
        private readonly IReservationRepository reservationRepository;
        private readonly ILeadDomainService leadDomainService;
        private readonly ILeadAssignmentRepository leadAssignmentRepository;
        private readonly ILeadAssignmentLimitService leadAssignmentLimitService;

        public GetConsultantDashboardStatusQueryHandler(
            IConsultantProfileRepository consultantProfileRepository,
            IReservationRepository reservationRepository,
            ILeadDomainService leadDomainService,
            ILeadAssignmentRepository leadAssignmentRepository,
            ILeadAssignmentLimitService leadAssignmentLimitService)
        {
            this.consultantProfileRepository = consultantProfileRepository;
            this.reservationRepository = reservationRepository;
            this.leadDomainService = leadDomainService;
            this.leadAssignmentRepository = leadAssignmentRepository;
            this.leadAssignmentLimitService = leadAssignmentLimitService;
        }

        public async Task<ConsultantDashboardStatusResponse> HandleAsync(
            GetConsultantDashboardStatusQuery query,
            CancellationToken cancellationToken = default)
        {
            var profile = await consultantProfileRepository.GetAll()
                .FirstOrDefaultAsync(x => x.Id == query.ProfileId, cancellationToken);

            if (profile == null)
                throw new InvalidOperationException("مشاوری یافت نشد");

            if (profile.IsDeleted)
                throw new InvalidOperationException("پروفایل مشاور حذف شده است");

            var isAfterWorkEnd = leadDomainService.IsAfterWorkEnd(DateTime.Now);
            var isWorkingTime = leadDomainService.IsWorkingTime(DateTime.Now);

            var canGoOnline = isWorkingTime &&
                profile.IsCompleteProfile && profile.IsAvailable;
            var pendingLeadsCount = await leadAssignmentRepository.GetAll()
                .CountAsync(x => !x.IsDeleted && x.ConsultantProfileId == profile.Id &&
                    x.LeadAssignmentState == LeadAssignmentState.Pending, cancellationToken);
            var pendingReportCount = await leadAssignmentRepository.GetAll()
                .CountAsync(x => !x.IsDeleted && x.ConsultantProfileId == profile.Id &&
                    x.ReportSubmittedAt == null, cancellationToken);
            var uncalledWithoutReportCount = await leadAssignmentRepository.GetAll()
                .CountAsync(x => !x.IsDeleted && x.ConsultantProfileId == profile.Id &&
                    x.ReportSubmittedAt == null && x.CallInitiatedAt == null, cancellationToken);
            var isNewLeadBlocked = pendingLeadsCount >= 10 || pendingReportCount > 0;
            var (todayStartUtc, todayEndUtc) = IranTimeHelper.GetIranDayRangeAsUtc(IranTimeHelper.TodayInIran());
            var activeReservations = reservationRepository.GetAll()
                .AsNoTracking()
                .Where(x => !x.IsDeleted &&
                            !x.IsCanceled &&
                            x.ConsultantProfileId == profile.Id);
            var totalReservationsCount = await activeReservations
                .CountAsync(cancellationToken);
            var totalReservedPatientsCount = await activeReservations
                .SumAsync(x => (int?)x.PatientCount, cancellationToken) ?? 0;
            var todayReservationsCount = await activeReservations
                .Where(x =>
                            x.CreatedAt >= todayStartUtc &&
                            x.CreatedAt < todayEndUtc)
                .SumAsync(x => (int?)x.PatientCount, cancellationToken) ?? 0;
            var todayCallsCount = await leadAssignmentRepository.GetTodayCallCountAsync(profile.Id);
            var dailyLimitStatus = await leadAssignmentLimitService.GetDailyLimitStatusAsync(profile.Id);

            return new ConsultantDashboardStatusResponse
            {
                ProfileId = profile.Id,
                IsAvailable = profile.IsAvailable,
                IsOnline = profile.IsOnline,
                LastOnlineAt = profile.LastOnlineAt,
                LastOfflineAt = profile.LastOfflineAt,
                CanGoOnline = canGoOnline,
                OnlineStatusBlockReason = !profile.IsCompleteProfile ? "پروفایل مشاور کامل نیست"
                    : !profile.IsAvailable ? "ابتدا حضور خود را ثبت کنید"
                    : !isWorkingTime ? ResolveOnlineStatusBlockReason(isAfterWorkEnd)
                        ?? "امکان آنلاین شدن قبل از ساعت ۹ صبح وجود ندارد"
                    : null,
                TodayReservationsCount = todayReservationsCount,
                TotalReservationsCount = totalReservationsCount,
                TotalReservedPatientsCount = totalReservedPatientsCount,
                TodayCallsCount = todayCallsCount,
                DailyLimit = dailyLimitStatus.EffectiveDailyLimit,
                TodayPickupCount = dailyLimitStatus.TodayPickupCount,
                RemainingDailyCapacity = Math.Max(
                    0,
                    dailyLimitStatus.EffectiveDailyLimit - dailyLimitStatus.TodayPickupCount),
                PendingReportCount = pendingReportCount,
                UncalledWithoutReportCount = uncalledWithoutReportCount,
                IsNewLeadBlocked = isNewLeadBlocked,
                ShouldShowWorkloadNotification = isNewLeadBlocked,
                WorkloadNotificationMessage = isNewLeadBlocked
                    ? pendingReportCount > 0 ? "ابتدا گزارش لیدهای بدون گزارش را ثبت کنید"
                        : "تعداد لیدهای در حال پیگیری به سقف مجاز رسیده است"
                    : null
            };
        }

        private static string? ResolveOnlineStatusBlockReason(bool isAfterWorkEnd)
        {
            if (isAfterWorkEnd)
                return "امکان آنلاین شدن بعد از ساعت ۹ شب وجود ندارد";

            return null;
        }
    }
}
