using DentalDashboard.Infrastracture.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
namespace DentalDashboard.Infrastracture.Migrations;

[DbContext(typeof(DentalContext))]
[Migration("20260830210000_AddPatientFinanceInitialPayment")]
public partial class AddPatientFinanceInitialPayment : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) =>
        migrationBuilder.AddColumn<decimal>(
            name: "InitialPaymentAmount",
            table: "PatientFinancialCases",
            type: "decimal(18,2)",
            nullable: false,
            defaultValue: 0m);

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropColumn(
            name: "InitialPaymentAmount",
            table: "PatientFinancialCases");
}
