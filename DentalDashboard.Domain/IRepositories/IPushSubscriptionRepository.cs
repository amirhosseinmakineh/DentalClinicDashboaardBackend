using DentalDashboard.Domain.Models;
using DentalDashboard.Framwork.IRepositories;

namespace DentalDashboard.Domain.IRepositories
{
    public interface IPushSubscriptionRepository : IBaseRepository<long,PushSubscription>
    {
        Task<IQueryable<PushSubscription>> GetByUserIdAsync(Guid userId,CancellationToken cancellationToken = default);
    }
}
