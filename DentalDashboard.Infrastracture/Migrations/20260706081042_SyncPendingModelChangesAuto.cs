using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DentalDashboard.Infrastracture.Migrations
{
    /// <inheritdoc />
    public partial class SyncPendingModelChangesAuto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF COL_LENGTH('Users', 'PushNotificationToken') IS NULL
                    ALTER TABLE Users ADD PushNotificationToken nvarchar(16000) NULL;

                IF COL_LENGTH('Users', 'PushNotificationToken') IS NOT NULL
                BEGIN
                    ALTER TABLE Users ALTER COLUMN PushNotificationToken nvarchar(16000) NULL;
                END

                IF COL_LENGTH('Users', 'LastSeenAt') IS NULL
                    ALTER TABLE Users ADD LastSeenAt datetime2 NULL;

                IF COL_LENGTH('LeadAssignments', 'CallInitiatedAt') IS NULL
                    ALTER TABLE LeadAssignments ADD CallInitiatedAt datetime2 NULL;

                IF COL_LENGTH('LeadAssignments', 'SecondaryPhoneNumber') IS NULL
                    ALTER TABLE LeadAssignments ADD SecondaryPhoneNumber nvarchar(20) NULL;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "PushNotificationToken",
                table: "Users",
                type: "nvarchar(max)",
                maxLength: 16000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(16000)",
                oldMaxLength: 16000,
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "PushNotificationToken",
                table: "Users",
                type: "nvarchar(16000)",
                maxLength: 16000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldMaxLength: 16000,
                oldNullable: true);
        }
    }
}
