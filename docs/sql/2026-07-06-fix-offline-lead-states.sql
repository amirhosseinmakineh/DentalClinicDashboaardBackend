/*
Normalize offline lead states to match application rules:
- Unassigned offline queue leads use LeadAssignmentState = 1 (New)
- Offline leads must never remain in Expired (6)

Run AFTER backing up the database. Safe to re-run (idempotent).
*/

-- Unassigned offline queue: New (not Pending)
UPDATE LeadAssignments
SET LeadAssignmentState = 1, -- New
    UpdatedAt = SYSDATETIME()
WHERE IsDeleted = 0
  AND AssignmentType = 2
  AND ConsultantProfileId IS NULL
  AND ReportSubmittedAt IS NULL
  AND LeadAssignmentState = 4; -- Pending

-- Assigned offline leads incorrectly marked Expired -> Assigned
UPDATE LeadAssignments
SET LeadAssignmentState = 2, -- Assigned
    UpdatedAt = SYSDATETIME()
WHERE IsDeleted = 0
  AND AssignmentType = 2
  AND ConsultantProfileId IS NOT NULL
  AND ReportSubmittedAt IS NULL
  AND LeadAssignmentState = 6; -- Expired

-- Unassigned offline leads incorrectly marked Expired -> New
UPDATE LeadAssignments
SET LeadAssignmentState = 1, -- New
    UpdatedAt = SYSDATETIME()
WHERE IsDeleted = 0
  AND AssignmentType = 2
  AND ConsultantProfileId IS NULL
  AND ReportSubmittedAt IS NULL
  AND LeadAssignmentState = 6; -- Expired
