using DentalDashboard.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalDashboard.Infrastracture.Configuration
{
    public class ReservationConfiguration : IEntityTypeConfiguration<Reservation>
    {
        public void Configure(EntityTypeBuilder<Reservation> builder)
        {
            builder.HasIndex(x => new { x.ConsultantProfileId, x.ReservationAt, x.IsCanceled });
            builder.HasIndex(x => new { x.LeadAssignmentId, x.IsCanceled });
            builder.HasIndex(x => x.PatientUserId);
            builder.HasIndex(x => x.SecretaryAnnouncementStatus);
            builder.HasIndex(x => new { x.ReservationType, x.ReservationAt });
            builder.HasIndex(x => new { x.OwnerType, x.OwnerUserId, x.CreatedAt });

            builder.Property(x => x.Description)
                .HasMaxLength(1000);

            builder.Property(x => x.DentalServices)
                .HasColumnType("nvarchar(max)");

            builder.Property(x => x.AttendancePrediction)
                .HasMaxLength(500);

            builder.Property(x => x.ConsultantAttendanceNote)
                .HasMaxLength(1000);

            builder.Property(x => x.SecretaryReviewNote)
                .HasMaxLength(1000);

            builder.Property(x => x.SecretaryAnnouncement)
                .HasMaxLength(1000);

            builder.HasOne(x => x.ConsultantProfile)
                .WithMany()
                .HasForeignKey(x => x.ConsultantProfileId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.LeadAssignment)
                .WithMany()
                .HasForeignKey(x => x.LeadAssignmentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.PatientUser)
                .WithMany()
                .HasForeignKey(x => x.PatientUserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
