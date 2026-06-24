using DentalDashboard.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalDashboard.Infrastracture.Configuration
{
    public class ConsultantProfileConfiguration : IEntityTypeConfiguration<ConsultantProfile>
    {
        public void Configure(EntityTypeBuilder<ConsultantProfile> builder)
        {
            builder.HasIndex(x => x.CurrentScore);
        }
    }
}
