using DentalDashboard.Infrastracture.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DentalDashboard.Infrastracture.Migrations;

[DbContext(typeof(DentalContext))]
[Migration("20261114010000_MakeLegacyPatientFinancialCaseServiceOptional")]
public partial class MakeLegacyPatientFinancialCaseServiceOptional : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Service is a legacy column replaced by Services in the current EF model.
        // Keep existing values; allow new rows to omit this unmapped column.
        migrationBuilder.Sql(@"
IF OBJECT_ID(N'dbo.PatientFinancialCases', N'U') IS NULL
    THROW 51000, 'PatientFinancialCases table is missing.', 1;

IF EXISTS (
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.PatientFinancialCases')
      AND name = N'Service'
      AND is_nullable = 0
)
    ALTER TABLE [dbo].[PatientFinancialCases] ALTER COLUMN [Service] int NULL;
");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // No-op: restoring NOT NULL would break rows created after this migration.
    }
}
