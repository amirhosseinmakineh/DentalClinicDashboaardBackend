using DentalDashboard.Infrastracture.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace DentalDashboard.Infrastracture.Migrations;

[DbContext(typeof(DentalContext))]
[Migration("20261115010000_RepairPatientFinanceGuaranteeColumns")]
public sealed class RepairPatientFinanceGuaranteeColumns : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF OBJECT_ID(N'dbo.PatientCheques', N'U') IS NOT NULL
               AND COL_LENGTH(N'dbo.PatientCheques', N'IsGuarantee') IS NULL
            BEGIN
                ALTER TABLE [dbo].[PatientCheques]
                ADD [IsGuarantee] bit NOT NULL
                    CONSTRAINT [DF_PatientCheques_IsGuarantee_Repair] DEFAULT (0);
            END;

            IF OBJECT_ID(N'dbo.PatientPromissoryNotes', N'U') IS NOT NULL
               AND COL_LENGTH(N'dbo.PatientPromissoryNotes', N'IsGuarantee') IS NULL
            BEGIN
                ALTER TABLE [dbo].[PatientPromissoryNotes]
                ADD [IsGuarantee] bit NOT NULL
                    CONSTRAINT [DF_PatientPromissoryNotes_IsGuarantee_Repair] DEFAULT (0);
            END;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Intentionally preserve legacy columns and their data.
    }
}
