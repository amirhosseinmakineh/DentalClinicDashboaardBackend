using DentalDashboard.Infrastracture.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DentalDashboard.Infrastracture.Migrations;

[DbContext(typeof(DentalContext))]
[Migration("20261114000000_RepairPatientFinancialCaseColumns")]
public partial class RepairPatientFinancialCaseColumns : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Older migration records can exist even when these columns are missing.
        // Preserve existing data and only add missing columns.
        migrationBuilder.Sql(@"
IF OBJECT_ID(N'dbo.PatientFinancialCases', N'U') IS NULL
    THROW 51000, 'PatientFinancialCases table is missing; cannot repair its columns.', 1;

IF COL_LENGTH(N'dbo.PatientFinancialCases', N'OpgImageUrl') IS NULL
    ALTER TABLE [dbo].[PatientFinancialCases] ADD [OpgImageUrl] nvarchar(2048) NULL;

IF COL_LENGTH(N'dbo.PatientFinancialCases', N'ServiceUnitCountsJson') IS NULL
    ALTER TABLE [dbo].[PatientFinancialCases] ADD [ServiceUnitCountsJson] nvarchar(max) NOT NULL CONSTRAINT [DF_PatientFinancialCases_ServiceUnitCountsJson_Repair] DEFAULT N'{}';

IF COL_LENGTH(N'dbo.PatientFinancialCases', N'Services') IS NULL
    ALTER TABLE [dbo].[PatientFinancialCases] ADD [Services] nvarchar(max) NOT NULL CONSTRAINT [DF_PatientFinancialCases_Services_Repair] DEFAULT N'[]';

IF COL_LENGTH(N'dbo.PatientFinancialCases', N'ToothUnitCount') IS NULL
    ALTER TABLE [dbo].[PatientFinancialCases] ADD [ToothUnitCount] int NULL;
");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Non-destructive rollback: this is a repair for existing production data.
    }
}
