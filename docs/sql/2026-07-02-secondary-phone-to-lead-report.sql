/*
Manual SQL: move SecondaryPhoneNumber from Reservations to LeadAssignments (patient/lead call report).
Run on SQL Server after backing up the database.
No EF migration was generated for this change.
*/

BEGIN TRANSACTION;

IF COL_LENGTH('LeadAssignments', 'SecondaryPhoneNumber') IS NULL
    ALTER TABLE LeadAssignments ADD SecondaryPhoneNumber nvarchar(20) NULL;

UPDATE la
SET la.SecondaryPhoneNumber = r.SecondaryPhoneNumber
FROM LeadAssignments la
INNER JOIN Reservations r ON r.LeadAssignmentId = la.Id
WHERE r.SecondaryPhoneNumber IS NOT NULL
  AND LTRIM(RTRIM(r.SecondaryPhoneNumber)) <> N''
  AND (la.SecondaryPhoneNumber IS NULL OR LTRIM(RTRIM(la.SecondaryPhoneNumber)) = N'');

IF COL_LENGTH('Reservations', 'SecondaryPhoneNumber') IS NOT NULL
    ALTER TABLE Reservations DROP COLUMN SecondaryPhoneNumber;

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Reservations_DueConsultantConfirmations' AND object_id = OBJECT_ID('Reservations'))
    DROP INDEX IX_Reservations_DueConsultantConfirmations ON Reservations;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Reservations_DueConsultantConfirmations' AND object_id = OBJECT_ID('Reservations'))
    CREATE INDEX IX_Reservations_DueConsultantConfirmations
        ON Reservations (ConsultantProfileId, AttendanceConfirmationStatus, IsCanceled, ReservationAt)
        INCLUDE (LeadAssignmentId, PatientUserId, Description);

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_LeadAssignments_ReservationDashboardFields' AND object_id = OBJECT_ID('LeadAssignments'))
    DROP INDEX IX_LeadAssignments_ReservationDashboardFields ON LeadAssignments;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_LeadAssignments_ReservationDashboardFields' AND object_id = OBJECT_ID('LeadAssignments'))
    CREATE INDEX IX_LeadAssignments_ReservationDashboardFields
        ON LeadAssignments (Id)
        INCLUDE (UserName, PhoneNumber, PatientCity, PatientRegion, BusinessName, AttendanceProbabilityPercent, SecondaryPhoneNumber);

COMMIT TRANSACTION;
