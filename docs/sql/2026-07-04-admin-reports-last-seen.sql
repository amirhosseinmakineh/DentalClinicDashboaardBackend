/*
  Admin reports enhancements - manual SQL (no EF migration)

  Run on SQL Server after backup.
*/

BEGIN TRANSACTION;

IF COL_LENGTH('Users', 'LastSeenAt') IS NULL
    ALTER TABLE Users ADD LastSeenAt datetime2 NULL;

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_Users_LastSeenAt'
      AND object_id = OBJECT_ID('Users')
)
    CREATE INDEX IX_Users_LastSeenAt ON Users (LastSeenAt DESC)
    WHERE IsDeleted = 0;

COMMIT TRANSACTION;

-- Optional verification
SELECT TOP 50
    u.Id,
    u.FirstName,
    u.LastName,
    u.PhoneNumber,
    u.LastSeenAt,
    u.CreatedAt
FROM Users u
WHERE u.IsDeleted = 0
ORDER BY u.LastSeenAt DESC, u.CreatedAt DESC;
