/*
Manual SQL for secretary reservation dashboard and attendance review workflow.
Run on SQL Server after backing up the database.
This script is idempotent and matches the reservation APIs added for the secretary dashboard.
*/

BEGIN TRANSACTION;

/* Required reservation columns used by patient profile completion and attendance review APIs. */
IF COL_LENGTH('Reservations', 'PatientUserId') IS NULL
    ALTER TABLE Reservations ADD PatientUserId uniqueidentifier NULL;

IF COL_LENGTH('Reservations', 'SecondaryPhoneNumber') IS NULL
    ALTER TABLE Reservations ADD SecondaryPhoneNumber nvarchar(20) NULL;

IF COL_LENGTH('Reservations', 'AttendanceConfirmationStatus') IS NULL
    ALTER TABLE Reservations ADD AttendanceConfirmationStatus int NOT NULL CONSTRAINT DF_Reservations_AttendanceConfirmationStatus DEFAULT (1);

IF COL_LENGTH('Reservations', 'ConsultantAttendanceConfirmedAt') IS NULL
    ALTER TABLE Reservations ADD ConsultantAttendanceConfirmedAt datetime2 NULL;

IF COL_LENGTH('Reservations', 'ConsultantSaysPatientAttended') IS NULL
    ALTER TABLE Reservations ADD ConsultantSaysPatientAttended bit NULL;

IF COL_LENGTH('Reservations', 'ConsultantAttendanceNote') IS NULL
    ALTER TABLE Reservations ADD ConsultantAttendanceNote nvarchar(1000) NULL;

IF COL_LENGTH('Reservations', 'SecretaryReviewedAt') IS NULL
    ALTER TABLE Reservations ADD SecretaryReviewedAt datetime2 NULL;

IF COL_LENGTH('Reservations', 'SecretaryUserId') IS NULL
    ALTER TABLE Reservations ADD SecretaryUserId uniqueidentifier NULL;

IF COL_LENGTH('Reservations', 'SecretaryApprovedConsultantConfirmation') IS NULL
    ALTER TABLE Reservations ADD SecretaryApprovedConsultantConfirmation bit NULL;

IF COL_LENGTH('Reservations', 'SecretaryReviewNote') IS NULL
    ALTER TABLE Reservations ADD SecretaryReviewNote nvarchar(1000) NULL;

IF COL_LENGTH('Reservations', 'IsAttendanceScoreApplied') IS NULL
    ALTER TABLE Reservations ADD IsAttendanceScoreApplied bit NOT NULL CONSTRAINT DF_Reservations_IsAttendanceScoreApplied DEFAULT (0);

IF COL_LENGTH('Reservations', 'AttendanceScoreValue') IS NULL
    ALTER TABLE Reservations ADD AttendanceScoreValue int NULL;

IF COL_LENGTH('Reservations', 'AttendanceScoreAppliedAt') IS NULL
    ALTER TABLE Reservations ADD AttendanceScoreAppliedAt datetime2 NULL;

/* Lead columns shown in reservation and secretary dashboards. */
IF COL_LENGTH('LeadAssignments', 'PatientCity') IS NULL
    ALTER TABLE LeadAssignments ADD PatientCity nvarchar(100) NULL;

IF COL_LENGTH('LeadAssignments', 'PatientRegion') IS NULL
    ALTER TABLE LeadAssignments ADD PatientRegion nvarchar(100) NULL;

IF COL_LENGTH('LeadAssignments', 'BusinessName') IS NULL
    ALTER TABLE LeadAssignments ADD BusinessName nvarchar(200) NULL;

IF COL_LENGTH('LeadAssignments', 'AttendanceProbabilityPercent') IS NULL
    ALTER TABLE LeadAssignments ADD AttendanceProbabilityPercent int NULL;

/* Keep legacy reservation prediction columns out of the new read path if they still exist. */
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Reservations') AND name = 'PatientCity')
    ALTER TABLE Reservations DROP COLUMN PatientCity;

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Reservations') AND name = 'AttendanceProbabilityPercent')
    ALTER TABLE Reservations DROP COLUMN AttendanceProbabilityPercent;

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Reservations') AND name = 'AttendancePrediction')
    ALTER TABLE Reservations DROP COLUMN AttendancePrediction;

/* Foreign keys and indexes for profile completion and dashboard reads. */
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Reservations_PatientUserId' AND object_id = OBJECT_ID('Reservations'))
    CREATE INDEX IX_Reservations_PatientUserId ON Reservations (PatientUserId);

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Reservations_Users_PatientUserId')
    ALTER TABLE Reservations ADD CONSTRAINT FK_Reservations_Users_PatientUserId
        FOREIGN KEY (PatientUserId) REFERENCES Users(Id) ON DELETE NO ACTION;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Reservations_SecretaryDashboard' AND object_id = OBJECT_ID('Reservations'))
    CREATE INDEX IX_Reservations_SecretaryDashboard
        ON Reservations (IsCanceled, ReservationAt DESC, Id DESC)
        INCLUDE (ConsultantProfileId, LeadAssignmentId, PatientUserId, AttendanceConfirmationStatus,
                 ConsultantAttendanceConfirmedAt, ConsultantSaysPatientAttended,
                 SecretaryReviewedAt, SecretaryUserId, SecretaryApprovedConsultantConfirmation,
                 IsAttendanceScoreApplied, AttendanceScoreValue, AttendanceScoreAppliedAt);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Reservations_SecretaryWaitingReview' AND object_id = OBJECT_ID('Reservations'))
    CREATE INDEX IX_Reservations_SecretaryWaitingReview
        ON Reservations (AttendanceConfirmationStatus, IsCanceled, ReservationAt DESC, Id DESC)
        INCLUDE (ConsultantProfileId, LeadAssignmentId, PatientUserId, ConsultantSaysPatientAttended,
                 ConsultantAttendanceConfirmedAt, IsAttendanceScoreApplied);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Reservations_DueConsultantConfirmations' AND object_id = OBJECT_ID('Reservations'))
    CREATE INDEX IX_Reservations_DueConsultantConfirmations
        ON Reservations (ConsultantProfileId, AttendanceConfirmationStatus, IsCanceled, ReservationAt)
        INCLUDE (LeadAssignmentId, PatientUserId, SecondaryPhoneNumber, Description);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_LeadAssignments_ReservationDashboardFields' AND object_id = OBJECT_ID('LeadAssignments'))
    CREATE INDEX IX_LeadAssignments_ReservationDashboardFields
        ON LeadAssignments (Id)
        INCLUDE (UserName, PhoneNumber, PatientCity, PatientRegion, BusinessName, AttendanceProbabilityPercent);

