using DentalDashboard.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalDashboard.Infrastracture.Configuration;

public class WalletTransactionConfiguration : IEntityTypeConfiguration<WalletTransaction>
{
    public void Configure(EntityTypeBuilder<WalletTransaction> builder)
    {
        builder.ToTable("WalletTransactions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Amount).HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.Type).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(1000);
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.HasIndex(x => new { x.WalletId, x.CreatedAt });
        builder.HasIndex(x => x.FinancialTransactionId);
        builder.HasOne(x => x.Wallet).WithMany(x => x.Transactions).HasForeignKey(x => x.WalletId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.FinancialTransaction).WithMany(x => x.WalletTransactions)
            .HasForeignKey(x => x.FinancialTransactionId).OnDelete(DeleteBehavior.Restrict);
    }
}
