using DentalDashboard.Domain.Models;
using DentalDashboard.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalDashboard.Infrastracture.Configuration
{
    public class ConsultantProfileConfiguration : IEntityTypeConfiguration<ConsultantProfile>
    {
        public void Configure(EntityTypeBuilder<ConsultantProfile> builder)
        {
            builder.Property(x => x.ConsultantLevel)
                .HasConversion<byte>()
                .HasDefaultValue(ConsultantLevel.Seller);

            builder.ToTable(table => table.HasCheckConstraint(
                "CK_ConsultantProfiles_ConsultantLevel",
                "[ConsultantLevel] IN (1, 2, 3)"));
        }
    }
}
