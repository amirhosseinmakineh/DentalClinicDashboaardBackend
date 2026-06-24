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

            builder.Property(x => x.Description)
                .HasMaxLength(1000);

            builder.HasOne(x => x.ConsultantProfile)
                .WithMany()
                .HasForeignKey(x => x.ConsultantProfileId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.LeadAssignment)
                .WithMany()
                .HasForeignKey(x => x.LeadAssignmentId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
