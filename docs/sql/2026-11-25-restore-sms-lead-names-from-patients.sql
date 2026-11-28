-- Manual repair: use existing patient account/file names to replace SMS lead placeholders.
-- Preview only by default. No change to reports, statuses, rewards, phone numbers, or patient names.
SET NOCOUNT ON;
SET XACT_ABORT ON;
DECLARE @Apply bit = 0;
DROP TABLE IF EXISTS #PatientNamesForSms;
DROP TABLE IF EXISTS #SmsLeadNameRepair;
DROP TABLE IF EXISTS #SmsLeadNameApplied;

;WITH PatientNames AS
(
    SELECT u.PhoneNumber, LTRIM(RTRIM(CONCAT(u.FirstName, N' ', u.LastName))) AS FullName
    FROM dbo.Users AS u
    WHERE u.IsDeleted = 0
      AND (EXISTS (SELECT 1 FROM dbo.PatientProfiles AS p WHERE p.UserId = u.Id AND p.IsDeleted = 0)
           OR EXISTS (SELECT 1 FROM dbo.UserRoles AS ur JOIN dbo.Roles AS r ON r.Id = ur.RoleId
                      WHERE ur.UserId = u.Id AND ur.IsDeleted = 0 AND r.IsDeleted = 0 AND r.RoleName = N'Patient'))
    UNION ALL
    SELECT p.PhoneNumber, LTRIM(RTRIM(CONCAT(p.FirstName, N' ', p.LastName)))
    FROM dbo.PatientFiles AS p WHERE p.IsDeleted = 0
), RealNames AS
(
    SELECT PhoneNumber, FullName FROM PatientNames
    WHERE LEN(FullName) BETWEEN 2 AND 201
      AND FullName NOT LIKE N'%پیامک%' AND LOWER(FullName) NOT LIKE N'%sms%'
      AND FullName NOT LIKE N'%test%' AND FullName NOT LIKE N'%تست%'
      AND FullName LIKE N'%[آ-ی]%'
)
SELECT PhoneNumber, MAX(FullName) AS FullName, COUNT(DISTINCT FullName) AS NameCount
INTO #PatientNamesForSms
FROM RealNames GROUP BY PhoneNumber;

SELECT l.Id AS LeadId, l.PhoneNumber, l.UserName AS OldName,
       n.FullName AS NewName
INTO #SmsLeadNameRepair
FROM dbo.LeadAssignments AS l
JOIN #PatientNamesForSms AS n ON n.PhoneNumber = l.PhoneNumber AND n.NameCount = 1
WHERE l.IsDeleted = 0 AND l.SourceType = 5
  AND (l.UserName LIKE N'%پنل%پیامک%' OR LOWER(l.UserName) LIKE N'%sms%panel%');

SELECT LeadId, PhoneNumber, OldName, NewName FROM #SmsLeadNameRepair ORDER BY PhoneNumber, LeadId;
SELECT DB_NAME() AS DatabaseName, COUNT(*) AS ProposedLeadChanges FROM #SmsLeadNameRepair;

-- All SMS placeholders without an unambiguous patient name; no TOP limit.
SELECT l.Id AS LeadId, l.PhoneNumber, l.UserName,
       CASE WHEN n.NameCount > 1 THEN N'Conflicting patient account/file names; manual review required'
            ELSE N'No usable patient account/file name was found' END AS SkipReason
FROM dbo.LeadAssignments AS l
LEFT JOIN #PatientNamesForSms AS n ON n.PhoneNumber = l.PhoneNumber
WHERE l.IsDeleted = 0 AND l.SourceType = 5
  AND (l.UserName LIKE N'%پنل%پیامک%' OR LOWER(l.UserName) LIKE N'%sms%panel%')
  AND (n.PhoneNumber IS NULL OR n.NameCount > 1)
ORDER BY l.PhoneNumber, l.Id;

IF @Apply = 0 RETURN;

CREATE TABLE #SmsLeadNameApplied (LeadId bigint, PhoneNumber nvarchar(20), OldName nvarchar(max), NewName nvarchar(201));
BEGIN TRY
    BEGIN TRANSACTION;
    UPDATE l SET UserName = r.NewName
    OUTPUT inserted.Id, inserted.PhoneNumber, deleted.UserName, inserted.UserName INTO #SmsLeadNameApplied
    FROM dbo.LeadAssignments AS l JOIN #SmsLeadNameRepair AS r ON r.LeadId = l.Id
    WHERE l.SourceType = 5 AND l.IsDeleted = 0 AND l.PhoneNumber = r.PhoneNumber AND l.UserName = r.OldName;

    IF (SELECT COUNT(*) FROM #SmsLeadNameApplied) <> (SELECT COUNT(*) FROM #SmsLeadNameRepair)
        THROW 50001, 'Lead data changed during repair; all updates rolled back. Run the preview again.', 1;

    COMMIT TRANSACTION;
    SELECT * FROM #SmsLeadNameApplied ORDER BY PhoneNumber, LeadId;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
