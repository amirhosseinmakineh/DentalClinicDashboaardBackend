using DentalDashboard.Domain.Secretary.PatientFinance.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalDashboard.Infrastracture.Secretary.PatientFinance
    .Configurations;
public sealed class PatientFinancialCaseConfiguration
    : IEntityTypeConfiguration<PatientFinancialCase> {
  public void Configure(EntityTypeBuilder<PatientFinancialCase> b) {
    b.Property(x => x.TotalAmount).HasPrecision(18, 2);
    b.HasIndex(x => x.PatientId);
    b.HasOne(x => x.Patient)
        .WithMany()
        .HasForeignKey(x => x.PatientId)
        .OnDelete(DeleteBehavior.Restrict);
    b.HasQueryFilter(x => !x.IsDeleted);
  }
}
public sealed class PatientChequeConfiguration
    : IEntityTypeConfiguration<PatientCheque> {
  public void Configure(EntityTypeBuilder<PatientCheque> b) {
    b.Property(x => x.Amount).HasPrecision(18, 2);
    b.Property(x => x.SayadNumber).HasMaxLength(32).IsRequired();
    b.Property(x => x.OwnerName).HasMaxLength(200).IsRequired();
    b.HasIndex(x => new { x.PatientFinancialCaseId, x.DueDate, x.Status });
    b.HasOne(x => x.FinancialCase)
        .WithMany(x => x.Cheques)
        .HasForeignKey(x => x.PatientFinancialCaseId)
        .OnDelete(DeleteBehavior.Restrict);
    b.HasQueryFilter(x => !x.IsDeleted);
  }
}
public sealed class PatientPromissoryNoteConfiguration
    : IEntityTypeConfiguration<PatientPromissoryNote> {
  public void Configure(EntityTypeBuilder<PatientPromissoryNote> b) {
    b.Property(x => x.Amount).HasPrecision(18, 2);
    b.Property(x => x.SerialNumber).HasMaxLength(64).IsRequired();
    b.HasIndex(x => new { x.PatientFinancialCaseId, x.DueDate, x.Status });
    b.HasOne(x => x.FinancialCase)
        .WithMany(x => x.PromissoryNotes)
        .HasForeignKey(x => x.PatientFinancialCaseId)
        .OnDelete(DeleteBehavior.Restrict);
    b.HasQueryFilter(x => !x.IsDeleted);
  }
}
public sealed class PatientDebtConfiguration
    : IEntityTypeConfiguration<PatientDebt> {
  public void Configure(EntityTypeBuilder<PatientDebt> b) {
    b.Property(x => x.Amount).HasPrecision(18, 2);
    b.HasIndex(x => new { x.SourceType, x.SourceId }).IsUnique();
    b.HasIndex(x => new { x.PatientFinancialCaseId, x.Status, x.DueDate });
    b.HasOne(x => x.FinancialCase)
        .WithMany(x => x.Debts)
        .HasForeignKey(x => x.PatientFinancialCaseId)
        .OnDelete(DeleteBehavior.Restrict);
    b.HasQueryFilter(x => !x.IsDeleted);
  }
}
public sealed class PatientFinancialTransactionConfiguration
    : IEntityTypeConfiguration<PatientFinancialTransaction> {
  public void Configure(EntityTypeBuilder<PatientFinancialTransaction> b) {
    b.Property(x => x.Amount).HasPrecision(18, 2);
    b.HasIndex(x => new { x.SourceType, x.SourceId, x.Type }).IsUnique();
    b.HasIndex(x => x.PatientFinancialCaseId);
    b.HasOne(x => x.FinancialCase)
        .WithMany(x => x.Transactions)
        .HasForeignKey(x => x.PatientFinancialCaseId)
        .OnDelete(DeleteBehavior.Restrict);
    b.HasQueryFilter(x => !x.IsDeleted);
  }
}
