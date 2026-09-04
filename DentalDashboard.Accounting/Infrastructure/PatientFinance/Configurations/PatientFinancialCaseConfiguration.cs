using DentalDashboard.Domain.Secretary.Accountant.PatientFinance.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalDashboard.Infrastracture.Secretary.Accountant.PatientFinance
    .Configurations;

public sealed class PatientFinancialCaseConfiguration
    : IEntityTypeConfiguration<PatientFinancialCase> {
  public void Configure(EntityTypeBuilder<PatientFinancialCase> builder) {
    builder.Property(entity => entity.TotalAmount).HasPrecision(18, 2);
    builder.Property(entity => entity.PrePaymentAmount).HasPrecision(18, 2);
    builder.Property(entity => entity.DepositAmount).HasPrecision(18, 2);
    builder.HasIndex(entity => entity.PatientId);
    builder.HasOne(entity => entity.Patient)
        .WithMany()
        .HasForeignKey(entity => entity.PatientId)
        .OnDelete(DeleteBehavior.Restrict);
    builder.HasOne(entity => entity.CreatedByUser)
        .WithMany()
        .HasForeignKey(entity => entity.CreatedByUserId)
        .OnDelete(DeleteBehavior.Restrict);
    builder.HasQueryFilter(entity => !entity.IsDeleted);
  }
}
