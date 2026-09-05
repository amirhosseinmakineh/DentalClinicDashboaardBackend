using DentalDashboard.Accounting.Domain.PatientFinance.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalDashboard.Accounting.Infrastructure.PatientFinance
    .Configurations;

public sealed class PatientPromissoryNoteConfiguration
    : IEntityTypeConfiguration<PatientPromissoryNote> {
  public void Configure(EntityTypeBuilder<PatientPromissoryNote> builder) {
    builder.Property(entity => entity.Amount).HasPrecision(18, 2);
    builder.Property(entity => entity.SerialNumber).HasMaxLength(64).IsRequired();
    builder.HasIndex(entity => new { entity.PatientFinancialCaseId, entity.DueDate, entity.Status });
    builder.HasOne(entity => entity.FinancialCase)
        .WithMany(entity => entity.PromissoryNotes)
        .HasForeignKey(entity => entity.PatientFinancialCaseId)
        .OnDelete(DeleteBehavior.Restrict);
    builder.HasQueryFilter(entity => !entity.IsDeleted);
  }
}
