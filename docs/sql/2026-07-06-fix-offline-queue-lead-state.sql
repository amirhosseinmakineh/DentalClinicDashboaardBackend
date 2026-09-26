/*
Fix offline queue leads stuck in LeadAssignmentState = 1 (New) instead of 4 (Pending).
These leads are invisible to assignment until state is normalized.

Run AFTER backing up the database. Safe to re-run (idempotent).
*/

UPDATE LeadAssignments
SET LeadAssignmentState = 4, -- Pending
    UpdatedAt = SYSDATETIME()
WHERE IsDeleted = 0
  AND AssignmentType = 2
  AND ConsultantProfileId IS NULL
  AND ReportSubmittedAt IS NULL
  AND LeadAssignmentState = 1;
