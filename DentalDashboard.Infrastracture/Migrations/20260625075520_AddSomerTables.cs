using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DentalDashboard.Infrastracture.Migrations
{
    /// <inheritdoc />
    public partial class AddSomerTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF COL_LENGTH('Reservations', 'PatientUserId') IS NULL
                    ALTER TABLE Reservations ADD PatientUserId uniqueidentifier NULL;

                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Reservations_PatientUserId' AND object_id = OBJECT_ID('Reservations'))
                    CREATE INDEX IX_Reservations_PatientUserId ON Reservations (PatientUserId);

                IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Reservations_Users_PatientUserId')
                    ALTER TABLE Reservations ADD CONSTRAINT FK_Reservations_Users_PatientUserId
                        FOREIGN KEY (PatientUserId) REFERENCES Users(Id) ON DELETE NO ACTION;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Reservations_Users_PatientUserId')
                    ALTER TABLE Reservations DROP CONSTRAINT FK_Reservations_Users_PatientUserId;

                IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Reservations_PatientUserId' AND object_id = OBJECT_ID('Reservations'))
                    DROP INDEX IX_Reservations_PatientUserId ON Reservations;

                IF COL_LENGTH('Reservations', 'PatientUserId') IS NOT NULL
                    ALTER TABLE Reservations DROP COLUMN PatientUserId;
                """);
        }
    }
}
