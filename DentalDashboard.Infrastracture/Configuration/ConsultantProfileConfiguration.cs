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

            builder.HasIndex(x => new { x.ConsultantLevel, x.TestStartedAt, x.TestCompletedAt });
            builder.HasIndex(x => new { x.ConsultantLevel, x.SellerStartedAt, x.SellerEvaluatedAt });
            builder.Property(x => x.TopSellerRewardLevel)
                .HasConversion<byte>()
                .HasDefaultValue(TopSellerRewardLevel.None);
            builder.HasIndex(x => new
            {
                x.ConsultantLevel,
                x.TopSellerStartedAt,
                x.TopSellerLastEvaluatedPeriodStart
            });

            builder.ToTable(table => table.HasCheckConstraint(
                "CK_ConsultantProfiles_ConsultantLevel",
                "[ConsultantLevel] IN (1, 2, 3)"));
        }
    }
}
