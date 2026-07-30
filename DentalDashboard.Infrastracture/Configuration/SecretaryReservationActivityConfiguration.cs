using DentalDashboard.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalDashboard.Infrastracture.Configuration;

public class SecretaryReservationActivityConfiguration : IEntityTypeConfiguration<SecretaryReservationActivity>
{
    public void Configure(EntityTypeBuilder<SecretaryReservationActivity> builder)
    {
        builder.Property(x => x.ActivityType).HasMaxLength(80).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(1000).IsRequired();
        builder.HasIndex(x => x.CreatedAt);
        builder.HasOne(x => x.Reservation).WithMany(x => x.SecretaryActivities)
            .HasForeignKey(x => x.ReservationId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.ActorUser).WithMany().HasForeignKey(x => x.ActorUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
