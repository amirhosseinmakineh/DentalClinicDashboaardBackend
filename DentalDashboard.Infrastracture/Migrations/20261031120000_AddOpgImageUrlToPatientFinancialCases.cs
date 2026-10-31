using DentalDashboard.Infrastracture.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DentalDashboard.Infrastracture.Migrations
{
    [DbContext(typeof(DentalContext))]
    [Migration("20261031120000_AddOpgImageUrlToPatientFinancialCases")]
    public partial class AddOpgImageUrlToPatientFinancialCases : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OpgImageUrl",
                table: "PatientFinancialCases",
                type: "nvarchar(2048)",
                maxLength: 2048,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OpgImageUrl",
                table: "PatientFinancialCases");
        }
    }
}
