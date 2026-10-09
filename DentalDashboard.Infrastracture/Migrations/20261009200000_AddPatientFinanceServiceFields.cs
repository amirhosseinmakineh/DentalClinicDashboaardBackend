using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DentalDashboard.Infrastracture.Migrations;

public partial class AddPatientFinanceServiceFields : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("IF COL_LENGTH(N'dbo.PatientFinancialCases', N'Services') IS NULL ALTER TABLE [dbo].[PatientFinancialCases] ADD [Services] nvarchar(max) NOT NULL CONSTRAINT [DF_PatientFinancialCases_Services] DEFAULT N'[]';");
        migrationBuilder.Sql("IF COL_LENGTH(N'dbo.PatientFinancialCases', N'ToothUnitCount') IS NULL ALTER TABLE [dbo].[PatientFinancialCases] ADD [ToothUnitCount] int NULL;");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("IF COL_LENGTH(N'dbo.PatientFinancialCases', N'Services') IS NULL RETURN; ALTER TABLE [dbo].[PatientFinancialCases] DROP CONSTRAINT [DF_PatientFinancialCases_Services]; ALTER TABLE [dbo].[PatientFinancialCases] DROP COLUMN [Services];");
        migrationBuilder.Sql("IF COL_LENGTH(N'dbo.PatientFinancialCases', N'ToothUnitCount') IS NOT NULL ALTER TABLE [dbo].[PatientFinancialCases] DROP COLUMN [ToothUnitCount];");
    }
}
