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

            builder.HasIndex(x => new
            {
                x.AssignmentType,
                x.LeadAssignmentState,
                x.ConsultantProfileId
            });

            builder.HasIndex(x => x.CallDeadlineAt);
        }
    }
}
