using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using DentalDashboard.Infrastracture.Context;

#nullable disable

namespace DentalDashboard.Infrastracture.Migrations;

[DbContext(typeof(DentalContext))]
[Migration("20260830193000_AddPatientFiles")]
public partial class AddPatientFiles : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "PatientFiles",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                PatientReferenceId = table.Column<long>(type: "bigint", nullable: true),
                FileNumber = table.Column<long>(type: "bigint", nullable: false),
                FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                PhoneNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                SourceType = table.Column<int>(type: "int", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PatientFiles", x => x.Id);
                table.CheckConstraint("CK_PatientFiles_FileNumber_Positive", "[FileNumber] > 0");
                table.ForeignKey("FK_PatientFiles_LeadAssignments_PatientReferenceId", x => x.PatientReferenceId,
                    "LeadAssignments", "Id", onDelete: ReferentialAction.Restrict);
            });
        migrationBuilder.CreateIndex("IX_PatientFiles_FileNumber", "PatientFiles", "FileNumber", unique: true);
        migrationBuilder.CreateIndex("IX_PatientFiles_PhoneNumber", "PatientFiles", "PhoneNumber");
        migrationBuilder.CreateIndex("IX_PatientFiles_SourceType", "PatientFiles", "SourceType");
        migrationBuilder.CreateIndex("IX_PatientFiles_PatientReferenceId", "PatientFiles", "PatientReferenceId", unique: true,
            filter: "[PatientReferenceId] IS NOT NULL AND [SourceType] = 1 AND [IsDeleted] = 0");
    }

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable("PatientFiles");
}
