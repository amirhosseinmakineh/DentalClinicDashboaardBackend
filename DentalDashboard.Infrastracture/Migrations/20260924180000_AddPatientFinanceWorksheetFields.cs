using DentalDashboard.Infrastracture.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DentalDashboard.Infrastracture.Migrations;

[DbContext(typeof(DentalContext))]
[Migration("20260924180000_AddPatientFinanceWorksheetFields")]
public partial class AddPatientFinanceWorksheetFields : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        foreach (var column in new[] { "PaymentMethod", "InstallmentStatus", "GuaranteeDocument",
                 "GuaranteeChequeRegistration", "Notes", "ConsultantName", "ReviewItems" })
            migrationBuilder.Sql($"IF COL_LENGTH(N'dbo.PatientFinancialCases', N'{column}') IS NULL ALTER TABLE [dbo].[PatientFinancialCases] ADD [{column}] nvarchar(max) NULL;");
        migrationBuilder.Sql("IF COL_LENGTH(N'dbo.PatientFinancialCases', N'GuaranteeDate') IS NULL ALTER TABLE [dbo].[PatientFinancialCases] ADD [GuaranteeDate] datetime2 NULL;");
        migrationBuilder.Sql("IF COL_LENGTH(N'dbo.PatientFinancialCases', N'GuaranteeAmount') IS NULL ALTER TABLE [dbo].[PatientFinancialCases] ADD [GuaranteeAmount] decimal(18,2) NULL;");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        foreach (var column in new[] { "PaymentMethod", "InstallmentStatus", "GuaranteeDocument",
                 "GuaranteeChequeRegistration", "Notes", "ConsultantName", "ReviewItems",
                 "GuaranteeDate", "GuaranteeAmount" })
            migrationBuilder.DropColumn(column, "PatientFinancialCases");
    }
}
