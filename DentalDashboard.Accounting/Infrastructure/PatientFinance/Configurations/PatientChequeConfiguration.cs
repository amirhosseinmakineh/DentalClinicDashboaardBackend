using DentalDashboard.Accounting.Domain.PatientFinance.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalDashboard.Accounting.Infrastructure.PatientFinance
    .Configurations;

public sealed class PatientChequeConfiguration
    : IEntityTypeConfiguration<PatientCheque> {
  public void Configure(EntityTypeBuilder<PatientCheque> builder) {
    builder.Property(entity => entity.Amount).HasPrecision(18, 2);
    builder.Property(entity => entity.SayadNumber).HasMaxLength(32).IsRequired();
    builder.Property(entity => entity.OwnerName).HasMaxLength(200).IsRequired();
    builder.HasIndex(entity => new { entity.PatientFinancialCaseId, entity.DueDate, entity.Status });
    builder.HasOne(entity => entity.FinancialCase)
        .WithMany(entity => entity.Cheques)
        .HasForeignKey(entity => entity.PatientFinancialCaseId)
        .OnDelete(DeleteBehavior.Restrict);
    builder.HasQueryFilter(entity => !entity.IsDeleted);
  }
}
