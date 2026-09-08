using DentalDashboard.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalDashboard.Infrastracture.Configuration
{
    public class ConsultantProfileConfiguration : IEntityTypeConfiguration<ConsultantProfile>
    {
        public void Configure(EntityTypeBuilder<ConsultantProfile> builder)
        {
            builder.ToTable("ConsultantProfiles", table =>
                table.HasCheckConstraint(
                    "CK_ConsultantProfiles_PreferredLeadSourceType",
                    "[PreferredLeadSourceType] IS NULL OR [PreferredLeadSourceType] IN (1, 2)"));
        }
    }
}
