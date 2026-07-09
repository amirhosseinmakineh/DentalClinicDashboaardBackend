using DentalDashboard.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalDashboard.Infrastracture.Configuration
{
    public class PushSubscriptionConfiguration : IEntityTypeConfiguration<PushSubscription>
    {
        public void Configure(
    EntityTypeBuilder<PushSubscription> builder)
        {
            builder.ToTable("PushSubscriptions");


            builder.HasKey(x => x.Id);


            builder.Property(x => x.Endpoint)
                .IsRequired()
                .HasMaxLength(500);


            builder.Property(x => x.P256dh)
                .IsRequired()
                .HasMaxLength(500);


            builder.Property(x => x.Auth)
                .IsRequired()
                .HasMaxLength(500);


            builder.HasOne<User>()
                .WithMany(x => x.PushSubscriptions)
                .HasForeignKey(x => x.UserId);
        }
    }
}
