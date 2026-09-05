using DentalDashboard.Accounting.Domain.PatientFinance.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalDashboard.Accounting.Infrastructure.PatientFinance
    .Configurations;

public sealed class PatientFinancialTransactionConfiguration
    : IEntityTypeConfiguration<PatientFinancialTransaction> {
  public void Configure(EntityTypeBuilder<PatientFinancialTransaction> builder) {
    builder.Property(entity => entity.Amount).HasPrecision(18, 2);
    builder.HasIndex(entity => new { entity.SourceType, entity.SourceId, entity.Type }).IsUnique();
    builder.HasIndex(entity => entity.PatientFinancialCaseId);
    builder.HasOne(entity => entity.FinancialCase)
        .WithMany(entity => entity.Transactions)
        .HasForeignKey(entity => entity.PatientFinancialCaseId)
        .OnDelete(DeleteBehavior.Restrict);
    builder.HasOne(entity => entity.CreatedByUser)
        .WithMany()
        .HasForeignKey(entity => entity.CreatedByUserId)
        .OnDelete(DeleteBehavior.Restrict);
    builder.HasQueryFilter(entity => !entity.IsDeleted);
  }
}
