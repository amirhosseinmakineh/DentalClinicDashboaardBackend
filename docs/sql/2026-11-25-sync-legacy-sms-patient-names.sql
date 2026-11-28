-- Manual SQL Server repair. Preview by default; change @Apply to 1 after reviewing.
-- SmsPanel = 5. Only replaces SMS placeholders; never overwrites an existing real name.
SET NOCOUNT ON;
SET XACT_ABORT ON;
DECLARE @Apply bit = 0;

DROP TABLE IF EXISTS #SmsNames;
DROP TABLE IF EXISTS #SmsChanges;
DROP TABLE IF EXISTS #SmsApplied;

;WITH ReportNames AS
(
    SELECT l.PhoneNumber, LTRIM(RTRIM(l.UserName)) AS FullName
    FROM dbo.LeadAssignments AS l
    WHERE l.SourceType = 5 AND l.IsDeleted = 0 AND l.ReportSubmittedAt IS NOT NULL
      AND NULLIF(LTRIM(RTRIM(l.PhoneNumber)), N'') IS NOT NULL
      AND NULLIF(LTRIM(RTRIM(l.UserName)), N'') IS NOT NULL
      AND l.UserName NOT LIKE N'%پیامک%'
      AND LOWER(l.UserName) NOT LIKE N'%sms%'
), Unambiguous AS
(
    SELECT PhoneNumber, MAX(FullName) AS FullName
    FROM ReportNames GROUP BY PhoneNumber HAVING COUNT(DISTINCT FullName) = 1
)
SELECT PhoneNumber, FullName,
       LEFT(FullName, CHARINDEX(N' ', FullName + N' ') - 1) AS FirstName,
       LTRIM(SUBSTRING(FullName, CHARINDEX(N' ', FullName + N' ') + 1, 4000)) AS LastName
INTO #SmsNames
FROM Unambiguous
WHERE LEN(FullName) BETWEEN 2 AND 201
  AND FullName LIKE N'%[آ-ی]%';

DELETE FROM #SmsNames WHERE LEN(FirstName) > 100 OR LEN(LastName) > 100;

CREATE TABLE #SmsChanges
(
    SourceTable nvarchar(40) NOT NULL,
    RecordId nvarchar(50) NOT NULL,
    PhoneNumber nvarchar(20) NOT NULL,
    OldName nvarchar(500) NOT NULL,
    NewName nvarchar(201) NOT NULL,
    FirstName nvarchar(100) NOT NULL,
    LastName nvarchar(100) NOT NULL
);

INSERT #SmsChanges
SELECT N'LeadAssignments', CONVERT(nvarchar(50), l.Id), l.PhoneNumber, l.UserName,
       n.FullName, n.FirstName, n.LastName
FROM dbo.LeadAssignments AS l JOIN #SmsNames AS n ON n.PhoneNumber = l.PhoneNumber
WHERE l.SourceType = 5 AND l.IsDeleted = 0
  AND (l.UserName LIKE N'%پنل%پیامک%' OR LOWER(l.UserName) LIKE N'%sms%panel%');

INSERT #SmsChanges
SELECT N'Users', CONVERT(nvarchar(50), u.Id), u.PhoneNumber,
       LTRIM(RTRIM(CONCAT(u.FirstName, N' ', u.LastName))), n.FullName, n.FirstName, n.LastName
FROM dbo.Users AS u JOIN #SmsNames AS n ON n.PhoneNumber = u.PhoneNumber
WHERE u.IsDeleted = 0
  AND (CONCAT(u.FirstName, N' ', u.LastName) LIKE N'%پنل%پیامک%'
       OR LOWER(CONCAT(u.FirstName, N' ', u.LastName)) LIKE N'%sms%panel%')
  AND (EXISTS (SELECT 1 FROM dbo.PatientProfiles AS p WHERE p.UserId = u.Id AND p.IsDeleted = 0)
       OR EXISTS (SELECT 1 FROM dbo.UserRoles AS ur JOIN dbo.Roles AS r ON r.Id = ur.RoleId
                  WHERE ur.UserId = u.Id AND ur.IsDeleted = 0 AND r.IsDeleted = 0 AND r.RoleName = N'Patient'));

INSERT #SmsChanges
SELECT N'PatientFiles', CONVERT(nvarchar(50), p.Id), p.PhoneNumber,
       LTRIM(RTRIM(CONCAT(p.FirstName, N' ', p.LastName))), n.FullName, n.FirstName, n.LastName
FROM dbo.PatientFiles AS p JOIN #SmsNames AS n ON n.PhoneNumber = p.PhoneNumber
WHERE p.IsDeleted = 0
  AND (CONCAT(p.FirstName, N' ', p.LastName) LIKE N'%پنل%پیامک%'
       OR LOWER(CONCAT(p.FirstName, N' ', p.LastName)) LIKE N'%sms%panel%');

INSERT #SmsChanges
SELECT N'AdminSheetLeads', CONVERT(nvarchar(50), p.Id), p.PhoneNumber,
       LTRIM(RTRIM(CONCAT(p.FirstName, N' ', p.LastName))), n.FullName, n.FirstName, n.LastName
FROM dbo.AdminSheetLeads AS p JOIN #SmsNames AS n ON n.PhoneNumber = p.PhoneNumber
WHERE p.IsDeleted = 0
  AND (CONCAT(p.FirstName, N' ', p.LastName) LIKE N'%پنل%پیامک%'
       OR LOWER(CONCAT(p.FirstName, N' ', p.LastName)) LIKE N'%sms%panel%');

INSERT #SmsChanges
SELECT N'PatientReferrals', CONVERT(nvarchar(50), p.Id), p.ReferredPhoneNumber,
       LTRIM(RTRIM(CONCAT(p.ReferredFirstName, N' ', p.ReferredLastName))), n.FullName, n.FirstName, n.LastName
