/*
Manual SQL: move SecondaryPhoneNumber from Reservations to LeadAssignments (patient/lead call report).
Run on SQL Server after backing up the database.
No EF migration was generated for this change.
*/

BEGIN TRANSACTION;

IF COL_LENGTH('LeadAssignments', 'SecondaryPhoneNumber') IS NULL
    ALTER TABLE LeadAssignments ADD SecondaryPhoneNumber nvarchar(20) NULL;

IF COL_LENGTH('Reservations', 'SecondaryPhoneNumber') IS NOT NULL
BEGIN
    UPDATE la
    SET la.SecondaryPhoneNumber = r.SecondaryPhoneNumber
    FROM LeadAssignments la
    INNER JOIN Reservations r ON r.LeadAssignmentId = la.Id
    WHERE r.SecondaryPhoneNumber IS NOT NULL
      AND LTRIM(RTRIM(r.SecondaryPhoneNumber)) <> N''
      AND (la.SecondaryPhoneNumber IS NULL OR LTRIM(RTRIM(la.SecondaryPhoneNumber)) = N'');

    ALTER TABLE Reservations DROP COLUMN SecondaryPhoneNumber;
END

COMMIT TRANSACTION;
GO

/* Index updates must run in separate batches (SQL Server limitation). */

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Reservations_DueConsultantConfirmations' AND object_id = OBJECT_ID('Reservations'))
    DROP INDEX IX_Reservations_DueConsultantConfirmations ON Reservations;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Reservations_DueConsultantConfirmations' AND object_id = OBJECT_ID('Reservations'))
    EXEC(N'CREATE INDEX IX_Reservations_DueConsultantConfirmations
        ON Reservations (ConsultantProfileId, AttendanceConfirmationStatus, IsCanceled, ReservationAt)
        INCLUDE (LeadAssignmentId, PatientUserId, Description)');
GO

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_LeadAssignments_ReservationDashboardFields' AND object_id = OBJECT_ID('LeadAssignments'))
    DROP INDEX IX_LeadAssignments_ReservationDashboardFields ON LeadAssignments;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_LeadAssignments_ReservationDashboardFields' AND object_id = OBJECT_ID('LeadAssignments'))
    EXEC(N'CREATE INDEX IX_LeadAssignments_ReservationDashboardFields
        ON LeadAssignments (Id)
        INCLUDE (UserName, PhoneNumber, PatientCity, PatientRegion, BusinessName, AttendanceProbabilityPercent, SecondaryPhoneNumber)');
GO
