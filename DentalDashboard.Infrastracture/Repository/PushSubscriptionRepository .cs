using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Domain.Models;
using DentalDashboard.Infrastracture.Context;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.Infrastracture.Repository
{
    public class PushSubscriptionRepository : BaseRepository<long, PushSubscription>, IPushSubscriptionRepository
    {
        public PushSubscriptionRepository(DentalContext context) : base(context)
        {
        }

        public Task<List<PushSubscription>> GetActiveByUserIdAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            return context.PushSubscriptions
                .Where(x => x.UserId == userId && !x.IsDeleted)
                .ToListAsync(cancellationToken);
        }

        public async Task<PushSubscription> UpsertAsync(
            Guid userId,
            string endpoint,
            string p256dh,
            string auth,
            CancellationToken cancellationToken = default)
        {
            var existing = await context.PushSubscriptions
                .FirstOrDefaultAsync(
                    x => x.UserId == userId &&
                         x.Endpoint == endpoint &&
                         !x.IsDeleted,
                    cancellationToken);

            var now = DateTime.UtcNow;
            if (existing != null)
            {
                existing.P256dh = p256dh;
                existing.Auth = auth;
                existing.UpdatedAt = now;
                context.PushSubscriptions.Update(existing);
                return existing;
            }

            var created = new PushSubscription
            {
                UserId = userId,
                Endpoint = endpoint,
                P256dh = p256dh,
                Auth = auth,
                CreatedAt = now,
                UpdatedAt = now,
                IsDeleted = false,
            };

            await context.PushSubscriptions.AddAsync(created, cancellationToken);
            return created;
        }

        public async Task DeactivateAllByUserIdAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;
            var subscriptions = await context.PushSubscriptions
                .Where(x => x.UserId == userId && !x.IsDeleted)
                .ToListAsync(cancellationToken);

            if (subscriptions.Count == 0)
                return;

            foreach (var subscription in subscriptions)
            {
                subscription.IsDeleted = true;
                subscription.UpdatedAt = now;
            }

            context.PushSubscriptions.UpdateRange(subscriptions);
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}
