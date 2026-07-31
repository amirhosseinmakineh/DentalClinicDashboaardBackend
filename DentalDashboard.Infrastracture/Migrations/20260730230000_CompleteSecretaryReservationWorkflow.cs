using DentalDashboard.Infrastracture.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
namespace DentalDashboard.Infrastracture.Migrations;

[DbContext(typeof(DentalContext))]
[Migration("20260730230000_CompleteSecretaryReservationWorkflow")]
public sealed class CompleteSecretaryReservationWorkflow : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<bool>("IsConfirmedWithPatient", "Reservations", "bit", nullable: true, oldClrType: typeof(bool), oldType: "bit");
        migrationBuilder.AddColumn<DateTime>("InitialReservationAt", "Reservations", "datetime2", nullable: true);
        migrationBuilder.AddColumn<DateTime>("LastActivityAt", "Reservations", "datetime2", nullable: true);
        migrationBuilder.AddColumn<Guid>("ConfirmedWithPatientByUserId", "Reservations", "uniqueidentifier", nullable: true);
        migrationBuilder.AddColumn<string>("CancellationReason", "Reservations", "nvarchar(1000)", maxLength: 1000, nullable: true);
        migrationBuilder.AddColumn<string>("PreviousValue", "SecretaryReservationActivities", "nvarchar(2000)", maxLength: 2000, nullable: true);
        migrationBuilder.AddColumn<string>("NewValue", "SecretaryReservationActivities", "nvarchar(2000)", maxLength: 2000, nullable: true);
        migrationBuilder.AddColumn<Guid>("CreatedByUserId", "ReservationFollowUps", "uniqueidentifier", nullable: true);
        migrationBuilder.AddColumn<Guid>("CompletedByUserId", "ReservationFollowUps", "uniqueidentifier", nullable: true);
        migrationBuilder.AddColumn<string>("Result", "ReservationFollowUps", "nvarchar(max)", nullable: true);
        migrationBuilder.AddColumn<byte[]>("RowVersion", "ReservationFollowUps", "rowversion", rowVersion: true, nullable: true);
        migrationBuilder.Sql("UPDATE Reservations SET InitialReservationAt = ReservationAt, LastActivityAt = COALESCE(UpdatedAt, CreatedAt), ReservationRequestStatus = CASE WHEN IsCanceled = 1 THEN 5 ELSE 2 END WHERE InitialReservationAt IS NULL");
        migrationBuilder.AlterColumn<DateTime>("InitialReservationAt", "Reservations", "datetime2", nullable: false, oldClrType: typeof(DateTime), oldType: "datetime2", oldNullable: true);
        migrationBuilder.AlterColumn<DateTime>("LastActivityAt", "Reservations", "datetime2", nullable: false, oldClrType: typeof(DateTime), oldType: "datetime2", oldNullable: true);
        migrationBuilder.AlterColumn<Guid>("CreatedByUserId", "ReservationFollowUps", "uniqueidentifier", nullable: false, defaultValue: Guid.Empty, oldClrType: typeof(Guid), oldType: "uniqueidentifier", oldNullable: true);

        migrationBuilder.CreateTable("ReservationContactLogs", table => new { Id = table.Column<long>("bigint", nullable: false).Annotation("SqlServer:Identity", "1, 1"), ReservationId = table.Column<long>("bigint", nullable: false), Result = table.Column<int>("int", nullable: false), Note = table.Column<string>("nvarchar(2000)", maxLength: 2000, nullable: true), CreatedByUserId = table.Column<Guid>("uniqueidentifier", nullable: false), CreatedAt = table.Column<DateTime>("datetime2", nullable: false), UpdatedAt = table.Column<DateTime>("datetime2", nullable: true), IsDeleted = table.Column<bool>("bit", nullable: false), DeletedAt = table.Column<DateTime>("datetime2", nullable: true) }, constraints: table => { table.PrimaryKey("PK_ReservationContactLogs", x => x.Id); table.ForeignKey("FK_ReservationContactLogs_Reservations_ReservationId", x => x.ReservationId, "Reservations", "Id", onDelete: ReferentialAction.Cascade); });
        migrationBuilder.CreateTable("ReservationNotes", table => new { Id = table.Column<long>("bigint", nullable: false).Annotation("SqlServer:Identity", "1, 1"), ReservationId = table.Column<long>("bigint", nullable: false), Note = table.Column<string>("nvarchar(2000)", maxLength: 2000, nullable: false), CreatedByUserId = table.Column<Guid>("uniqueidentifier", nullable: false), CreatedAt = table.Column<DateTime>("datetime2", nullable: false), UpdatedAt = table.Column<DateTime>("datetime2", nullable: true), IsDeleted = table.Column<bool>("bit", nullable: false), DeletedAt = table.Column<DateTime>("datetime2", nullable: true) }, constraints: table => { table.PrimaryKey("PK_ReservationNotes", x => x.Id); table.ForeignKey("FK_ReservationNotes_Reservations_ReservationId", x => x.ReservationId, "Reservations", "Id", onDelete: ReferentialAction.Cascade); });
        migrationBuilder.CreateTable("ReservationNotificationOutbox", table => new { Id = table.Column<long>("bigint", nullable: false).Annotation("SqlServer:Identity", "1, 1"), ReservationId = table.Column<long>("bigint", nullable: false), ActivityId = table.Column<long>("bigint", nullable: false), EventType = table.Column<string>("nvarchar(100)", maxLength: 100, nullable: false), Payload = table.Column<string>("nvarchar(4000)", maxLength: 4000, nullable: false), IdempotencyKey = table.Column<string>("nvarchar(250)", maxLength: 250, nullable: false), ProcessedAt = table.Column<DateTime>("datetime2", nullable: true), LastError = table.Column<string>("nvarchar(2000)", maxLength: 2000, nullable: true), CreatedAt = table.Column<DateTime>("datetime2", nullable: false), UpdatedAt = table.Column<DateTime>("datetime2", nullable: true), IsDeleted = table.Column<bool>("bit", nullable: false), DeletedAt = table.Column<DateTime>("datetime2", nullable: true) }, constraints: table => table.PrimaryKey("PK_ReservationNotificationOutbox", x => x.Id));
        migrationBuilder.CreateIndex("IX_Reservations_RequestStatus_IsCanceled", "Reservations", new[] { "ReservationRequestStatus", "IsCanceled" });
        migrationBuilder.CreateIndex("IX_Reservations_LastActivityAt", "Reservations", "LastActivityAt");
        migrationBuilder.CreateIndex("IX_ReservationFollowUps_ReservationId_Status", "ReservationFollowUps", new[] { "ReservationId", "Status" });
        migrationBuilder.CreateIndex("IX_SecretaryReservationActivities_ReservationId_CreatedAt", "SecretaryReservationActivities", new[] { "ReservationId", "CreatedAt" });
        migrationBuilder.CreateIndex("IX_ReservationContactLogs_ReservationId_CreatedAt", "ReservationContactLogs", new[] { "ReservationId", "CreatedAt" });
        migrationBuilder.CreateIndex("IX_ReservationNotes_ReservationId_CreatedAt", "ReservationNotes", new[] { "ReservationId", "CreatedAt" });
        migrationBuilder.CreateIndex("IX_ReservationNotificationOutbox_IdempotencyKey", "ReservationNotificationOutbox", "IdempotencyKey", unique: true);
        migrationBuilder.CreateIndex("IX_ReservationNotificationOutbox_ProcessedAt_CreatedAt", "ReservationNotificationOutbox", new[] { "ProcessedAt", "CreatedAt" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("ReservationContactLogs"); migrationBuilder.DropTable("ReservationNotes"); migrationBuilder.DropTable("ReservationNotificationOutbox");
        migrationBuilder.DropIndex("IX_Reservations_RequestStatus_IsCanceled", "Reservations"); migrationBuilder.DropIndex("IX_Reservations_LastActivityAt", "Reservations");
        migrationBuilder.DropIndex("IX_ReservationFollowUps_ReservationId_Status", "ReservationFollowUps"); migrationBuilder.DropIndex("IX_SecretaryReservationActivities_ReservationId_CreatedAt", "SecretaryReservationActivities");
        foreach (var c in new[] { "InitialReservationAt", "LastActivityAt", "ConfirmedWithPatientByUserId", "CancellationReason" }) migrationBuilder.DropColumn(c, "Reservations");
        foreach (var c in new[] { "PreviousValue", "NewValue" }) migrationBuilder.DropColumn(c, "SecretaryReservationActivities");
        foreach (var c in new[] { "CreatedByUserId", "CompletedByUserId", "Result", "RowVersion" }) migrationBuilder.DropColumn(c, "ReservationFollowUps");
        migrationBuilder.AlterColumn<bool>("IsConfirmedWithPatient", "Reservations", "bit", nullable: false, defaultValue: false, oldClrType: typeof(bool), oldType: "bit", oldNullable: true);
    }
}
