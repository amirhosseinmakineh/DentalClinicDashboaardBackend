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
        private readonly ILeadDomainService leadDomainService;

        public GetConsultantDashboardStatusQueryHandler(
            IConsultantProfileRepository consultantProfileRepository,
            ILeadDomainService leadDomainService)
        {
            this.consultantProfileRepository = consultantProfileRepository;
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

            return new ConsultantDashboardStatusResponse
            {
                ProfileId = profile.Id,
                IsAvailable = profile.IsAvailable,
                IsOnline = profile.IsOnline,
                LastOnlineAt = profile.LastOnlineAt,
                LastOfflineAt = profile.LastOfflineAt,
                CanGoOnline = canGoOnline,
                OnlineStatusBlockReason = ResolveOnlineStatusBlockReason(isAfterWorkEnd)
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
