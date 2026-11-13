using DentalDashboard.Infrastracture.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DentalDashboard.Infrastracture.Migrations;

[DbContext(typeof(DentalContext))]
[Migration("20261112180000_AddPatientPaperRecords")]
public partial class AddPatientPaperRecords : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "PatientPaperRecords",
            columns: table => new
            {
                PatientFileId = table.Column<long>(type: "bigint", nullable: false),
                IntakeJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                TreatmentsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                DentalChartJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PatientPaperRecords", x => x.PatientFileId);
                table.ForeignKey("FK_PatientPaperRecords_PatientFiles_PatientFileId",
                    x => x.PatientFileId, "PatientFiles", "Id", onDelete: ReferentialAction.Restrict);
            });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "PatientPaperRecords");
    }
}
