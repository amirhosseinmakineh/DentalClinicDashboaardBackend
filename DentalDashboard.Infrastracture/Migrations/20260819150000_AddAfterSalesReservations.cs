using DentalDashboard.Infrastracture.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DentalDashboard.Infrastracture.Migrations;

[DbContext(typeof(DentalContext))]
[Migration("20260819150000_AddAfterSalesReservations")]
public partial class AddAfterSalesReservations : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "PatientReceivedService",
            table: "Reservations",
            type: "bit",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "ReservationType",
            table: "Reservations",
            type: "int",
            nullable: false,
            defaultValue: 1);

        migrationBuilder.CreateIndex(
            name: "IX_Reservations_ReservationType_ReservationAt",
            table: "Reservations",
            columns: new[] { "ReservationType", "ReservationAt" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_Reservations_ReservationType_ReservationAt",
            table: "Reservations");
        migrationBuilder.DropColumn(name: "PatientReceivedService", table: "Reservations");
        migrationBuilder.DropColumn(name: "ReservationType", table: "Reservations");
    }
}
