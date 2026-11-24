using DentalDashboard.Domain.Accounting;
using DentalDashboard.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalDashboard.Infrastracture.Accounting.Configurations;

public sealed class AccountantPermissionConfiguration : IEntityTypeConfiguration<AccountantPermission>
{
    public void Configure(EntityTypeBuilder<AccountantPermission> builder)
    {
        builder.ToTable("AccountantPermissions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Permission).HasMaxLength(100).IsRequired();
        builder.HasIndex(x => new { x.UserId, x.Permission }).IsUnique();
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.GrantedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
public sealed class AccountingAuditConfiguration : IEntityTypeConfiguration<AccountingAuditEntry>
{
    public void Configure(EntityTypeBuilder<AccountingAuditEntry> builder)
    {
        builder.ToTable("AccountingAuditEntries");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Operation).HasMaxLength(100).IsRequired();
        builder.Property(x => x.EntityType).HasMaxLength(100).IsRequired();
        builder.Property(x => x.EntityId).HasMaxLength(150).IsRequired();
        builder.HasIndex(x => x.OccurredAtUtc);
        builder.HasIndex(x => new { x.EntityType, x.EntityId });
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.ActorUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
