using DentalDashboard.ApplicationService.Contract.Requests.Consultant.Queries;
using DentalDashboard.ApplicationService.Contract.Responses.ConsultantResponse;
using DentalDashboard.Domain.IDomainService;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.ApplicationService.Handlers.QueryHandlers.Consultant
{
    public class GetConsultantDashboardStatusQueryHandler : IQueryHandler<GetConsultantDashboardStatusQuery, ConsultantDashboardStatusResponse>
    {
        private readonly IConsultantProfileRepository consultantProfileRepository;
        private readonly ILeadAssignmentRepository leadAssignmentRepository;
        private readonly ILeadDomainService leadDomainService;

        public GetConsultantDashboardStatusQueryHandler(
            IConsultantProfileRepository consultantProfileRepository,
            ILeadAssignmentRepository leadAssignmentRepository,
            ILeadDomainService leadDomainService)
        {
            this.consultantProfileRepository = consultantProfileRepository;
            this.leadAssignmentRepository = leadAssignmentRepository;
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

            var hasActiveRealTimeLead = await leadAssignmentRepository.HasActiveRealTimeLeadAsync(profile.Id);
            var isWorkingTime = leadDomainService.IsWorkingTime(DateTime.Now);

            var canGoOnline = profile.IsCompleteProfile &&
                              profile.IsAvailable &&
                              !hasActiveRealTimeLead &&
                              isWorkingTime;

            return new ConsultantDashboardStatusResponse
            {
                ProfileId = profile.Id,
                IsAvailable = profile.IsAvailable,
                IsOnline = profile.IsOnline,
                LastOnlineAt = profile.LastOnlineAt,
                LastOfflineAt = profile.LastOfflineAt,
                CanGoOnline = canGoOnline,
                OnlineStatusBlockReason = ResolveOnlineStatusBlockReason(
                    profile.IsCompleteProfile,
                    profile.IsAvailable,
                    hasActiveRealTimeLead,
                    isWorkingTime)
            };
        }

        private static string? ResolveOnlineStatusBlockReason(
            bool isCompleteProfile,
            bool isAvailable,
            bool hasActiveRealTimeLead,
            bool isWorkingTime)
        {
            if (!isWorkingTime)
                return "امکان آنلاین شدن فقط بین ساعت ۹ صبح تا ۹ شب وجود دارد";

            if (!isCompleteProfile)
                return "پروفایل مشاور کامل نیست";

            if (!isAvailable)
                return "ابتدا حضور خود را ثبت کنید";

            if (hasActiveRealTimeLead)
                return "ابتدا تکلیف لید لحظه‌ای قبلی را مشخص کنید";

            return null;
        }
    }
}
