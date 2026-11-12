using DentalDashboard.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalDashboard.Infrastracture.Configuration;

public sealed class PatientPaperRecordConfiguration : IEntityTypeConfiguration<PatientPaperRecord>
{
    public void Configure(EntityTypeBuilder<PatientPaperRecord> builder)
    {
        builder.ToTable("PatientPaperRecords");
        builder.HasKey(x => x.PatientFileId);
        builder.Property(x => x.IntakeJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(x => x.TreatmentsJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(x => x.DentalChartJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(x => x.UpdatedAtUtc).IsRequired();
        builder.HasOne(x => x.PatientFile).WithOne().HasForeignKey<PatientPaperRecord>(x => x.PatientFileId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
