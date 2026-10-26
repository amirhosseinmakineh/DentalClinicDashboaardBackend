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
            builder.Property(x => x.ConsultationNotes).HasMaxLength(1000);
            builder.HasIndex(x => x.ConsultationRequestId).IsUnique()
                .HasFilter("[ConsultationRequestId] IS NOT NULL");
            builder.HasOne(x => x.ConsultationService).WithMany()
                .HasForeignKey(x => x.ConsultationServiceId).OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => new
            {
                x.AssignmentType,
                x.LeadAssignmentState,
                x.ConsultantProfileId
            });

            // Supports the consultant's active-leads list and its AssignedAt/Id pagination.
            // Keep existing indexes intact for other lead-assignment workflows.
            builder.HasIndex(x => new
            {
                x.ConsultantProfileId,
                x.AssignedAt,
                x.Id
            })
            .HasDatabaseName("IX_LeadAssignments_Consultant_Active_AssignedAt_Id")
            .IsDescending(false, true, true)
            .HasFilter("[IsDeleted] = 0 AND [ReportSubmittedAt] IS NULL");

            builder.HasIndex(x => x.CallDeadlineAt);
            builder.HasIndex(x => x.ReportSubmittedAt);
            builder.HasIndex(x => new
            {
                x.IsDeleted,
                x.LeadAssignmentState,
                x.ConsultantProfileId,
                x.CreatedAt
            });

            builder.Property(x => x.PatientCity).HasMaxLength(100);
            builder.Property(x => x.PatientRegion).HasMaxLength(100);
            builder.Property(x => x.BusinessName).HasMaxLength(200);
            builder.Property(x => x.SecondaryPhoneNumber).HasMaxLength(20);
            builder.Property(x => x.ClosureDescription).HasMaxLength(500);
        }
    }
}
