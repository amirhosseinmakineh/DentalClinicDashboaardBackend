using System;
using DentalDashboard.Infrastracture.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DentalDashboard.Infrastracture.Migrations;

[DbContext(typeof(DentalContext))]
[Migration("20260820120000_AddReservationOwner")]
public partial class AddReservationOwner : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "OwnerType",
            table: "Reservations",
            type: "int",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "OwnerUserId",
            table: "Reservations",
            type: "uniqueidentifier",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_Reservations_OwnerType_OwnerUserId_CreatedAt",
            table: "Reservations",
            columns: new[] { "OwnerType", "OwnerUserId", "CreatedAt" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_Reservations_OwnerType_OwnerUserId_CreatedAt",
            table: "Reservations");
        migrationBuilder.DropColumn(name: "OwnerType", table: "Reservations");
        migrationBuilder.DropColumn(name: "OwnerUserId", table: "Reservations");
    }
}
