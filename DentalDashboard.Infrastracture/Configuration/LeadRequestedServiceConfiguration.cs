using DentalDashboard.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalDashboard.Infrastracture.Configuration;

public sealed class LeadRequestedServiceConfiguration : IEntityTypeConfiguration<LeadRequestedService>
{
    public void Configure(EntityTypeBuilder<LeadRequestedService> builder)
    {
        builder.HasKey(x => new { x.LeadAssignmentId, x.ServiceId });
        builder.HasOne(x => x.LeadAssignment).WithMany(x => x.RequestedServices)
            .HasForeignKey(x => x.LeadAssignmentId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Service).WithMany()
            .HasForeignKey(x => x.ServiceId).OnDelete(DeleteBehavior.Restrict);
    }
}
