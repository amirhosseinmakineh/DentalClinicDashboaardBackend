using DentalDashboard.Domain.Secretary.Account.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalDashboard.Infrastracture.Secretary.Account.Configurations;

public sealed class FinancialTransactionConfiguration : IEntityTypeConfiguration<FinancialTransaction>
{
    public void Configure(EntityTypeBuilder<FinancialTransaction> builder)
    {
        builder.ToTable("FinancialTransactions");
        builder.Property(x => x.Amount).HasPrecision(18, 2);
        builder.Property(x => x.Subject).HasMaxLength(200);
        builder.Property(x => x.CounterpartyName).HasMaxLength(200);
        builder.Property(x => x.Description).HasMaxLength(1000);
        builder.HasIndex(x => x.TransactionDate);
        builder.HasIndex(x => new { x.Type, x.ExpenseCategoryId });
        builder.HasOne(x => x.ExpenseCategory)
            .WithMany(x => x.FinancialTransactions)
            .HasForeignKey(x => x.ExpenseCategoryId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.CreatedByUser)
            .WithMany()
            .HasForeignKey(x => x.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class ExpenseCategoryConfiguration : IEntityTypeConfiguration<ExpenseCategory>
{
    public void Configure(EntityTypeBuilder<ExpenseCategory> builder)
    {
        builder.ToTable("ExpenseCategories");
        builder.Property(x => x.Title).HasMaxLength(100).IsRequired();
        builder.HasIndex(x => x.Title).IsUnique();
    }
}
