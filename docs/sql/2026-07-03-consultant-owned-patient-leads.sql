/*
Manual SQL: consultant-owned patient leads (LeadAssignmentType = 3 / ConsultantOwned).
Run on SQL Server after backing up the database.
No EF migration was generated for this change.

AssignmentType is already stored as int on LeadAssignments; value 3 is ConsultantOwned.
No schema change is required for the new type.

Optional: verify existing data and document the enum mapping.
*/

-- Enum reference (application layer):
-- 1 = RealTime (آنی)
-- 2 = OfflineQueue (صف آفلاین)
-- 3 = ConsultantOwned (مریض مشاور)

-- Verify no invalid AssignmentType values exist
SELECT AssignmentType, COUNT(*) AS LeadCount
FROM LeadAssignments
WHERE IsDeleted = 0
GROUP BY AssignmentType
ORDER BY AssignmentType;

-- Consultant-owned leads created by the app will have:
--   AssignmentType = 3
--   LeadAssignmentState = 2 (Assigned)
--   ConsultantProfileId IS NOT NULL
--   RequiresThreeMinuteCall = 0
--   NotificationSent = 1

-- Optional index to speed up consultant-owned lead listing (run only if needed)
IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_LeadAssignments_ConsultantOwned'
      AND object_id = OBJECT_ID('LeadAssignments')
)
BEGIN
    CREATE INDEX IX_LeadAssignments_ConsultantOwned
        ON LeadAssignments (ConsultantProfileId, AssignmentType, LeadAssignmentState, ReportSubmittedAt)
        INCLUDE (UserName, PhoneNumber, AssignedAt, PatientCity, PatientRegion, SecondaryPhoneNumber)
        WHERE IsDeleted = 0 AND AssignmentType = 3;
END;
