using DentalDashboard.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalDashboard.Infrastracture.Configuration;

public sealed class ReservationContactLogConfiguration : IEntityTypeConfiguration<ReservationContactLog>
{
    public void Configure(EntityTypeBuilder<ReservationContactLog> builder)
    {
        builder.HasIndex(x => new { x.ReservationId, x.CreatedAt });
        builder.Property(x => x.Note).HasMaxLength(2000);
        builder.HasOne(x => x.Reservation).WithMany(x => x.ContactLogs).HasForeignKey(x => x.ReservationId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class ReservationNoteConfiguration : IEntityTypeConfiguration<ReservationNote>
{
    public void Configure(EntityTypeBuilder<ReservationNote> builder)
    {
        builder.HasIndex(x => new { x.ReservationId, x.CreatedAt });
        builder.Property(x => x.Note).HasMaxLength(2000).IsRequired();
        builder.HasOne(x => x.Reservation).WithMany(x => x.Notes).HasForeignKey(x => x.ReservationId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class ReservationNotificationOutboxConfiguration : IEntityTypeConfiguration<ReservationNotificationOutbox>
{
    public void Configure(EntityTypeBuilder<ReservationNotificationOutbox> builder)
    {
        builder.HasIndex(x => x.IdempotencyKey).IsUnique();
        builder.HasIndex(x => new { x.ProcessedAt, x.CreatedAt });
        builder.Property(x => x.EventType).HasMaxLength(100).IsRequired();
        builder.Property(x => x.IdempotencyKey).HasMaxLength(250).IsRequired();
        builder.Property(x => x.Payload).HasMaxLength(4000).IsRequired();
        builder.Property(x => x.LastError).HasMaxLength(2000);
    }
}
