using DentalDashboard.Domain.Secretary.Accountant.PatientFinance.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalDashboard.Infrastracture.Secretary.Accountant.PatientFinance.Configurations;

public sealed class PatientInstallmentConfiguration : IEntityTypeConfiguration<PatientInstallment>
{
    public void Configure(EntityTypeBuilder<PatientInstallment> b)
    {
        b.ToTable("PatientInstallments");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedOnAdd();
        b.Property(x => x.Amount).HasPrecision(18, 2);
        b.HasIndex(x => new { x.PatientFinancialCaseId, x.DueDate });
        b.HasOne(x => x.FinancialCase).WithMany(x => x.Installments)
            .HasForeignKey(x => x.PatientFinancialCaseId).OnDelete(DeleteBehavior.Restrict);
        b.HasQueryFilter(x => !x.IsDeleted);
    }
}
