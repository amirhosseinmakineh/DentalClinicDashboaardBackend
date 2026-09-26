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
                .HasMaxLength(2000);


            builder.Property(x => x.P256dh)
                .IsRequired()
                .HasMaxLength(512);


            builder.Property(x => x.Auth)
                .IsRequired()
                .HasMaxLength(256);


            builder.HasOne(x => x.User)
                .WithMany(x => x.PushSubscriptions)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
