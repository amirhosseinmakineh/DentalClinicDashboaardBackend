/*
Emergency fix when backend crashes on startup with:
  PendingModelChangesWarning (model/snapshot mismatch)

This is a CODE + DB issue. SQL alone is not enough — redeploy backend after SQL.

Run AFTER backing up the database. Safe to re-run (idempotent).
*/

BEGIN TRANSACTION;

IF COL_LENGTH('Users', 'PushNotificationToken') IS NOT NULL
    ALTER TABLE Users ALTER COLUMN PushNotificationToken nvarchar(max) NULL;

IF COL_LENGTH('Users', 'LastSeenAt') IS NULL
    ALTER TABLE Users ADD LastSeenAt datetime2 NULL;

IF COL_LENGTH('LeadAssignments', 'CallInitiatedAt') IS NULL
    ALTER TABLE LeadAssignments ADD CallInitiatedAt datetime2 NULL;

IF COL_LENGTH('LeadAssignments', 'SecondaryPhoneNumber') IS NULL
    ALTER TABLE LeadAssignments ADD SecondaryPhoneNumber nvarchar(20) NULL;

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

IF NOT EXISTS (SELECT 1 FROM __EFMigrationsHistory WHERE MigrationId = '20260706081042_SyncPendingModelChangesAuto')
    INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES ('20260706081042_SyncPendingModelChangesAuto', '10.0.8');

IF NOT EXISTS (SELECT 1 FROM __EFMigrationsHistory WHERE MigrationId = '20260706140000_AddUserPresenceLogs')
   AND OBJECT_ID(N'UserPresenceLogs', N'U') IS NOT NULL
    INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES ('20260706140000_AddUserPresenceLogs', '10.0.8');

COMMIT TRANSACTION;

SELECT MigrationId FROM __EFMigrationsHistory WHERE MigrationId LIKE '202607%' ORDER BY MigrationId;
