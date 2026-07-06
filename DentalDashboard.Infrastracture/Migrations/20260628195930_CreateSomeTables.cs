using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DentalDashboard.Infrastracture.Migrations
{
    /// <inheritdoc />
    public partial class CreateSomeTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Idempotent: manual SQL may have already moved/dropped these reservation columns.
            migrationBuilder.Sql("""
                IF COL_LENGTH('Reservations', 'AttendancePrediction') IS NOT NULL
                    ALTER TABLE Reservations DROP COLUMN AttendancePrediction;

                IF COL_LENGTH('Reservations', 'AttendanceProbabilityPercent') IS NOT NULL
                    ALTER TABLE Reservations DROP COLUMN AttendanceProbabilityPercent;

                IF COL_LENGTH('Reservations', 'PatientCity') IS NOT NULL
                    ALTER TABLE Reservations DROP COLUMN PatientCity;

                IF COL_LENGTH('Reservations', 'SecondaryPhoneNumber') IS NULL
                    ALTER TABLE Reservations ADD SecondaryPhoneNumber nvarchar(20) NULL;

                IF COL_LENGTH('LeadAssignments', 'AttendanceProbabilityPercent') IS NULL
                    ALTER TABLE LeadAssignments ADD AttendanceProbabilityPercent int NULL;

                IF COL_LENGTH('LeadAssignments', 'BusinessName') IS NULL
                    ALTER TABLE LeadAssignments ADD BusinessName nvarchar(200) NULL;

                IF COL_LENGTH('LeadAssignments', 'PatientCity') IS NULL
                    ALTER TABLE LeadAssignments ADD PatientCity nvarchar(100) NULL;

                IF COL_LENGTH('LeadAssignments', 'PatientRegion') IS NULL
                    ALTER TABLE LeadAssignments ADD PatientRegion nvarchar(100) NULL;

                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_LeadAssignments_ReportSubmittedAt' AND object_id = OBJECT_ID('LeadAssignments'))
                    CREATE INDEX IX_LeadAssignments_ReportSubmittedAt ON LeadAssignments (ReportSubmittedAt);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_LeadAssignments_ReportSubmittedAt' AND object_id = OBJECT_ID('LeadAssignments'))
                    DROP INDEX IX_LeadAssignments_ReportSubmittedAt ON LeadAssignments;

                IF COL_LENGTH('Reservations', 'SecondaryPhoneNumber') IS NOT NULL
                    ALTER TABLE Reservations DROP COLUMN SecondaryPhoneNumber;

                IF COL_LENGTH('LeadAssignments', 'AttendanceProbabilityPercent') IS NOT NULL
                    ALTER TABLE LeadAssignments DROP COLUMN AttendanceProbabilityPercent;

                IF COL_LENGTH('LeadAssignments', 'BusinessName') IS NOT NULL
                    ALTER TABLE LeadAssignments DROP COLUMN BusinessName;

                IF COL_LENGTH('LeadAssignments', 'PatientCity') IS NOT NULL
                    ALTER TABLE LeadAssignments DROP COLUMN PatientCity;

                IF COL_LENGTH('LeadAssignments', 'PatientRegion') IS NOT NULL
                    ALTER TABLE LeadAssignments DROP COLUMN PatientRegion;

                IF COL_LENGTH('Reservations', 'AttendancePrediction') IS NULL
                    ALTER TABLE Reservations ADD AttendancePrediction nvarchar(1000) NOT NULL CONSTRAINT DF_Reservations_AttendancePrediction_Down DEFAULT (N'');

                IF COL_LENGTH('Reservations', 'AttendanceProbabilityPercent') IS NULL
                    ALTER TABLE Reservations ADD AttendanceProbabilityPercent int NOT NULL CONSTRAINT DF_Reservations_AttendanceProbabilityPercent_Down DEFAULT (0);

                IF COL_LENGTH('Reservations', 'PatientCity') IS NULL
                    ALTER TABLE Reservations ADD PatientCity nvarchar(100) NOT NULL CONSTRAINT DF_Reservations_PatientCity_Down DEFAULT (N'');
                """);
        }
    }
}
