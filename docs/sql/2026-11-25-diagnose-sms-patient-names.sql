-- Read-only diagnosis. Always returns the summary row, even if no repair candidates exist.
SET NOCOUNT ON;
SELECT DB_NAME() AS DatabaseName,
       COUNT_BIG(*) AS TotalActiveLeads,
       COUNT_BIG(CASE WHEN SourceType = 5 THEN 1 END) AS SmsSourceLeads,
       COUNT_BIG(CASE WHEN SourceType = 5 AND ReportSubmittedAt IS NOT NULL THEN 1 END) AS SmsWithReport,
       COUNT_BIG(CASE WHEN SourceType = 5 AND ReportSubmittedAt IS NOT NULL
           AND NULLIF(LTRIM(RTRIM(UserName)), N'') IS NOT NULL
           AND UserName NOT LIKE N'%پیامک%' AND LOWER(UserName) NOT LIKE N'%sms%'
           THEN 1 END) AS SmsReportedNonPlaceholderNames,
       COUNT_BIG(CASE WHEN SourceType = 5 AND ReportSubmittedAt IS NULL
           AND NULLIF(LTRIM(RTRIM(UserName)), N'') IS NOT NULL
           AND UserName NOT LIKE N'%پیامک%' AND LOWER(UserName) NOT LIKE N'%sms%'
           THEN 1 END) AS SmsUnreportedNonPlaceholderNames,
       COUNT_BIG(CASE WHEN UserName LIKE N'%پنل%پیامک%' OR LOWER(UserName) LIKE N'%sms%panel%'
           THEN 1 END) AS PlaceholderLeadsAllSources,
       COUNT_BIG(CASE WHEN SourceType <> 5 AND
           (UserName LIKE N'%پنل%پیامک%' OR LOWER(UserName) LIKE N'%sms%panel%')
           THEN 1 END) AS PlaceholderLeadsWithOtherSource
FROM dbo.LeadAssignments WHERE IsDeleted = 0;

;WITH SmsPhones AS
(
    SELECT DISTINCT PhoneNumber FROM dbo.LeadAssignments
    WHERE IsDeleted = 0 AND (SourceType = 5 OR UserName LIKE N'%پنل%پیامک%' OR LOWER(UserName) LIKE N'%sms%panel%')
), LeadNames AS
(
    SELECT l.Id, l.PhoneNumber, l.SourceType, l.UserName, l.ReportSubmittedAt,
           CASE WHEN l.UserName LIKE N'%پنل%پیامک%' OR LOWER(l.UserName) LIKE N'%sms%panel%' THEN 1 ELSE 0 END AS IsPlaceholder,
           ROW_NUMBER() OVER (PARTITION BY l.PhoneNumber ORDER BY l.ReportSubmittedAt DESC, l.Id DESC) AS RowNumber
    FROM dbo.LeadAssignments AS l JOIN SmsPhones AS s ON s.PhoneNumber = l.PhoneNumber
    WHERE l.IsDeleted = 0
)
SELECT TOP (300) l.PhoneNumber, l.Id AS LeadId, l.SourceType, l.UserName AS LeadName,
       l.ReportSubmittedAt, l.IsPlaceholder,
       u.AccountName, p.FileName,
       CASE WHEN l.IsPlaceholder = 1 THEN N'Lead still has an SMS placeholder; this is not a corrected name'
            WHEN l.ReportSubmittedAt IS NULL THEN N'Name exists but no submitted report; excluded by repair query'
            WHEN l.SourceType <> 5 THEN N'Reported name belongs to another source; excluded by repair query'
            ELSE N'Non-placeholder reported SMS name; compare with the account/file before repair' END AS Diagnosis
FROM LeadNames AS l
OUTER APPLY
(
    SELECT TOP (1) LTRIM(RTRIM(CONCAT(x.FirstName, N' ', x.LastName))) AS AccountName
    FROM dbo.Users AS x WHERE x.PhoneNumber = l.PhoneNumber AND x.IsDeleted = 0
    ORDER BY x.Id
) AS u
OUTER APPLY
(
    SELECT TOP (1) LTRIM(RTRIM(CONCAT(x.FirstName, N' ', x.LastName))) AS FileName
    FROM dbo.PatientFiles AS x WHERE x.PhoneNumber = l.PhoneNumber AND x.IsDeleted = 0
    ORDER BY x.Id DESC
) AS p
WHERE l.IsPlaceholder = 1 OR l.RowNumber = 1 OR l.ReportSubmittedAt IS NOT NULL
ORDER BY l.IsPlaceholder DESC, l.PhoneNumber, l.Id;
