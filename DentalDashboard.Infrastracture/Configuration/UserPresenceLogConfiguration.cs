using DentalDashboard.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalDashboard.Infrastracture.Configuration;

public class UserPresenceLogConfiguration : IEntityTypeConfiguration<UserPresenceLog>
{
    public void Configure(EntityTypeBuilder<UserPresenceLog> builder)
    {
        builder.ToTable("UserPresenceLogs");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Description)
            .HasMaxLength(500);

        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.OccurredAt);
        builder.HasIndex(x => new { x.UserId, x.OccurredAt });

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
