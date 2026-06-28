/*
Manual SQL for lead report / reservation changes. Run on SQL Server after backing up the database.
No EF migration was generated for this change.
*/

BEGIN TRANSACTION;

IF COL_LENGTH('LeadAssignments', 'PatientCity') IS NULL
    ALTER TABLE LeadAssignments ADD PatientCity nvarchar(100) NULL;

IF COL_LENGTH('LeadAssignments', 'PatientRegion') IS NULL
    ALTER TABLE LeadAssignments ADD PatientRegion nvarchar(100) NULL;

IF COL_LENGTH('LeadAssignments', 'BusinessName') IS NULL
    ALTER TABLE LeadAssignments ADD BusinessName nvarchar(200) NULL;

IF COL_LENGTH('LeadAssignments', 'AttendanceProbabilityPercent') IS NULL
    ALTER TABLE LeadAssignments ADD AttendanceProbabilityPercent int NULL;

IF COL_LENGTH('Reservations', 'SecondaryPhoneNumber') IS NULL
    ALTER TABLE Reservations ADD SecondaryPhoneNumber nvarchar(20) NULL;

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Reservations') AND name = 'PatientCity')
    ALTER TABLE Reservations DROP COLUMN PatientCity;

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Reservations') AND name = 'AttendanceProbabilityPercent')
    ALTER TABLE Reservations DROP COLUMN AttendanceProbabilityPercent;

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Reservations') AND name = 'AttendancePrediction')
    ALTER TABLE Reservations DROP COLUMN AttendancePrediction;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_LeadAssignments_ReportSubmittedAt' AND object_id = OBJECT_ID('LeadAssignments'))
    CREATE INDEX IX_LeadAssignments_ReportSubmittedAt ON LeadAssignments (ReportSubmittedAt);

COMMIT TRANSACTION;
