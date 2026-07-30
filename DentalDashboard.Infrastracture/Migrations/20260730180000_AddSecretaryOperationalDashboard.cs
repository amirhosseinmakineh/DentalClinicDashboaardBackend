using DentalDashboard.Infrastracture.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
namespace DentalDashboard.Infrastracture.Migrations;

[DbContext(typeof(DentalContext))]
[Migration("20260730180000_AddSecretaryOperationalDashboard")]
public sealed class AddSecretaryOperationalDashboard : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTime>("ConfirmedWithPatientAt", "Reservations", "datetime2", nullable: true);
        migrationBuilder.AddColumn<bool>("IsConfirmedWithPatient", "Reservations", "bit", nullable: false, defaultValue: false);
        migrationBuilder.AddColumn<string>("PatientConfirmationNote", "Reservations", "nvarchar(1000)", maxLength: 1000, nullable: true);
        migrationBuilder.AddColumn<int>("ReservationRequestStatus", "Reservations", "int", nullable: false, defaultValue: 1);
        migrationBuilder.AddColumn<DateTime>("RequestReviewedAt", "Reservations", "datetime2", nullable: true);
        migrationBuilder.AddColumn<Guid>("RequestReviewedByUserId", "Reservations", "uniqueidentifier", nullable: true);
        migrationBuilder.AddColumn<string>("RequestReviewNote", "Reservations", "nvarchar(1000)", maxLength: 1000, nullable: true);
        migrationBuilder.AddColumn<int>("RejectionReasonCode", "Reservations", "int", nullable: true);
        migrationBuilder.AddColumn<string>("RejectionReason", "Reservations", "nvarchar(1000)", maxLength: 1000, nullable: true);
        migrationBuilder.AddColumn<byte[]>("RowVersion", "Reservations", "rowversion", rowVersion: true, nullable: false);
        migrationBuilder.AddColumn<int>("VisitResultStatus", "Reservations", "int", nullable: false, defaultValue: 1);
        migrationBuilder.AddColumn<DateTime>("VisitResultRecordedAt", "Reservations", "datetime2", nullable: true);
        migrationBuilder.AddColumn<Guid>("VisitResultRecordedByUserId", "Reservations", "uniqueidentifier", nullable: true);
        migrationBuilder.AddColumn<string>("VisitResultNote", "Reservations", "nvarchar(1000)", maxLength: 1000, nullable: true);

        migrationBuilder.CreateTable("ReservationFollowUps", table => new
        {
            Id = table.Column<long>("bigint", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
            ReservationId = table.Column<long>("bigint", nullable: false), ScheduledAt = table.Column<DateTime>("datetime2", nullable: false),
            ReminderAt = table.Column<DateTime>("datetime2", nullable: true), Status = table.Column<int>("int", nullable: false),
            Priority = table.Column<int>("int", nullable: false), Reason = table.Column<string>("nvarchar(1000)", maxLength: 1000, nullable: false),
            AssignedSecretaryUserId = table.Column<Guid>("uniqueidentifier", nullable: true), CompletedAt = table.Column<DateTime>("datetime2", nullable: true),
            CreatedAt = table.Column<DateTime>("datetime2", nullable: false), UpdatedAt = table.Column<DateTime>("datetime2", nullable: true),
            IsDeleted = table.Column<bool>("bit", nullable: false), DeletedAt = table.Column<DateTime>("datetime2", nullable: true)
        }, constraints: table => { table.PrimaryKey("PK_ReservationFollowUps", x => x.Id); table.ForeignKey("FK_ReservationFollowUps_Reservations_ReservationId", x => x.ReservationId, "Reservations", "Id", onDelete: ReferentialAction.Cascade); });

        migrationBuilder.CreateTable("SecretaryReservationActivities", table => new
        {
            Id = table.Column<long>("bigint", nullable: false).Annotation("SqlServer:Identity", "1, 1"), ReservationId = table.Column<long>("bigint", nullable: false),
            ActorUserId = table.Column<Guid>("uniqueidentifier", nullable: false), ActivityType = table.Column<string>("nvarchar(80)", maxLength: 80, nullable: false),
            Description = table.Column<string>("nvarchar(1000)", maxLength: 1000, nullable: false), CreatedAt = table.Column<DateTime>("datetime2", nullable: false),
            UpdatedAt = table.Column<DateTime>("datetime2", nullable: true), IsDeleted = table.Column<bool>("bit", nullable: false), DeletedAt = table.Column<DateTime>("datetime2", nullable: true)
        }, constraints: table => { table.PrimaryKey("PK_SecretaryReservationActivities", x => x.Id); table.ForeignKey("FK_SecretaryReservationActivities_Reservations_ReservationId", x => x.ReservationId, "Reservations", "Id", onDelete: ReferentialAction.Cascade); table.ForeignKey("FK_SecretaryReservationActivities_Users_ActorUserId", x => x.ActorUserId, "Users", "Id", onDelete: ReferentialAction.Restrict); });

        migrationBuilder.CreateIndex("IX_ReservationFollowUps_ReservationId", "ReservationFollowUps", "ReservationId");
        migrationBuilder.CreateIndex("IX_ReservationFollowUps_Status_ScheduledAt", "ReservationFollowUps", new[] { "Status", "ScheduledAt" });
        migrationBuilder.CreateIndex("IX_ReservationFollowUps_Status_ReminderAt", "ReservationFollowUps", new[] { "Status", "ReminderAt" });
        migrationBuilder.CreateIndex("IX_SecretaryReservationActivities_ActorUserId", "SecretaryReservationActivities", "ActorUserId");
        migrationBuilder.CreateIndex("IX_SecretaryReservationActivities_ReservationId", "SecretaryReservationActivities", "ReservationId");
        migrationBuilder.CreateIndex("IX_SecretaryReservationActivities_CreatedAt", "SecretaryReservationActivities", "CreatedAt");
        migrationBuilder.CreateIndex("IX_Reservations_ReservationRequestStatus_ReservationAt_IsCanceled_IsDeleted", "Reservations", new[] { "ReservationRequestStatus", "ReservationAt", "IsCanceled", "IsDeleted" });
        migrationBuilder.CreateIndex("IX_Reservations_VisitResultStatus_ReservationAt_IsCanceled_IsDeleted", "Reservations", new[] { "VisitResultStatus", "ReservationAt", "IsCanceled", "IsDeleted" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("ReservationFollowUps"); migrationBuilder.DropTable("SecretaryReservationActivities");
        migrationBuilder.DropIndex("IX_Reservations_ReservationRequestStatus_ReservationAt_IsCanceled_IsDeleted", "Reservations");
        migrationBuilder.DropIndex("IX_Reservations_VisitResultStatus_ReservationAt_IsCanceled_IsDeleted", "Reservations");
        foreach (var column in new[] { "ConfirmedWithPatientAt", "IsConfirmedWithPatient", "PatientConfirmationNote", "ReservationRequestStatus", "RequestReviewedAt", "RequestReviewedByUserId", "RequestReviewNote", "RejectionReasonCode", "RejectionReason", "RowVersion", "VisitResultStatus", "VisitResultRecordedAt", "VisitResultRecordedByUserId", "VisitResultNote" }) migrationBuilder.DropColumn(column, "Reservations");
    }
}
