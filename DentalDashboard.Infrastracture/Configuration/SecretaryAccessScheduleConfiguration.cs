using DentalDashboard.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalDashboard.Infrastracture.Configuration;

public sealed class SecretaryAccessScheduleConfiguration : IEntityTypeConfiguration<SecretaryAccessSchedule>
{
    public void Configure(EntityTypeBuilder<SecretaryAccessSchedule> builder)
    {
        builder.ToTable("SecretaryAccessSchedules");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.HasIndex(x => new { x.UserId, x.DayOfWeek }).IsUnique();
        builder.HasOne(x => x.User).WithMany(x => x.SecretaryAccessSchedules).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class SecretaryAccessPermissionConfiguration : IEntityTypeConfiguration<SecretaryAccessPermission>
{
    public void Configure(EntityTypeBuilder<SecretaryAccessPermission> builder)
    {
        builder.ToTable("SecretaryAccessPermissions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.HasIndex(x => new { x.SecretaryUserId, x.DayOfWeek, x.PermissionType }).IsUnique();
        builder.HasOne(x => x.SecretaryUser).WithMany(x => x.SecretaryAccessPermissions)
            .HasForeignKey(x => x.SecretaryUserId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class SecretaryAccessScheduleAuditConfiguration : IEntityTypeConfiguration<SecretaryAccessScheduleAudit>
{
    public void Configure(EntityTypeBuilder<SecretaryAccessScheduleAudit> builder)
    {
        builder.ToTable("SecretaryAccessScheduleAudits");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.OldDays).HasMaxLength(100);
        builder.Property(x => x.NewDays).HasMaxLength(100);
        builder.HasIndex(x => x.SecretaryUserId);
    }
}
