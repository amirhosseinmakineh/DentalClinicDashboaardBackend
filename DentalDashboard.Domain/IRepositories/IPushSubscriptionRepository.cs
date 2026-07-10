using DentalDashboard.Domain.Models;
using DentalDashboard.Framwork.IRepositories;

namespace DentalDashboard.Domain.IRepositories
{
    public interface IPushSubscriptionRepository : IBaseRepository<long, PushSubscription>
    {
        Task<List<PushSubscription>> GetActiveByUserIdAsync(
            Guid userId,
            CancellationToken cancellationToken = default);

        Task<PushSubscription> UpsertAsync(
            Guid userId,
            string endpoint,
            string p256dh,
            string auth,
            CancellationToken cancellationToken = default);

        Task DeactivateAllByUserIdAsync(
            Guid userId,
            CancellationToken cancellationToken = default);
    }
}
