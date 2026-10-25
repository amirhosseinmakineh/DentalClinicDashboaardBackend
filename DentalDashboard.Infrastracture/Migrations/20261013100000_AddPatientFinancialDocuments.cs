using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using DentalDashboard.Infrastracture.Context;

#nullable disable

namespace DentalDashboard.Infrastracture.Migrations;

[DbContext(typeof(DentalContext))]
[Migration("20261013100000_AddPatientFinancialDocuments")]
public partial class AddPatientFinancialDocuments : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "PatientFinancialDocuments",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                PatientFinancialCaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                StoragePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                ContentType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PatientFinancialDocuments", document => document.Id);
                table.ForeignKey(
                    name: "FK_PatientFinancialDocuments_PatientFinancialCases_PatientFinancialCaseId",
                    column: document => document.PatientFinancialCaseId,
                    principalTable: "PatientFinancialCases",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_PatientFinancialDocuments_PatientFinancialCaseId",
            table: "PatientFinancialDocuments",
            column: "PatientFinancialCaseId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "PatientFinancialDocuments");
    }
}
