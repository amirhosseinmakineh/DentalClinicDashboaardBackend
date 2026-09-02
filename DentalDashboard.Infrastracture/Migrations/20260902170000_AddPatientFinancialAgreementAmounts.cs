using DentalDashboard.Infrastracture.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DentalDashboard.Infrastracture.Migrations;

[DbContext(typeof(DentalContext))]
[Migration("20260902170000_AddPatientFinancialAgreementAmounts")]
public partial class AddPatientFinancialAgreementAmounts : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
IF COL_LENGTH(N'[dbo].[PatientFinancialCases]', N'DepositAmount') IS NULL
BEGIN
    ALTER TABLE [dbo].[PatientFinancialCases]
        ADD [DepositAmount] decimal(18,2) NOT NULL
            CONSTRAINT [DF_PatientFinancialCases_DepositAmount] DEFAULT (0);
END;

IF COL_LENGTH(N'[dbo].[PatientFinancialCases]', N'PrePaymentAmount') IS NULL
BEGIN
    ALTER TABLE [dbo].[PatientFinancialCases]
        ADD [PrePaymentAmount] decimal(18,2) NOT NULL
            CONSTRAINT [DF_PatientFinancialCases_PrePaymentAmount] DEFAULT (0);
END;
""");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "DepositAmount", table: "PatientFinancialCases");
        migrationBuilder.DropColumn(name: "PrePaymentAmount", table: "PatientFinancialCases");
    }
}
