using DentalDashboard.Domain.Enums;
using DentalDashboard.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalDashboard.Infrastracture.Configuration;

public class ReservationTimeChangeConfiguration : IEntityTypeConfiguration<ReservationTimeChange>
{
    public void Configure(EntityTypeBuilder<ReservationTimeChange> builder)
    {
        builder.Property(x => x.Note).HasMaxLength(1000);
        builder.HasIndex(x => x.ReservationId)
            .IsUnique()
            .HasFilter($"[Status] = {(int)ReservationTimeChangeStatus.PendingConsultantConfirmation}")
            .HasDatabaseName("UX_ReservationTimeChanges_ReservationId_Pending");
        builder.HasOne(x => x.Reservation).WithMany(x => x.ReservationTimeChanges).HasForeignKey(x => x.ReservationId).OnDelete(DeleteBehavior.Cascade);
    }
}
