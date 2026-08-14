using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DentalDashboard.Infrastracture.Migrations;

[Migration("20260814120000_AddConsultantRoleEvaluations")]
public partial class AddConsultantRoleEvaluations : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "LastEvaluationResult",
            table: "ConsultantProfiles",
            type: "int",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "LastEvaluatedAt",
            table: "ConsultantProfiles",
            type: "datetime2",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "NextRoleEvaluationAt",
            table: "ConsultantProfiles",
            type: "datetime2",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "RoleStartedAt",
            table: "ConsultantProfiles",
            type: "datetime2",
            nullable: true);

        migrationBuilder.CreateTable(
            name: "ConsultantRoleEvaluations",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                ConsultantProfileId = table.Column<long>(type: "bigint", nullable: false),
                EvaluatedRole = table.Column<int>(type: "int", nullable: false),
                ResultingRole = table.Column<int>(type: "int", nullable: true),
                PeriodStartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                PeriodEndedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                EvaluatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                SuccessfulPatientCount = table.Column<int>(type: "int", nullable: false),
                Result = table.Column<int>(type: "int", nullable: false),
                RewardLevel = table.Column<int>(type: "int", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ConsultantRoleEvaluations", x => x.Id);
                table.ForeignKey(
                    name: "FK_ConsultantRoleEvaluations_ConsultantProfiles_ConsultantProfileId",
                    column: x => x.ConsultantProfileId,
                    principalTable: "ConsultantProfiles",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_ConsultantRoleEvaluations_ConsultantProfileId_PeriodStartedAt",
            table: "ConsultantRoleEvaluations",
            columns: new[] { "ConsultantProfileId", "PeriodStartedAt" },
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "ConsultantRoleEvaluations");
        migrationBuilder.DropColumn(name: "LastEvaluationResult", table: "ConsultantProfiles");
        migrationBuilder.DropColumn(name: "LastEvaluatedAt", table: "ConsultantProfiles");
        migrationBuilder.DropColumn(name: "NextRoleEvaluationAt", table: "ConsultantProfiles");
        migrationBuilder.DropColumn(name: "RoleStartedAt", table: "ConsultantProfiles");
    }
}
