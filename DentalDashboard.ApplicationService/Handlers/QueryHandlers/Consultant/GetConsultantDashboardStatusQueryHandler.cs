using DentalDashboard.ApplicationService.Contract.Requests.Consultant.Queries;
using DentalDashboard.ApplicationService.Contract.Responses.ConsultantResponse;
using DentalDashboard.Domain.IDomainService;
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

        public GetConsultantDashboardStatusQueryHandler(
            IConsultantProfileRepository consultantProfileRepository,
            IReservationRepository reservationRepository,
            ILeadDomainService leadDomainService)
        {
            this.consultantProfileRepository = consultantProfileRepository;
            this.reservationRepository = reservationRepository;
            this.leadDomainService = leadDomainService;
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

            var canGoOnline = !isAfterWorkEnd;
            var (todayStartUtc, todayEndUtc) = IranTimeHelper.GetIranDayRangeAsUtc(IranTimeHelper.TodayInIran());
            var todayReservationsCount = await reservationRepository.GetAll()
                .AsNoTracking()
                .CountAsync(
                    x => !x.IsDeleted &&
                         !x.IsCanceled &&
                         x.ConsultantProfileId == profile.Id &&
                         x.CreatedAt >= todayStartUtc &&
                         x.CreatedAt < todayEndUtc,
                    cancellationToken);

            return new ConsultantDashboardStatusResponse
            {
                ProfileId = profile.Id,
                IsAvailable = profile.IsAvailable,
                IsOnline = profile.IsOnline,
                LastOnlineAt = profile.LastOnlineAt,
                LastOfflineAt = profile.LastOfflineAt,
                CanGoOnline = canGoOnline,
                OnlineStatusBlockReason = ResolveOnlineStatusBlockReason(isAfterWorkEnd),
                TodayReservationsCount = todayReservationsCount
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
