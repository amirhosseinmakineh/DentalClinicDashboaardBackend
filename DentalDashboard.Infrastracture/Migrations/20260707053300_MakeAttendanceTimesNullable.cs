using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DentalDashboard.Infrastracture.Migrations
{
    /// <inheritdoc />
    public partial class MakeAttendanceTimesNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF COL_LENGTH('Attendances', 'CheckInTime') IS NOT NULL
                    ALTER TABLE Attendances ALTER COLUMN CheckInTime time NULL;

                IF COL_LENGTH('Attendances', 'CheckOutTime') IS NOT NULL
                    ALTER TABLE Attendances ALTER COLUMN CheckOutTime time NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE Attendances
                SET CheckInTime = COALESCE(CheckInTime, CAST('00:00:00' AS time))
                WHERE CheckInTime IS NULL;

                UPDATE Attendances
                SET CheckOutTime = COALESCE(CheckOutTime, CAST('00:00:00' AS time))
                WHERE CheckOutTime IS NULL;

                IF COL_LENGTH('Attendances', 'CheckInTime') IS NOT NULL
                    ALTER TABLE Attendances ALTER COLUMN CheckInTime time NOT NULL;

                IF COL_LENGTH('Attendances', 'CheckOutTime') IS NOT NULL
                    ALTER TABLE Attendances ALTER COLUMN CheckOutTime time NOT NULL;
                """);
        }
    }
}
