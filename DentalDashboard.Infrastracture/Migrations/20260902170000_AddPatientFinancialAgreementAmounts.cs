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
        migrationBuilder.AddColumn<decimal>(
            name: "DepositAmount",
            table: "PatientFinancialCases",
            type: "decimal(18,2)",
            nullable: false,
            defaultValue: 0m);

        migrationBuilder.AddColumn<decimal>(
            name: "PrePaymentAmount",
            table: "PatientFinancialCases",
            type: "decimal(18,2)",
            nullable: false,
            defaultValue: 0m);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "DepositAmount", table: "PatientFinancialCases");
        migrationBuilder.DropColumn(name: "PrePaymentAmount", table: "PatientFinancialCases");
    }
}
