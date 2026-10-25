using DentalDashboard.Domain.Secretary.Accountant.SecretarySales.Entities;
using DentalDashboard.Domain.Secretary.Accountant.SecretarySales.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalDashboard.Infrastracture.Secretary.Accountant.SecretarySales.Configurations;

public sealed class SecretarySaleServiceConfiguration : IEntityTypeConfiguration<SecretarySaleService>
{
    public void Configure(EntityTypeBuilder<SecretarySaleService> builder)
    {
        builder.ToTable("SecretarySaleServices");
        builder.Property(x => x.Title).HasMaxLength(150).IsRequired();
        builder.Property(x => x.Price).HasPrecision(18, 2);
        builder.Property(x => x.SecretaryReward).HasPrecision(18, 2);
        builder.HasIndex(x => x.Title).IsUnique().HasFilter("[IsDeleted] = 0");
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

public sealed class SecretarySaleConfiguration : IEntityTypeConfiguration<SecretarySale>
{
    public void Configure(EntityTypeBuilder<SecretarySale> builder)
    {
        builder.ToTable("SecretarySales", table =>
        {
            table.HasCheckConstraint("CK_SecretarySales_SalePrice", "[SalePrice] > 0");
            table.HasCheckConstraint("CK_SecretarySales_SecretaryReward", "[SecretaryReward] > 0");
        });
        builder.Property(x => x.SalePrice).HasPrecision(18, 2);
        builder.Property(x => x.SecretaryReward).HasPrecision(18, 2);
        builder.HasIndex(x => new { x.Status, x.CreatedAt });
        builder.HasIndex(x => new { x.SecretaryUserId, x.CreatedAt });
        builder.HasIndex(x => x.PatientUserId);
        builder.HasIndex(x => x.ServiceId);
        builder.HasOne(x => x.SecretaryUser).WithMany().HasForeignKey(x => x.SecretaryUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.PatientUser).WithMany().HasForeignKey(x => x.PatientUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ReviewedByAdmin).WithMany().HasForeignKey(x => x.ReviewedByAdminId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Service).WithMany(x => x.Sales).HasForeignKey(x => x.ServiceId).OnDelete(DeleteBehavior.Restrict);
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

public sealed class SecretaryWalletConfiguration : IEntityTypeConfiguration<SecretaryWallet>
{
    public void Configure(EntityTypeBuilder<SecretaryWallet> builder)
    {
        builder.ToTable("SecretaryWallets", table =>
            table.HasCheckConstraint("CK_SecretaryWallets_Balance", "[Balance] >= 0"));
        builder.Property(x => x.Balance).HasPrecision(18, 2);
        builder.HasIndex(x => x.SecretaryUserId).IsUnique().HasFilter("[IsDeleted] = 0");
        builder.HasOne(x => x.SecretaryUser).WithMany().HasForeignKey(x => x.SecretaryUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

public sealed class SecretaryWalletTransactionConfiguration : IEntityTypeConfiguration<SecretaryWalletTransaction>
{
    public void Configure(EntityTypeBuilder<SecretaryWalletTransaction> builder)
    {
        builder.ToTable("SecretaryWalletTransactions", table =>
            table.HasCheckConstraint("CK_SecretaryWalletTransactions_Amount", "[Amount] <> 0"));
        builder.Property(x => x.Amount).HasPrecision(18, 2);
        builder.Property(x => x.Description).HasMaxLength(500).IsRequired();
        builder.HasIndex(x => new { x.SecretaryUserId, x.CreatedAt });
        builder.HasIndex(x => new { x.SecretarySaleId, x.TransactionType })
            .IsUnique()
            .HasFilter($"[SecretarySaleId] IS NOT NULL AND [TransactionType] = {(int)SecretaryWalletTransactionType.SaleReward}");
        builder.HasOne(x => x.Wallet).WithMany(x => x.Transactions).HasForeignKey(x => x.WalletId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.SecretaryUser).WithMany().HasForeignKey(x => x.SecretaryUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.SecretarySale).WithMany(x => x.WalletTransactions).HasForeignKey(x => x.SecretarySaleId).OnDelete(DeleteBehavior.Restrict);
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
