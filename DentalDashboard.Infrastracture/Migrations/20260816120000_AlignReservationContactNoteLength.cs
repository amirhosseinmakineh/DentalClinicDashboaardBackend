using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DentalDashboard.Infrastracture.Migrations;

/// <summary>
/// Aligns the existing contact-log column with the secretary API contract.
/// Existing values longer than the new limit are truncated before the schema change.
/// </summary>
public partial class AlignReservationContactNoteLength : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            "UPDATE [ReservationContactLogs] SET [Note] = LEFT([Note], 1000) WHERE LEN([Note]) > 1000");

        migrationBuilder.AlterColumn<string>(
            name: "Note",
            table: "ReservationContactLogs",
            type: "nvarchar(1000)",
            maxLength: 1000,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "nvarchar(2000)",
            oldMaxLength: 2000,
            oldNullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<string>(
            name: "Note",
            table: "ReservationContactLogs",
            type: "nvarchar(2000)",
            maxLength: 2000,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "nvarchar(1000)",
            oldMaxLength: 1000,
            oldNullable: true);
    }
}
