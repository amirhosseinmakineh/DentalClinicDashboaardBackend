using DentalDashboard.Accounting.Domain.SecretarySales.Entities;
using DentalDashboard.Accounting.Domain.SecretarySales.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalDashboard.Accounting.Infrastructure.SecretarySales.Configurations;

public sealed class SecretarySaleServiceConfiguration : IEntityTypeConfiguration<SecretarySaleService>
{
    public void Configure(EntityTypeBuilder<SecretarySaleService> builder)
    {
        builder.ToTable("SecretarySaleServices");
        builder.Property(entity => entity.Title).HasMaxLength(150).IsRequired();
        builder.Property(entity => entity.Price).HasPrecision(18, 2);
        builder.Property(entity => entity.SecretaryReward).HasPrecision(18, 2);
        builder.HasIndex(entity => entity.Title).IsUnique().HasFilter("[IsDeleted] = 0");
        builder.HasQueryFilter(entity => !entity.IsDeleted);
    }
}
