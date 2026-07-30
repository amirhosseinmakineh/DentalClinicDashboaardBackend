using DentalDashboard.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalDashboard.Infrastracture.Configuration;

public class ReservationFollowUpConfiguration : IEntityTypeConfiguration<ReservationFollowUp>
{
    public void Configure(EntityTypeBuilder<ReservationFollowUp> builder)
    {
        builder.Property(x => x.Reason).HasMaxLength(1000).IsRequired();
        builder.HasIndex(x => new { x.Status, x.ScheduledAt });
        builder.HasIndex(x => new { x.Status, x.ReminderAt });
        builder.HasOne(x => x.Reservation).WithMany(x => x.FollowUps)
            .HasForeignKey(x => x.ReservationId).OnDelete(DeleteBehavior.Cascade);
    }
}
