/*
  Consultant patient leads (AssignmentType = 3 / ConsultantPatient)

  API endpoints:
    POST /api/Consultant/CreateConsultantPatientLead
    POST /api/Consultant/AddPatientLead   (alias)

  No schema migration is required. The existing LeadAssignments.AssignmentType
  column already stores enum values as INT. This script documents the new value
  and optional verification queries.
*/

-- New enum value used by application code:
--   1 = RealTime
--   2 = OfflineQueue
--   3 = ConsultantPatient  (consultant-added patient/lead, no online/offline queue)

-- Optional: verify current assignment type distribution
SELECT
    la.AssignmentType,
    COUNT(*) AS LeadCount
FROM LeadAssignments la
WHERE la.IsDeleted = 0
GROUP BY la.AssignmentType
ORDER BY la.AssignmentType;

-- Optional: list consultant-added patient leads
SELECT
    la.Id,
    la.UserName,
    la.PhoneNumber,
    la.ConsultantProfileId,
    la.LeadAssignmentState,
    la.AssignmentType,
    la.AssignedAt,
    la.ReportSubmittedAt,
    la.CreatedAt
FROM LeadAssignments la
WHERE la.IsDeleted = 0
  AND la.AssignmentType = 3
ORDER BY la.Id DESC;
