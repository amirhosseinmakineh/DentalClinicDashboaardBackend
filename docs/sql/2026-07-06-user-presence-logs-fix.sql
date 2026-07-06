/*
Emergency fix when backend crashes on startup with:
  PendingModelChangesWarning / UserPresenceLogs migration issues

Run AFTER backing up the database. Safe to re-run (idempotent).

Steps:
1) Run this entire script on SQL Server
2) Redeploy backend build that includes updated DentalContextModelSnapshot
*/

BEGIN TRANSACTION;

/* --- Optional: columns from earlier migrations (if still missing) --- */

IF COL_LENGTH('LeadAssignments', 'CallInitiatedAt') IS NULL
    ALTER TABLE LeadAssignments ADD CallInitiatedAt datetime2 NULL;

IF COL_LENGTH('LeadAssignments', 'SecondaryPhoneNumber') IS NULL
    ALTER TABLE LeadAssignments ADD SecondaryPhoneNumber nvarchar(20) NULL;

IF COL_LENGTH('Users', 'PushNotificationToken') IS NULL
    ALTER TABLE Users ADD PushNotificationToken nvarchar(max) NULL;

IF COL_LENGTH('Users', 'PushNotificationToken') IS NOT NULL
    ALTER TABLE Users ALTER COLUMN PushNotificationToken nvarchar(max) NULL;

IF COL_LENGTH('Users', 'LastSeenAt') IS NULL
    ALTER TABLE Users ADD LastSeenAt datetime2 NULL;

/* --- UserPresenceLogs table (admin presence dashboard) --- */

IF OBJECT_ID(N'UserPresenceLogs', N'U') IS NULL
BEGIN
    CREATE TABLE UserPresenceLogs (
        Id bigint NOT NULL IDENTITY(1,1),
        UserId uniqueidentifier NOT NULL,
        EventType int NOT NULL,
        OccurredAt datetime2 NOT NULL,
        Description nvarchar(500) NULL,
        CreatedAt datetime2 NOT NULL,
        UpdatedAt datetime2 NULL,
        IsDeleted bit NOT NULL CONSTRAINT DF_UserPresenceLogs_IsDeleted DEFAULT 0,
        DeletedAt datetime2 NULL,
        CONSTRAINT PK_UserPresenceLogs PRIMARY KEY (Id),
        CONSTRAINT FK_UserPresenceLogs_Users_UserId FOREIGN KEY (UserId) REFERENCES Users(Id)
    );

    CREATE INDEX IX_UserPresenceLogs_UserId ON UserPresenceLogs(UserId);
    CREATE INDEX IX_UserPresenceLogs_OccurredAt ON UserPresenceLogs(OccurredAt);
    CREATE INDEX IX_UserPresenceLogs_UserId_OccurredAt ON UserPresenceLogs(UserId, OccurredAt);
END

/* --- Register EF migrations in history --- */

IF NOT EXISTS (SELECT 1 FROM __EFMigrationsHistory WHERE MigrationId = '20260703102400_AddPushNotificationTokenToUser')
   AND COL_LENGTH('Users', 'PushNotificationToken') IS NOT NULL
    INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES ('20260703102400_AddPushNotificationTokenToUser', '10.0.8');

IF NOT EXISTS (SELECT 1 FROM __EFMigrationsHistory WHERE MigrationId = '20260703213000_ExpandPushNotificationTokenLength')
   AND COL_LENGTH('Users', 'PushNotificationToken') IS NOT NULL
    INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES ('20260703213000_ExpandPushNotificationTokenLength', '10.0.8');

IF NOT EXISTS (SELECT 1 FROM __EFMigrationsHistory WHERE MigrationId = '20260704054000_ExpandPushNotificationTokenForMultiDevice')
   AND COL_LENGTH('Users', 'PushNotificationToken') IS NOT NULL
    INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES ('20260704054000_ExpandPushNotificationTokenForMultiDevice', '10.0.8');

IF NOT EXISTS (SELECT 1 FROM __EFMigrationsHistory WHERE MigrationId = '20260705143000_AddCallInitiatedAtToLeadAssignment')
   AND COL_LENGTH('LeadAssignments', 'CallInitiatedAt') IS NOT NULL
    INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES ('20260705143000_AddCallInitiatedAtToLeadAssignment', '10.0.8');

IF NOT EXISTS (SELECT 1 FROM __EFMigrationsHistory WHERE MigrationId = '20260706081042_SyncPendingModelChangesAuto')
   AND COL_LENGTH('Users', 'LastSeenAt') IS NOT NULL
    INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES ('20260706081042_SyncPendingModelChangesAuto', '10.0.8');

IF NOT EXISTS (SELECT 1 FROM __EFMigrationsHistory WHERE MigrationId = '20260706140000_AddUserPresenceLogs')
   AND OBJECT_ID(N'UserPresenceLogs', N'U') IS NOT NULL
    INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES ('20260706140000_AddUserPresenceLogs', '10.0.8');

COMMIT TRANSACTION;

/* Verify */
SELECT MigrationId, ProductVersion
FROM __EFMigrationsHistory
WHERE MigrationId LIKE '202607%'
ORDER BY MigrationId;

SELECT
    OBJECT_ID(N'UserPresenceLogs', N'U') AS UserPresenceLogsTableId,
    COL_LENGTH('LeadAssignments', 'CallInitiatedAt') AS CallInitiatedAtExists,
    COL_LENGTH('Users', 'LastSeenAt') AS LastSeenAtExists;
