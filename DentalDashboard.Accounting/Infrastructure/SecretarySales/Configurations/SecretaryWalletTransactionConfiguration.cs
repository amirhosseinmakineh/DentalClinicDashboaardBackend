using DentalDashboard.Domain.Secretary.Accountant.SecretarySales.Entities;
using DentalDashboard.Domain.Secretary.Accountant.SecretarySales.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalDashboard.Infrastracture.Secretary.Accountant.SecretarySales.Configurations;

public sealed class SecretaryWalletTransactionConfiguration : IEntityTypeConfiguration<SecretaryWalletTransaction>
{
    public void Configure(EntityTypeBuilder<SecretaryWalletTransaction> builder)
    {
        builder.ToTable("SecretaryWalletTransactions", table =>
            table.HasCheckConstraint("CK_SecretaryWalletTransactions_Amount", "[Amount] <> 0"));
        builder.Property(entity => entity.Amount).HasPrecision(18, 2);
        builder.Property(entity => entity.Description).HasMaxLength(500).IsRequired();
        builder.HasIndex(entity => new { entity.SecretaryUserId, entity.CreatedAt });
        builder.HasIndex(entity => new { entity.SecretarySaleId, entity.TransactionType })
            .IsUnique()
            .HasFilter($"[SecretarySaleId] IS NOT NULL AND [TransactionType] = {(int)SecretaryWalletTransactionType.SaleReward}");
        builder.HasOne(entity => entity.Wallet)
            .WithMany(entity => entity.Transactions)
            .HasForeignKey(entity => entity.WalletId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(entity => entity.SecretaryUser)
            .WithMany()
            .HasForeignKey(entity => entity.SecretaryUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(entity => entity.SecretarySale)
            .WithMany(entity => entity.WalletTransactions)
            .HasForeignKey(entity => entity.SecretarySaleId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasQueryFilter(entity => !entity.IsDeleted);
    }
}
