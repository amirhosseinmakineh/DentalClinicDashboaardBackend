using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DentalDashboard.Infrastracture.Migrations
{
    /// <inheritdoc />
    public partial class AddConsultantServiceRewardRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "RewardConsultantProfileId",
                table: "Reservations",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ServiceRewardSnapshotJson",
                table: "Reservations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ConsultantRewardExecutions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FinancialCaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReservationId = table.Column<long>(type: "bigint", nullable: false),
                    ConsultantProfileId = table.Column<long>(type: "bigint", nullable: false),
                    ServiceId = table.Column<int>(type: "int", nullable: false),
                    ToothId = table.Column<int>(type: "int", nullable: false),
                    PerformedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsultantRewardExecutions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConsultantRewardExecutions_ConsultantProfiles_ConsultantProfileId",
                        column: x => x.ConsultantProfileId,
                        principalTable: "ConsultantProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ConsultantRewardExecutions_PatientFinancialCases_FinancialCaseId",
                        column: x => x.FinancialCaseId,
                        principalTable: "PatientFinancialCases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ConsultantRewardExecutions_Reservations_ReservationId",
                        column: x => x.ReservationId,
                        principalTable: "Reservations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ConsultantServiceRewardRules",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ServiceId = table.Column<int>(type: "int", nullable: false),
                    Basis = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    UnitsPerReward = table.Column<int>(type: "int", nullable: false),
                    RemainderMode = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsultantServiceRewardRules", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_RewardConsultantProfileId",
                table: "Reservations",
                column: "RewardConsultantProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_ConsultantRewardExecutions_ConsultantProfileId",
                table: "ConsultantRewardExecutions",
                column: "ConsultantProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_ConsultantRewardExecutions_FinancialCaseId_ServiceId_ToothId",
                table: "ConsultantRewardExecutions",
                columns: new[] { "FinancialCaseId", "ServiceId", "ToothId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ConsultantRewardExecutions_ReservationId",
                table: "ConsultantRewardExecutions",
                column: "ReservationId");

            migrationBuilder.CreateIndex(
                name: "IX_ConsultantServiceRewardRules_ServiceId_Id",
                table: "ConsultantServiceRewardRules",
                columns: new[] { "ServiceId", "Id" });

            migrationBuilder.AddForeignKey(
                name: "FK_Reservations_ConsultantProfiles_RewardConsultantProfileId",
                table: "Reservations",
                column: "RewardConsultantProfileId",
                principalTable: "ConsultantProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reservations_ConsultantProfiles_RewardConsultantProfileId",
                table: "Reservations");

            migrationBuilder.DropTable(
                name: "ConsultantRewardExecutions");

            migrationBuilder.DropTable(
                name: "ConsultantServiceRewardRules");

            migrationBuilder.DropIndex(
                name: "IX_Reservations_RewardConsultantProfileId",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "RewardConsultantProfileId",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "ServiceRewardSnapshotJson",
                table: "Reservations");
        }
    }
}
