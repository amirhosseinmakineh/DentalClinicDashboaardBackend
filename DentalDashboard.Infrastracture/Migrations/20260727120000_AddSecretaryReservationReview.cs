using DentalDashboard.Infrastracture.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DentalDashboard.Infrastracture.Migrations;

[DbContext(typeof(DentalContext))]
[Migration("20260727120000_AddSecretaryReservationReview")]
public sealed class AddSecretaryReservationReview : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTime>(
            name: "SecretaryReservationReviewedAt",
            table: "Reservations",
            type: "datetime2",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "SecretaryReservationReviewerUserId",
            table: "Reservations",
            type: "uniqueidentifier",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "SecretaryReservationReviewNote",
            table: "Reservations",
            type: "nvarchar(1000)",
            maxLength: 1000,
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "SecretaryReservationReviewStatus",
            table: "Reservations",
            type: "int",
            nullable: false,
            defaultValue: 1);

        migrationBuilder.CreateIndex(
            name: "IX_Reservations_SecretaryReservationReviewStatus_IsCanceled_ReservationAt",
            table: "Reservations",
            columns: new[] { "SecretaryReservationReviewStatus", "IsCanceled", "ReservationAt" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_Reservations_SecretaryReservationReviewStatus_IsCanceled_ReservationAt",
            table: "Reservations");

        migrationBuilder.DropColumn(name: "SecretaryReservationReviewedAt", table: "Reservations");
        migrationBuilder.DropColumn(name: "SecretaryReservationReviewerUserId", table: "Reservations");
        migrationBuilder.DropColumn(name: "SecretaryReservationReviewNote", table: "Reservations");
        migrationBuilder.DropColumn(name: "SecretaryReservationReviewStatus", table: "Reservations");
    }
}
