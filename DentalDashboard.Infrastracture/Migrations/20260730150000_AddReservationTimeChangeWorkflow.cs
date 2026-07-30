using DentalDashboard.Infrastracture.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
namespace DentalDashboard.Infrastracture.Migrations;

[DbContext(typeof(DentalContext))]
[Migration("20260730150000_AddReservationTimeChangeWorkflow")]
public sealed class AddReservationTimeChangeWorkflow : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(name: "ReservationTimeChanges", columns: table => new
        {
            Id = table.Column<long>(type: "bigint", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
            ReservationId = table.Column<long>(type: "bigint", nullable: false),
            PreviousReservationAt = table.Column<DateTime>(type: "datetime2", nullable: false),
            NewReservationAt = table.Column<DateTime>(type: "datetime2", nullable: false),
            ChangedBySecretaryUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
            Note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
            Status = table.Column<int>(type: "int", nullable: false),
            ConfirmedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
            ConfirmedByConsultantProfileId = table.Column<long>(type: "bigint", nullable: true),
            CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false), UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
            IsDeleted = table.Column<bool>(type: "bit", nullable: false), DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
        }, constraints: table => { table.PrimaryKey("PK_ReservationTimeChanges", x => x.Id); table.ForeignKey("FK_ReservationTimeChanges_Reservations_ReservationId", x => x.ReservationId, "Reservations", "Id", onDelete: ReferentialAction.Cascade); });
        migrationBuilder.CreateIndex("UX_ReservationTimeChanges_ReservationId_Pending", "ReservationTimeChanges", "ReservationId", unique: true, filter: "[Status] = 1");

        migrationBuilder.CreateTable(name: "UserNotifications", columns: table => new
        {
            Id = table.Column<long>(type: "bigint", nullable: false).Annotation("SqlServer:Identity", "1, 1"), UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
            Type = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false), Title = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
            Body = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false), ReservationId = table.Column<long>(type: "bigint", nullable: true),
            Route = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true), ReadAt = table.Column<DateTime>(type: "datetime2", nullable: true),
            CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false), UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true), IsDeleted = table.Column<bool>(type: "bit", nullable: false), DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
        }, constraints: table => table.PrimaryKey("PK_UserNotifications", x => x.Id));
        migrationBuilder.CreateIndex("IX_UserNotifications_UserId_ReadAt_CreatedAt", "UserNotifications", new[] { "UserId", "ReadAt", "CreatedAt" });
    }
    protected override void Down(MigrationBuilder migrationBuilder)
    { migrationBuilder.DropTable("ReservationTimeChanges"); migrationBuilder.DropTable("UserNotifications"); }
}
