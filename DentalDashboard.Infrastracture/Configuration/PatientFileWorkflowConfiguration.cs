using DentalDashboard.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalDashboard.Infrastracture.Configuration;

public sealed class PatientFileWorkflowConfiguration : IEntityTypeConfiguration<PatientFileWorkflow>
{
    public void Configure(EntityTypeBuilder<PatientFileWorkflow> b)
    {
        b.ToTable("PatientFileWorkflows");
        b.HasKey(x => x.PatientFileId);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.TreatmentConfirmedByUserId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.FinanceConfirmedByUserId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.ProfileConfirmedByUserId).OnDelete(DeleteBehavior.Restrict);
        b.Property(x => x.ApprovedFinanceHash).HasMaxLength(64);
        b.HasOne(x => x.PatientFile).WithOne().HasForeignKey<PatientFileWorkflow>(x => x.PatientFileId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.FinancialCase).WithOne().HasForeignKey<PatientFileWorkflow>(x => x.FinancialCaseId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.ApprovedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
