-- Read-only report. No UPDATE, schema changes, or automatic selection of conflicting names.
-- Candidate source: active leads with a submitted report, matched by the primary phone only.
SET NOCOUNT ON;

;WITH ReportNames AS
(
    SELECT l.Id AS LeadId, l.PhoneNumber, LTRIM(RTRIM(l.UserName)) AS ReportName,
           l.ReportSubmittedAt
    FROM dbo.LeadAssignments AS l
    WHERE l.IsDeleted = 0 AND l.ReportSubmittedAt IS NOT NULL
      AND NULLIF(LTRIM(RTRIM(l.UserName)), N'') IS NOT NULL
      AND NULLIF(LTRIM(RTRIM(l.PhoneNumber)), N'') IS NOT NULL
), NamesPerPhone AS
(
    SELECT PhoneNumber, COUNT(DISTINCT ReportName) AS DifferentReportNames
    FROM ReportNames GROUP BY PhoneNumber
), Sources AS
(
    SELECT r.*, n.DifferentReportNames,
           ROW_NUMBER() OVER (PARTITION BY r.PhoneNumber ORDER BY r.ReportSubmittedAt DESC, r.LeadId DESC) AS RowNumber
    FROM ReportNames AS r
    JOIN NamesPerPhone AS n ON n.PhoneNumber = r.PhoneNumber
), ExistingNames AS
(
    SELECT N'LeadAssignments' AS SourceTable, CONVERT(nvarchar(50), Id) AS RecordId,
           PhoneNumber, LTRIM(RTRIM(UserName)) AS ExistingName
    FROM dbo.LeadAssignments WHERE IsDeleted = 0
    UNION ALL
    SELECT N'AdminSheetLeads', CONVERT(nvarchar(50), Id), PhoneNumber,
           LTRIM(RTRIM(CONCAT(FirstName, N' ', LastName)))
    FROM dbo.AdminSheetLeads WHERE IsDeleted = 0
    UNION ALL
    SELECT N'PatientFiles', CONVERT(nvarchar(50), Id), PhoneNumber,
           LTRIM(RTRIM(CONCAT(FirstName, N' ', LastName)))
    FROM dbo.PatientFiles WHERE IsDeleted = 0
    UNION ALL
    SELECT N'Users', CONVERT(nvarchar(50), u.Id), u.PhoneNumber,
           LTRIM(RTRIM(CONCAT(u.FirstName, N' ', u.LastName)))
    FROM dbo.Users AS u
    WHERE u.IsDeleted = 0 AND
      (EXISTS (SELECT 1 FROM dbo.PatientProfiles AS p WHERE p.UserId = u.Id AND p.IsDeleted = 0)
       OR EXISTS (SELECT 1 FROM dbo.UserRoles AS ur JOIN dbo.Roles AS r ON r.Id = ur.RoleId
                  WHERE ur.UserId = u.Id AND ur.IsDeleted = 0 AND r.RoleName = N'Patient' AND r.IsDeleted = 0))
    UNION ALL
    SELECT N'PatientReferrals', CONVERT(nvarchar(50), Id), ReferredPhoneNumber,
           LTRIM(RTRIM(CONCAT(ReferredFirstName, N' ', ReferredLastName)))
    FROM dbo.PatientReferrals WHERE IsDeleted = 0
)
SELECT s.PhoneNumber, s.LeadId AS CandidateReportLeadId,
       s.ReportName AS CandidateName, s.ReportSubmittedAt,
       s.DifferentReportNames,
       CASE WHEN s.DifferentReportNames > 1 THEN N'Conflicting report names: manual review required'
            ELSE N'Candidate only: verify before overwriting existing patient names' END AS ReviewStatus,
       e.SourceTable, e.RecordId, e.ExistingName
FROM Sources AS s
JOIN ExistingNames AS e ON e.PhoneNumber = s.PhoneNumber
WHERE s.RowNumber = 1 AND ISNULL(e.ExistingName, N'') <> s.ReportName
ORDER BY s.PhoneNumber, e.SourceTable, e.RecordId;
