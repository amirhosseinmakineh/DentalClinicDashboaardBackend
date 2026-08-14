using DentalDashboard.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalDashboard.Infrastracture.Configuration;

public class ConsultantRoleEvaluationConfiguration : IEntityTypeConfiguration<ConsultantRoleEvaluation>
{
    public void Configure(EntityTypeBuilder<ConsultantRoleEvaluation> builder)
    {
        builder.HasIndex(x => new { x.ConsultantProfileId, x.PeriodStartedAt }).IsUnique();
        builder.HasOne(x => x.ConsultantProfile)
            .WithMany(x => x.RoleEvaluations)
            .HasForeignKey(x => x.ConsultantProfileId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
