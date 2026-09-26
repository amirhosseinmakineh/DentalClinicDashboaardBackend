using DentalDashboard.Infrastracture.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DentalDashboard.Infrastracture.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(DentalContext))]
    [Migration("20260716120000_AddReservationAttendancePrediction")]
    public partial class AddReservationAttendancePrediction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Older databases may contain this column from a manual deployment while the
            // migration itself is missing from __EFMigrationsHistory.
            migrationBuilder.Sql(
                """
                IF COL_LENGTH(N'dbo.Reservations', N'AttendancePrediction') IS NULL
                BEGIN
                    ALTER TABLE [dbo].[Reservations]
                        ADD [AttendancePrediction] nvarchar(500) NULL;
                END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF COL_LENGTH(N'dbo.Reservations', N'AttendancePrediction') IS NOT NULL
                BEGIN
                    ALTER TABLE [dbo].[Reservations] DROP COLUMN [AttendancePrediction];
                END;
                """);
        }
    }
}
