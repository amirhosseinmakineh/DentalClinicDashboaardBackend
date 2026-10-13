using DentalDashboard.Infrastracture.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DentalDashboard.Infrastracture.Migrations;

[DbContext(typeof(DentalContext))]
[Migration("20261013160000_AddPatientFinanceServiceUnitCounts")]
public partial class AddPatientFinanceServiceUnitCounts : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "ServiceUnitCountsJson",
            table: "PatientFinancialCases",
            type: "nvarchar(max)",
            nullable: false,
            defaultValue: "{}");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "ServiceUnitCountsJson",
            table: "PatientFinancialCases");
    }
}
