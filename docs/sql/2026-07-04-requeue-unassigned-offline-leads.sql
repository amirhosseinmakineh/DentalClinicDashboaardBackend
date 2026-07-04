-- One-time repair: move unassigned leads stuck in wrong states into the offline assignment queue.
-- Safe to run multiple times; only updates rows that are still unassigned.

UPDATE LeadAssignments
SET AssignmentType = 2,          -- OfflineQueue
    LeadAssignmentState = 4,       -- Pending
    BroadcastStartedAt = NULL,
    BroadcastExpiresAt = NULL,
    RequiresThreeMinuteCall = 0,
    CallDeadlineAt = NULL
WHERE IsDeleted = 0
  AND ConsultantProfileId IS NULL
  AND AssignmentType <> 3        -- ConsultantPatient
  AND (
        AssignmentType = 0
     OR LeadAssignmentState = 0
     OR (AssignmentType = 1 AND LeadAssignmentState = 6)  -- RealTime + Expired
     OR (AssignmentType = 1 AND LeadAssignmentState = 1)  -- RealTime + New
  );
