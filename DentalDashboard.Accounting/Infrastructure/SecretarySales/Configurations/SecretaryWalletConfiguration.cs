using DentalDashboard.Domain.Secretary.Accountant.SecretarySales.Entities;
using DentalDashboard.Domain.Secretary.Accountant.SecretarySales.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalDashboard.Infrastracture.Secretary.Accountant.SecretarySales.Configurations;

public sealed class SecretaryWalletConfiguration : IEntityTypeConfiguration<SecretaryWallet>
{
    public void Configure(EntityTypeBuilder<SecretaryWallet> builder)
    {
        builder.ToTable("SecretaryWallets", table =>
            table.HasCheckConstraint("CK_SecretaryWallets_Balance", "[Balance] >= 0"));
        builder.Property(entity => entity.Balance).HasPrecision(18, 2);
        builder.HasIndex(entity => entity.SecretaryUserId).IsUnique().HasFilter("[IsDeleted] = 0");
        builder.HasOne(entity => entity.SecretaryUser).WithMany().HasForeignKey(entity => entity.SecretaryUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasQueryFilter(entity => !entity.IsDeleted);
    }
}
