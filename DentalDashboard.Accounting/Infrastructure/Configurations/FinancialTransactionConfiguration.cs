using DentalDashboard.Domain.Secretary.Accountant.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalDashboard.Infrastracture.Secretary.Accountant.Configurations;

public sealed class FinancialTransactionConfiguration : IEntityTypeConfiguration<FinancialTransaction>
{
    public void Configure(EntityTypeBuilder<FinancialTransaction> builder)
    {
        builder.ToTable("FinancialTransactions");
        builder.Property(entity => entity.Amount).HasPrecision(18, 2);
        builder.Property(entity => entity.Subject).HasMaxLength(200);
        builder.Property(entity => entity.CounterpartyName).HasMaxLength(200);
        builder.Property(entity => entity.Description).HasMaxLength(1000);
        builder.HasIndex(entity => entity.TransactionDate);
        builder.HasIndex(entity => new { entity.Type, entity.ExpenseCategoryId });
        builder.HasOne(entity => entity.ExpenseCategory)
            .WithMany(entity => entity.FinancialTransactions)
            .HasForeignKey(entity => entity.ExpenseCategoryId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(entity => entity.CreatedByUser)
            .WithMany()
            .HasForeignKey(entity => entity.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