FROM dbo.PatientReferrals AS p JOIN #SmsNames AS n ON n.PhoneNumber = p.ReferredPhoneNumber
WHERE p.IsDeleted = 0
  AND (CONCAT(p.ReferredFirstName, N' ', p.ReferredLastName) LIKE N'%پنل%پیامک%'
       OR LOWER(CONCAT(p.ReferredFirstName, N' ', p.ReferredLastName)) LIKE N'%sms%panel%');

SELECT SourceTable, RecordId, PhoneNumber, OldName, NewName
FROM #SmsChanges ORDER BY PhoneNumber, SourceTable;
SELECT SourceTable, COUNT(*) AS ProposedChanges FROM #SmsChanges GROUP BY SourceTable;
SELECT DB_NAME() AS DatabaseName,
       (SELECT COUNT(*) FROM #SmsNames) AS UnambiguousReportedSmsNames,
       (SELECT COUNT(*) FROM #SmsChanges) AS ProposedChanges;

IF @Apply = 0 RETURN;

-- Capture before/after values in the result set; save that output with your database backup.
CREATE TABLE #SmsApplied (SourceTable nvarchar(40), RecordId nvarchar(50), OldName nvarchar(500), NewName nvarchar(201));
BEGIN TRY
    BEGIN TRANSACTION;

    UPDATE l SET UserName = c.NewName
    OUTPUT N'LeadAssignments', CONVERT(nvarchar(50), inserted.Id), deleted.UserName, inserted.UserName INTO #SmsApplied
    FROM dbo.LeadAssignments AS l JOIN #SmsChanges AS c
      ON c.SourceTable = N'LeadAssignments' AND c.RecordId = CONVERT(nvarchar(50), l.Id)
    WHERE l.IsDeleted = 0 AND l.SourceType = 5 AND l.PhoneNumber = c.PhoneNumber AND l.UserName = c.OldName;

    UPDATE u SET FirstName = c.FirstName, LastName = c.LastName
    OUTPUT N'Users', CONVERT(nvarchar(50), inserted.Id), LTRIM(RTRIM(CONCAT(deleted.FirstName, N' ', deleted.LastName))), LTRIM(RTRIM(CONCAT(inserted.FirstName, N' ', inserted.LastName))) INTO #SmsApplied
    FROM dbo.Users AS u JOIN #SmsChanges AS c
      ON c.SourceTable = N'Users' AND c.RecordId = CONVERT(nvarchar(50), u.Id)
    WHERE u.IsDeleted = 0 AND u.PhoneNumber = c.PhoneNumber AND LTRIM(RTRIM(CONCAT(u.FirstName, N' ', u.LastName))) = c.OldName;

    UPDATE p SET FirstName = c.FirstName, LastName = c.LastName
    OUTPUT N'PatientFiles', CONVERT(nvarchar(50), inserted.Id), LTRIM(RTRIM(CONCAT(deleted.FirstName, N' ', deleted.LastName))), LTRIM(RTRIM(CONCAT(inserted.FirstName, N' ', inserted.LastName))) INTO #SmsApplied
    FROM dbo.PatientFiles AS p JOIN #SmsChanges AS c
      ON c.SourceTable = N'PatientFiles' AND c.RecordId = CONVERT(nvarchar(50), p.Id)
    WHERE p.IsDeleted = 0 AND p.PhoneNumber = c.PhoneNumber AND LTRIM(RTRIM(CONCAT(p.FirstName, N' ', p.LastName))) = c.OldName;

    UPDATE p SET FirstName = c.FirstName, LastName = c.LastName
    OUTPUT N'AdminSheetLeads', CONVERT(nvarchar(50), inserted.Id), LTRIM(RTRIM(CONCAT(deleted.FirstName, N' ', deleted.LastName))), LTRIM(RTRIM(CONCAT(inserted.FirstName, N' ', inserted.LastName))) INTO #SmsApplied
    FROM dbo.AdminSheetLeads AS p JOIN #SmsChanges AS c
      ON c.SourceTable = N'AdminSheetLeads' AND c.RecordId = CONVERT(nvarchar(50), p.Id)
    WHERE p.IsDeleted = 0 AND p.PhoneNumber = c.PhoneNumber AND LTRIM(RTRIM(CONCAT(p.FirstName, N' ', p.LastName))) = c.OldName;

    UPDATE p SET ReferredFirstName = c.FirstName, ReferredLastName = c.LastName
    OUTPUT N'PatientReferrals', CONVERT(nvarchar(50), inserted.Id), LTRIM(RTRIM(CONCAT(deleted.ReferredFirstName, N' ', deleted.ReferredLastName))), LTRIM(RTRIM(CONCAT(inserted.ReferredFirstName, N' ', inserted.ReferredLastName))) INTO #SmsApplied
    FROM dbo.PatientReferrals AS p JOIN #SmsChanges AS c
      ON c.SourceTable = N'PatientReferrals' AND c.RecordId = CONVERT(nvarchar(50), p.Id)
    WHERE p.IsDeleted = 0 AND p.ReferredPhoneNumber = c.PhoneNumber AND LTRIM(RTRIM(CONCAT(p.ReferredFirstName, N' ', p.ReferredLastName))) = c.OldName;

    IF (SELECT COUNT(*) FROM #SmsApplied) <> (SELECT COUNT(*) FROM #SmsChanges)
        THROW 50001, 'Some records changed during repair. All updates rolled back. Run the preview again.', 1;

    COMMIT TRANSACTION;
    SELECT * FROM #SmsApplied ORDER BY SourceTable, RecordId;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
