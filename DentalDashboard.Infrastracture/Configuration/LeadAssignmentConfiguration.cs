using DentalDashboard.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalDashboard.Infrastracture.Configuration
{
    public class LeadAssignmentConfiguration : IEntityTypeConfiguration<LeadAssignment>
    {
        public void Configure(EntityTypeBuilder<LeadAssignment> builder)
        {
            builder.HasIndex(x => x.PhoneNumber);

            builder.HasIndex(x => new
            {
                x.AssignmentType,
                x.LeadAssignmentState,
                x.ConsultantProfileId
            });

            builder.HasIndex(x => x.CallDeadlineAt);
            builder.HasIndex(x => x.ReportSubmittedAt);
            builder.HasIndex(x => x.BroadcastExpiresAt);

            builder.Property(x => x.PatientCity).HasMaxLength(100);
            builder.Property(x => x.PatientRegion).HasMaxLength(100);
            builder.Property(x => x.BusinessName).HasMaxLength(200);
            builder.Property(x => x.SecondaryPhoneNumber).HasMaxLength(20);
        }
    }
}
