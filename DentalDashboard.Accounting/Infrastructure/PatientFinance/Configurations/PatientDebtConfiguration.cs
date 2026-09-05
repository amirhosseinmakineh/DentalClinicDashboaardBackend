using DentalDashboard.Accounting.Domain.PatientFinance.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalDashboard.Accounting.Infrastructure.PatientFinance
    .Configurations;

public sealed class PatientDebtConfiguration
    : IEntityTypeConfiguration<PatientDebt> {
  public void Configure(EntityTypeBuilder<PatientDebt> builder) {
    builder.Property(entity => entity.Amount).HasPrecision(18, 2);
    builder.HasIndex(entity => new { entity.SourceType, entity.SourceId }).IsUnique();
    builder.HasIndex(entity => new { entity.PatientFinancialCaseId, entity.Status, entity.DueDate });
    builder.HasOne(entity => entity.FinancialCase)
        .WithMany(entity => entity.Debts)
        .HasForeignKey(entity => entity.PatientFinancialCaseId)
        .OnDelete(DeleteBehavior.Restrict);
    builder.HasQueryFilter(entity => !entity.IsDeleted);
  }
}
