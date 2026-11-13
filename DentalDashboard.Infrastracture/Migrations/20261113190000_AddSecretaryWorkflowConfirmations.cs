using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using DentalDashboard.Infrastracture.Context;

#nullable disable

namespace DentalDashboard.Infrastracture.Migrations;

[DbContext(typeof(DentalContext))]
[Migration("20261113190000_AddSecretaryWorkflowConfirmations")]
public partial class AddSecretaryWorkflowConfirmations : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTime>("TreatmentConfirmedAtUtc", "PatientFileWorkflows", "datetime2", nullable: true);
        migrationBuilder.AddColumn<Guid>("TreatmentConfirmedByUserId", "PatientFileWorkflows", "uniqueidentifier", nullable: true);
        migrationBuilder.AddColumn<DateTime>("FinanceConfirmedAtUtc", "PatientFileWorkflows", "datetime2", nullable: true);
        migrationBuilder.AddColumn<Guid>("FinanceConfirmedByUserId", "PatientFileWorkflows", "uniqueidentifier", nullable: true);
        migrationBuilder.AddColumn<DateTime>("ProfileConfirmedAtUtc", "PatientFileWorkflows", "datetime2", nullable: true);
        migrationBuilder.AddColumn<Guid>("ProfileConfirmedByUserId", "PatientFileWorkflows", "uniqueidentifier", nullable: true);
        foreach (var prefix in new[] { "Treatment", "Finance", "Profile" })
        {
            migrationBuilder.CreateIndex("IX_PatientFileWorkflows_" + prefix + "ConfirmedByUserId",
                "PatientFileWorkflows", prefix + "ConfirmedByUserId");
            migrationBuilder.AddForeignKey("FK_PatientFileWorkflows_Users_" + prefix + "ConfirmedByUserId",
                table: "PatientFileWorkflows",
                column: prefix + "ConfirmedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        foreach (var prefix in new[] { "Treatment", "Finance", "Profile" })
        {
            migrationBuilder.DropForeignKey("FK_PatientFileWorkflows_Users_" + prefix + "ConfirmedByUserId", "PatientFileWorkflows");
            migrationBuilder.DropIndex("IX_PatientFileWorkflows_" + prefix + "ConfirmedByUserId", "PatientFileWorkflows");
            migrationBuilder.DropColumn(prefix + "ConfirmedAtUtc", "PatientFileWorkflows");
            migrationBuilder.DropColumn(prefix + "ConfirmedByUserId", "PatientFileWorkflows");
        }
    }
}
