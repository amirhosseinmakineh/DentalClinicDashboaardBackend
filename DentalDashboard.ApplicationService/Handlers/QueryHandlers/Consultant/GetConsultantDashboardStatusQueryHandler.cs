using DentalDashboard.ApplicationService.Contract.Requests.Consultant.Queries;
using DentalDashboard.ApplicationService.Contract.Responses.ConsultantResponse;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.ApplicationService.Handlers.QueryHandlers.Consultant
{
    public class GetConsultantDashboardStatusQueryHandler : IQueryHandler<GetConsultantDashboardStatusQuery, ConsultantDashboardStatusResponse>
    {
        private readonly IConsultantProfileRepository consultantProfileRepository;
        private readonly ILeadAssignmentRepository leadAssignmentRepository;

        public GetConsultantDashboardStatusQueryHandler(
            IConsultantProfileRepository consultantProfileRepository,
            ILeadAssignmentRepository leadAssignmentRepository)
        {
            this.consultantProfileRepository = consultantProfileRepository;
            this.leadAssignmentRepository = leadAssignmentRepository;
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

            var pendingOfflineLeadCount = await leadAssignmentRepository
                .CountPendingOfflineLeadsAsync(profile.Id);

            var hasActiveRealTimeLead = await leadAssignmentRepository.HasActiveRealTimeLeadAsync(profile.Id);

            var canGoOnline = profile.IsCompleteProfile &&
                              profile.IsAvailable &&
                              pendingOfflineLeadCount == 0 &&
                              !hasActiveRealTimeLead;

            return new ConsultantDashboardStatusResponse
            {
                ProfileId = profile.Id,
                IsAvailable = profile.IsAvailable,
                IsOnline = profile.IsOnline,
                LastOnlineAt = profile.LastOnlineAt,
                LastOfflineAt = profile.LastOfflineAt,
                PendingOfflineLeadCount = pendingOfflineLeadCount,
                CurrentScore = profile.CurrentScore,
                CanGoOnline = canGoOnline,
                OnlineStatusBlockReason = ResolveOnlineStatusBlockReason(
                    profile.IsCompleteProfile,
                    profile.IsAvailable,
                    pendingOfflineLeadCount,
                    hasActiveRealTimeLead)
            };
        }

        private static string? ResolveOnlineStatusBlockReason(
            bool isCompleteProfile,
            bool isAvailable,
            int pendingOfflineLeadCount,
            bool hasActiveRealTimeLead)
        {
            if (!isCompleteProfile)
                return "پروفایل مشاور کامل نیست";

            if (!isAvailable)
                return "ابتدا حضور خود را ثبت کنید";

            if (pendingOfflineLeadCount > 0)
                return "ابتدا لیدهای آفلاین خود را تعیین تکلیف کنید";

            if (hasActiveRealTimeLead)
                return "ابتدا تکلیف لید لحظه‌ای قبلی را مشخص کنید";

            return null;
        }
    }
}
