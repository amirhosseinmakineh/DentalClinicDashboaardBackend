/*
Emergency fix when backend crashes on SyncPendingModelChangesAuto with:
  The size (16000) given to the column 'PushNotificationToken' exceeds the maximum allowed (8000).

SQL Server nvarchar(n) max is 4000 characters; use nvarchar(max) for larger payloads.
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

IF NOT EXISTS (SELECT 1 FROM __EFMigrationsHistory WHERE MigrationId = '20260706081042_SyncPendingModelChangesAuto')
    INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES ('20260706081042_SyncPendingModelChangesAuto', '10.0.8');

COMMIT TRANSACTION;
