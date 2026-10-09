using Microsoft.EntityFrameworkCore.Migrations;

namespace DentalDashboard.Infrastracture.Migrations;

public partial class AddAdminLeadSheets : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "SourceType",
            table: "LeadAssignments",
            type: "int",
            nullable: false,
            defaultValue: 1);

        migrationBuilder.CreateTable(
            name: "AdminLeadSheets",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                IsActive = table.Column<bool>(type: "bit", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                UpdatedAt = table.Column<DateTime?>(type: "datetime2", nullable: true),
                DeletedAt = table.Column<DateTime?>(type: "datetime2", nullable: true),
                IsDeleted = table.Column<bool>(type: "bit", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_AdminLeadSheets", x => x.Id));

        migrationBuilder.CreateTable(
            name: "AdminSheetLeads",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                AdminLeadSheetId = table.Column<long>(type: "bigint", nullable: false),
                PhoneNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                ProcessingStatus = table.Column<int>(type: "int", nullable: false),
                LeadAssignmentId = table.Column<long>(type: "bigint", nullable: true),
                ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                ProcessedAt = table.Column<DateTime?>(type: "datetime2", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                UpdatedAt = table.Column<DateTime?>(type: "datetime2", nullable: true),
                DeletedAt = table.Column<DateTime?>(type: "datetime2", nullable: true),
                IsDeleted = table.Column<bool>(type: "bit", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AdminSheetLeads", x => x.Id);
                table.ForeignKey(
                    name: "FK_AdminSheetLeads_AdminLeadSheets_AdminLeadSheetId",
                    column: x => x.AdminLeadSheetId,
                    principalTable: "AdminLeadSheets",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_AdminSheetLeads_LeadAssignments_LeadAssignmentId",
                    column: x => x.LeadAssignmentId,
                    principalTable: "LeadAssignments",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
            });

        migrationBuilder.CreateIndex(
            name: "IX_AdminSheetLeads_AdminLeadSheetId",
            table: "AdminSheetLeads",
            column: "AdminLeadSheetId");
        migrationBuilder.CreateIndex(
            name: "IX_AdminSheetLeads_LeadAssignmentId",
            table: "AdminSheetLeads",
            column: "LeadAssignmentId");
        migrationBuilder.CreateIndex(
            name: "IX_AdminSheetLeads_ProcessingStatus",
            table: "AdminSheetLeads",
            column: "ProcessingStatus");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "AdminSheetLeads");
        migrationBuilder.DropTable(name: "AdminLeadSheets");
        migrationBuilder.DropColumn(name: "SourceType", table: "LeadAssignments");
    }
}
