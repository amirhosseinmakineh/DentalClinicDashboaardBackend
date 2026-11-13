using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DentalDashboard.Infrastracture.Migrations;

// Only the workflow table is new. PatientPaperRecords and OpgImageUrl already
// have their own migrations; the regenerated snapshot also accounts for them.
public partial class AddPatientFileWorkflow : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "PatientFileWorkflows",
            columns: table => new
            {
                PatientFileId = table.Column<long>(type: "bigint", nullable: false),
                ConsultationCompleted = table.Column<bool>(type: "bit", nullable: false),
                FinancialCaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                ApprovedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                ApprovedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                ApprovedFinanceHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PatientFileWorkflows", x => x.PatientFileId);
                table.ForeignKey("FK_PatientFileWorkflows_PatientFiles_PatientFileId",
                    x => x.PatientFileId, "PatientFiles", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_PatientFileWorkflows_PatientFinancialCases_FinancialCaseId",
                    x => x.FinancialCaseId, "PatientFinancialCases", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_PatientFileWorkflows_Users_ApprovedByUserId",
                    x => x.ApprovedByUserId, "Users", "Id", onDelete: ReferentialAction.Restrict);
            });
        migrationBuilder.CreateIndex("IX_PatientFileWorkflows_ApprovedByUserId", "PatientFileWorkflows", "ApprovedByUserId");
        migrationBuilder.CreateIndex("IX_PatientFileWorkflows_FinancialCaseId", "PatientFileWorkflows", "FinancialCaseId",
            unique: true, filter: "[FinancialCaseId] IS NOT NULL");
    }

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropTable("PatientFileWorkflows");
}
