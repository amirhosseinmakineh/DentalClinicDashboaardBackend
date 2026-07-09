using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Domain.Models;
using DentalDashboard.Infrastracture.Context;

namespace DentalDashboard.Infrastracture.Repository
{
    public class PushSubscriptionRepository :  BaseRepository<long, PushSubscription>, IPushSubscriptionRepository
    {
        public PushSubscriptionRepository(DentalContext context) : base(context)
        {
        }

        public async Task<IQueryable<PushSubscription>> GetByUserIdAsync(Guid userId,CancellationToken cancellationToken = default)
        {
            var result = context.PushSubscriptions.Where(x => x.UserId == userId && !x.IsDeleted);
            return result;
        }

    }
}
