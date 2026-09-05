using DentalDashboard.Accounting.Domain.SecretarySales.Entities;
using DentalDashboard.Accounting.Domain.SecretarySales.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalDashboard.Accounting.Infrastructure.SecretarySales.Configurations;

public sealed class SecretarySaleConfiguration : IEntityTypeConfiguration<SecretarySale>
{
    public void Configure(EntityTypeBuilder<SecretarySale> builder)
    {
        builder.ToTable("SecretarySales", table =>
        {
            table.HasCheckConstraint("CK_SecretarySales_SalePrice", "[SalePrice] > 0");
            table.HasCheckConstraint("CK_SecretarySales_SecretaryReward", "[SecretaryReward] > 0");
        });
        builder.Property(entity => entity.SalePrice).HasPrecision(18, 2);
        builder.Property(entity => entity.SecretaryReward).HasPrecision(18, 2);
        builder.HasIndex(entity => new { entity.Status, entity.CreatedAt });
        builder.HasIndex(entity => new { entity.SecretaryUserId, entity.CreatedAt });
        builder.HasIndex(entity => entity.PatientUserId);
        builder.HasIndex(entity => entity.ServiceId);
        builder.HasOne(entity => entity.SecretaryUser).WithMany().HasForeignKey(entity => entity.SecretaryUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(entity => entity.PatientUser).WithMany().HasForeignKey(entity => entity.PatientUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(entity => entity.ReviewedByAdmin).WithMany().HasForeignKey(entity => entity.ReviewedByAdminId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(entity => entity.Service).WithMany(entity => entity.Sales).HasForeignKey(entity => entity.ServiceId).OnDelete(DeleteBehavior.Restrict);
        builder.HasQueryFilter(entity => !entity.IsDeleted);
    }
}
