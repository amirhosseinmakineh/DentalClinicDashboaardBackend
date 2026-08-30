using DentalDashboard.Domain.Enums;
using DentalDashboard.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalDashboard.Infrastracture.Configuration;

public sealed class PatientFileConfiguration : IEntityTypeConfiguration<PatientFile>
{
    public void Configure(EntityTypeBuilder<PatientFile> builder)
    {
        builder.ToTable("PatientFiles", t => t.HasCheckConstraint("CK_PatientFiles_FileNumber_Positive", "[FileNumber] > 0"));
        builder.Property(x => x.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.LastName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.PhoneNumber).HasMaxLength(20).IsRequired();
        builder.Property(x => x.SourceType).IsRequired();
        builder.HasIndex(x => x.FileNumber).IsUnique();
        builder.HasIndex(x => x.PhoneNumber);
        builder.HasIndex(x => x.SourceType);
        builder.HasIndex(x => x.PatientReferenceId)
            .IsUnique()
            .HasFilter($"[PatientReferenceId] IS NOT NULL AND [SourceType] = {(int)PatientFileSourceType.System} AND [IsDeleted] = 0");
        builder.HasOne(x => x.PatientReference).WithMany().HasForeignKey(x => x.PatientReferenceId).OnDelete(DeleteBehavior.Restrict);
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
