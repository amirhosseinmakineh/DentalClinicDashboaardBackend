using DentalDashboard.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalDashboard.Infrastracture.Configurations;

public sealed class ConsultantServiceRewardRuleConfiguration : IEntityTypeConfiguration<ConsultantServiceRewardRule>
{
    public void Configure(EntityTypeBuilder<ConsultantServiceRewardRule> b)
    {
        b.ToTable("ConsultantServiceRewardRules"); b.HasKey(x => x.Id);
        b.Property(x => x.Amount).HasPrecision(18, 2);
        b.Property(x => x.Basis).HasMaxLength(16); b.Property(x => x.RemainderMode).HasMaxLength(16);
        b.HasIndex(x => new { x.ServiceId, x.Id });
    }
}
public sealed class ConsultantRewardExecutionConfiguration : IEntityTypeConfiguration<ConsultantRewardExecution>
{
    public void Configure(EntityTypeBuilder<ConsultantRewardExecution> b)
    {
        b.ToTable("ConsultantRewardExecutions"); b.HasKey(x => x.Id);
        b.HasIndex(x => new { x.FinancialCaseId, x.ServiceId, x.ToothId }).IsUnique();
        b.HasIndex(x => x.ReservationId);
        b.HasOne<Reservation>().WithMany().HasForeignKey(x => x.ReservationId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<DentalDashboard.Domain.Secretary.Accountant.PatientFinance.Entities.PatientFinancialCase>()
            .WithMany().HasForeignKey(x => x.FinancialCaseId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<ConsultantProfile>().WithMany().HasForeignKey(x => x.ConsultantProfileId).OnDelete(DeleteBehavior.Restrict);
    }
}
public sealed class ReservationServiceRewardConfiguration : IEntityTypeConfiguration<Reservation>
{
    public void Configure(EntityTypeBuilder<Reservation> b)
    {
        b.HasOne(x => x.RewardConsultantProfile).WithMany().HasForeignKey(x => x.RewardConsultantProfileId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
