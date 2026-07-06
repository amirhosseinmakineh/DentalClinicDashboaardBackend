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
                    ALTER TABLE Users ADD PushNotificationToken nvarchar(max) NULL;

                IF COL_LENGTH('Users', 'PushNotificationToken') IS NOT NULL
                    ALTER TABLE Users ALTER COLUMN PushNotificationToken nvarchar(max) NULL;

                IF COL_LENGTH('Users', 'LastSeenAt') IS NULL
                    ALTER TABLE Users ADD LastSeenAt datetime2 NULL;

                IF COL_LENGTH('LeadAssignments', 'CallInitiatedAt') IS NULL
                    ALTER TABLE LeadAssignments ADD CallInitiatedAt datetime2 NULL;

                IF COL_LENGTH('LeadAssignments', 'SecondaryPhoneNumber') IS NULL
                    ALTER TABLE LeadAssignments ADD SecondaryPhoneNumber nvarchar(20) NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF COL_LENGTH('LeadAssignments', 'SecondaryPhoneNumber') IS NOT NULL
                    ALTER TABLE LeadAssignments DROP COLUMN SecondaryPhoneNumber;

                IF COL_LENGTH('LeadAssignments', 'CallInitiatedAt') IS NOT NULL
                    ALTER TABLE LeadAssignments DROP COLUMN CallInitiatedAt;

                IF COL_LENGTH('Users', 'LastSeenAt') IS NOT NULL
                    ALTER TABLE Users DROP COLUMN LastSeenAt;
                """);
        }
    }
}
