using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DentalDashboard.Infrastracture.Migrations
{
    /// <inheritdoc />
    public partial class AddPushNotificationTokenToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF COL_LENGTH('Users', 'PushNotificationToken') IS NULL
                    ALTER TABLE Users ADD PushNotificationToken nvarchar(1000) NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF COL_LENGTH('Users', 'PushNotificationToken') IS NOT NULL
                    ALTER TABLE Users DROP COLUMN PushNotificationToken;
                """);
        }
    }
}
