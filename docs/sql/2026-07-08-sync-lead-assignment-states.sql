/*
Sync lead assignment states between backend rules and frontend display.

Run AFTER backing up the database. Safe to re-run (idempotent).

Rules:
- Pending (4) is follow-up AFTER a submitted call report.
- New (1) / Assigned (2) are unreported leads still waiting for action.
*/

-- Unreported leads incorrectly marked Pending -> New/Assigned
UPDATE LeadAssignments
SET LeadAssignmentState = CASE
        WHEN ConsultantProfileId IS NULL THEN 1 -- New
        ELSE 2 -- Assigned
    END,
    UpdatedAt = SYSDATETIME()
WHERE IsDeleted = 0
  AND ReportSubmittedAt IS NULL
  AND LeadAssignmentState = 4; -- Pending

-- Offline leads with follow-up call results still marked Contacted -> Pending
UPDATE LeadAssignments
SET LeadAssignmentState = 4, -- Pending
    UpdatedAt = SYSDATETIME()
WHERE IsDeleted = 0
  AND AssignmentType = 2 -- OfflineQueue
  AND ReportSubmittedAt IS NOT NULL
  AND LeadAssignmentState = 3 -- Contacted
  AND CallResult IN (4, 6, 7, 8); -- NoAnswer, NeedFollowUp, Busy, PatientHungUp

-- Offline leads with rejected call results still marked Contacted -> Rejected
UPDATE LeadAssignments
SET LeadAssignmentState = 7, -- Rejected
    UpdatedAt = SYSDATETIME()
WHERE IsDeleted = 0
  AND AssignmentType = 2 -- OfflineQueue
  AND ReportSubmittedAt IS NOT NULL
  AND LeadAssignmentState = 3 -- Contacted
  AND CallResult IN (3, 5); -- Rejected, WrongNumber
