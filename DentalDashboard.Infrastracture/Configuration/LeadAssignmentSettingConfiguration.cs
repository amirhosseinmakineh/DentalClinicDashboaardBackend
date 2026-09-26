using DentalDashboard.Domain.Enums;
using DentalDashboard.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalDashboard.Infrastracture.Configuration;

public sealed class LeadAssignmentSettingConfiguration : IEntityTypeConfiguration<LeadAssignmentSetting>
{
    public void Configure(EntityTypeBuilder<LeadAssignmentSetting> builder)
    {
        builder.ToTable("LeadAssignmentSettings", table =>
        {
            table.HasCheckConstraint("CK_LeadAssignmentSettings_Singleton", "[Id] = 1");
            table.HasCheckConstraint("CK_LeadAssignmentSettings_SourceType", "[AssignmentSourceType] IN (1, 2)");
        });

        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.HasOne(x => x.UpdatedByAdmin)
            .WithMany()
            .HasForeignKey(x => x.UpdatedByAdminId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasData(new LeadAssignmentSetting
        {
            Id = LeadAssignmentSetting.SingletonId,
            AssignmentSourceType = LeadAssignmentSourceType.NewLeads,
            CreatedAt = new DateTime(2026, 9, 3, 0, 0, 0, DateTimeKind.Utc),
            IsDeleted = false
        });
    }
}