COMMIT TRANSACTION;

/* Query: secretary reservation dashboard, same shape as GET /api/Reservation/SecretaryReservations.
   Replace variables with API query params. */
DECLARE @ConsultantProfileId bigint = NULL;
DECLARE @From datetime2 = NULL;
DECLARE @To datetime2 = NULL;
DECLARE @AttendanceConfirmationStatus int = NULL;
DECLARE @OnlyWaitingForSecretaryReview bit = 0;
DECLARE @IncludeCanceled bit = 0;
DECLARE @PageNumber int = 1;
DECLARE @PageSize int = 10;

SELECT
    r.Id,
    r.LeadAssignmentId,
    r.ConsultantProfileId,
    cp.UserId AS ConsultantUserId,
    CONCAT(u.FirstName, N' ', u.LastName) AS ConsultantFullName,
    r.PatientUserId,
    CASE WHEN r.PatientUserId IS NULL THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END AS RequiresPatientProfile,
    r.ReservationAt,
    la.UserName AS PatientName,
    la.PhoneNumber AS PatientPhoneNumber,
    r.SecondaryPhoneNumber,
    ISNULL(la.PatientCity, N'') AS PatientCity,
    la.PatientRegion,
    la.BusinessName,
    la.AttendanceProbabilityPercent,
    r.AttendanceConfirmationStatus,
    r.ConsultantAttendanceConfirmedAt,
    r.ConsultantSaysPatientAttended,
    r.ConsultantAttendanceNote,
    CASE WHEN r.AttendanceConfirmationStatus IN (2, 3) THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END AS IsWaitingForSecretaryReview,
    r.SecretaryReviewedAt,
    r.SecretaryUserId,
    r.SecretaryApprovedConsultantConfirmation,
    r.SecretaryReviewNote,
    r.IsAttendanceScoreApplied,
    r.AttendanceScoreValue,
    r.AttendanceScoreAppliedAt,
    r.Description,
    r.IsCanceled
FROM Reservations r
INNER JOIN LeadAssignments la ON la.Id = r.LeadAssignmentId
INNER JOIN ConsultantProfiles cp ON cp.Id = r.ConsultantProfileId
INNER JOIN Users u ON u.Id = cp.UserId
WHERE (@IncludeCanceled = 1 OR r.IsCanceled = 0)
  AND (@ConsultantProfileId IS NULL OR r.ConsultantProfileId = @ConsultantProfileId)
  AND (@From IS NULL OR r.ReservationAt >= @From)
  AND (@To IS NULL OR r.ReservationAt <= @To)
  AND (@AttendanceConfirmationStatus IS NULL OR r.AttendanceConfirmationStatus = @AttendanceConfirmationStatus)
  AND (@OnlyWaitingForSecretaryReview = 0 OR r.AttendanceConfirmationStatus IN (2, 3))
ORDER BY r.ReservationAt DESC, r.Id DESC
OFFSET ((CASE WHEN @PageNumber <= 0 THEN 1 ELSE @PageNumber END - 1) * CASE WHEN @PageSize <= 0 THEN 10 WHEN @PageSize > 100 THEN 100 ELSE @PageSize END) ROWS
FETCH NEXT (CASE WHEN @PageSize <= 0 THEN 10 WHEN @PageSize > 100 THEN 100 ELSE @PageSize END) ROWS ONLY;

/* Query: consultant due confirmations, same condition as GET /api/Reservation/DueConfirmations. */
DECLARE @DueConsultantProfileId bigint = 0;
DECLARE @Now datetime2 = SYSDATETIME();

SELECT
    r.Id,
    r.LeadAssignmentId,
    r.ConsultantProfileId,
    r.PatientUserId,
    CASE WHEN r.PatientUserId IS NULL THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END AS RequiresPatientProfile,
    r.ReservationAt,
    la.UserName AS PatientName,
    la.PhoneNumber AS PatientPhoneNumber,
    r.SecondaryPhoneNumber,
    ISNULL(la.PatientCity, N'') AS PatientCity,
    la.PatientRegion,
    la.BusinessName,
    la.AttendanceProbabilityPercent,
    r.AttendanceConfirmationStatus,
    CAST(1 AS bit) AS IsDueForConsultantConfirmation,
    r.Description,
    r.IsCanceled
FROM Reservations r
INNER JOIN LeadAssignments la ON la.Id = r.LeadAssignmentId
WHERE r.ConsultantProfileId = @DueConsultantProfileId
  AND r.IsCanceled = 0
  AND r.ReservationAt <= @Now
  AND r.AttendanceConfirmationStatus = 1
ORDER BY r.ReservationAt, r.Id;
