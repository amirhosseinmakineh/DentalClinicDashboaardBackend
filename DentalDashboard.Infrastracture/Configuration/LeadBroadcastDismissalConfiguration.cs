using DentalDashboard.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalDashboard.Infrastracture.Configuration;

public class LeadBroadcastDismissalConfiguration : IEntityTypeConfiguration<LeadBroadcastDismissal>
{
    public void Configure(EntityTypeBuilder<LeadBroadcastDismissal> builder)
    {
        builder.HasIndex(x => new { x.LeadAssignmentId, x.ConsultantProfileId })
            .IsUnique();

        builder.HasOne(x => x.LeadAssignment)
            .WithMany()
            .HasForeignKey(x => x.LeadAssignmentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.ConsultantProfile)
            .WithMany()
            .HasForeignKey(x => x.ConsultantProfileId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
