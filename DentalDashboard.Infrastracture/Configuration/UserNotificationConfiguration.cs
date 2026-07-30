using DentalDashboard.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalDashboard.Infrastracture.Configuration;

public class UserNotificationConfiguration : IEntityTypeConfiguration<UserNotification>
{
    public void Configure(EntityTypeBuilder<UserNotification> builder)
    {
        builder.Property(x => x.Type).HasMaxLength(100);
        builder.Property(x => x.Title).HasMaxLength(250);
        builder.Property(x => x.Body).HasMaxLength(1000);
        builder.Property(x => x.Route).HasMaxLength(500);
        builder.HasIndex(x => new { x.UserId, x.ReadAt, x.CreatedAt });
    }
}
