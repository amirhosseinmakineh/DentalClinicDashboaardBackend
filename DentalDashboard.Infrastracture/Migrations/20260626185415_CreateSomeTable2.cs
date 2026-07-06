using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DentalDashboard.Infrastracture.Migrations
{
    /// <inheritdoc />
    public partial class CreateSomeTable2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Idempotent: columns may already exist if manual SQL was applied before this migration ran.
            migrationBuilder.Sql("""
                IF COL_LENGTH('Reservations', 'AttendanceConfirmationStatus') IS NULL
                    ALTER TABLE Reservations ADD AttendanceConfirmationStatus int NOT NULL CONSTRAINT DF_Reservations_AttendanceConfirmationStatus_Migration DEFAULT (0);

                IF COL_LENGTH('Reservations', 'AttendancePrediction') IS NULL
                    ALTER TABLE Reservations ADD AttendancePrediction nvarchar(1000) NOT NULL CONSTRAINT DF_Reservations_AttendancePrediction DEFAULT (N'');

                IF COL_LENGTH('Reservations', 'AttendanceProbabilityPercent') IS NULL
                    ALTER TABLE Reservations ADD AttendanceProbabilityPercent int NOT NULL CONSTRAINT DF_Reservations_AttendanceProbabilityPercent DEFAULT (0);

                IF COL_LENGTH('Reservations', 'AttendanceScoreAppliedAt') IS NULL
                    ALTER TABLE Reservations ADD AttendanceScoreAppliedAt datetime2 NULL;

                IF COL_LENGTH('Reservations', 'AttendanceScoreValue') IS NULL
                    ALTER TABLE Reservations ADD AttendanceScoreValue int NULL;

                IF COL_LENGTH('Reservations', 'ConsultantAttendanceConfirmedAt') IS NULL
                    ALTER TABLE Reservations ADD ConsultantAttendanceConfirmedAt datetime2 NULL;

                IF COL_LENGTH('Reservations', 'ConsultantAttendanceNote') IS NULL
                    ALTER TABLE Reservations ADD ConsultantAttendanceNote nvarchar(1000) NULL;

                IF COL_LENGTH('Reservations', 'ConsultantSaysPatientAttended') IS NULL
                    ALTER TABLE Reservations ADD ConsultantSaysPatientAttended bit NULL;

                IF COL_LENGTH('Reservations', 'IsAttendanceScoreApplied') IS NULL
                    ALTER TABLE Reservations ADD IsAttendanceScoreApplied bit NOT NULL CONSTRAINT DF_Reservations_IsAttendanceScoreApplied_Migration DEFAULT (0);

                IF COL_LENGTH('Reservations', 'PatientCity') IS NULL
                    ALTER TABLE Reservations ADD PatientCity nvarchar(100) NOT NULL CONSTRAINT DF_Reservations_PatientCity DEFAULT (N'');

                IF COL_LENGTH('Reservations', 'SecretaryApprovedConsultantConfirmation') IS NULL
                    ALTER TABLE Reservations ADD SecretaryApprovedConsultantConfirmation bit NULL;

                IF COL_LENGTH('Reservations', 'SecretaryReviewNote') IS NULL
                    ALTER TABLE Reservations ADD SecretaryReviewNote nvarchar(1000) NULL;

                IF COL_LENGTH('Reservations', 'SecretaryReviewedAt') IS NULL
                    ALTER TABLE Reservations ADD SecretaryReviewedAt datetime2 NULL;

                IF COL_LENGTH('Reservations', 'SecretaryUserId') IS NULL
                    ALTER TABLE Reservations ADD SecretaryUserId uniqueidentifier NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF COL_LENGTH('Reservations', 'AttendanceConfirmationStatus') IS NOT NULL
                    ALTER TABLE Reservations DROP COLUMN AttendanceConfirmationStatus;

                IF COL_LENGTH('Reservations', 'AttendancePrediction') IS NOT NULL
                    ALTER TABLE Reservations DROP COLUMN AttendancePrediction;

                IF COL_LENGTH('Reservations', 'AttendanceProbabilityPercent') IS NOT NULL
                    ALTER TABLE Reservations DROP COLUMN AttendanceProbabilityPercent;

                IF COL_LENGTH('Reservations', 'AttendanceScoreAppliedAt') IS NOT NULL
                    ALTER TABLE Reservations DROP COLUMN AttendanceScoreAppliedAt;

                IF COL_LENGTH('Reservations', 'AttendanceScoreValue') IS NOT NULL
                    ALTER TABLE Reservations DROP COLUMN AttendanceScoreValue;

                IF COL_LENGTH('Reservations', 'ConsultantAttendanceConfirmedAt') IS NOT NULL
                    ALTER TABLE Reservations DROP COLUMN ConsultantAttendanceConfirmedAt;

                IF COL_LENGTH('Reservations', 'ConsultantAttendanceNote') IS NOT NULL
                    ALTER TABLE Reservations DROP COLUMN ConsultantAttendanceNote;

                IF COL_LENGTH('Reservations', 'ConsultantSaysPatientAttended') IS NOT NULL
                    ALTER TABLE Reservations DROP COLUMN ConsultantSaysPatientAttended;

                IF COL_LENGTH('Reservations', 'IsAttendanceScoreApplied') IS NOT NULL
                    ALTER TABLE Reservations DROP COLUMN IsAttendanceScoreApplied;

                IF COL_LENGTH('Reservations', 'PatientCity') IS NOT NULL
                    ALTER TABLE Reservations DROP COLUMN PatientCity;

                IF COL_LENGTH('Reservations', 'SecretaryApprovedConsultantConfirmation') IS NOT NULL
                    ALTER TABLE Reservations DROP COLUMN SecretaryApprovedConsultantConfirmation;

                IF COL_LENGTH('Reservations', 'SecretaryReviewNote') IS NOT NULL
                    ALTER TABLE Reservations DROP COLUMN SecretaryReviewNote;

                IF COL_LENGTH('Reservations', 'SecretaryReviewedAt') IS NOT NULL
                    ALTER TABLE Reservations DROP COLUMN SecretaryReviewedAt;

                IF COL_LENGTH('Reservations', 'SecretaryUserId') IS NOT NULL
                    ALTER TABLE Reservations DROP COLUMN SecretaryUserId;
                """);
        }
    }
}
