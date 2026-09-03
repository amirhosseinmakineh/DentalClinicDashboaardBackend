using DentalDashboard.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalDashboard.Infrastracture.Configuration;

public sealed class LeadAssignmentHistoryConfiguration : IEntityTypeConfiguration<LeadAssignmentHistory>
{
    public void Configure(EntityTypeBuilder<LeadAssignmentHistory> builder)
    {
        builder.HasIndex(x => new { x.LeadAssignmentId, x.AssignedAt });

        builder.HasOne(x => x.LeadAssignment)
            .WithMany()
            .HasForeignKey(x => x.LeadAssignmentId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.PreviousConsultantProfile)
            .WithMany()
            .HasForeignKey(x => x.PreviousConsultantProfileId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.NewConsultantProfile)
            .WithMany()
            .HasForeignKey(x => x.NewConsultantProfileId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
