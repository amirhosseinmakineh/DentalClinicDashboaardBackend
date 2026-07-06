/*
Emergency fix when AddLeadsAsync fails with:
  Invalid column name 'CallInitiatedAt'

Cause: EF model expects LeadAssignments.CallInitiatedAt but migration was not applied.

Run AFTER backing up the database. Safe to re-run (idempotent).
*/

BEGIN TRANSACTION;

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
   AND COL_LENGTH('LeadAssignments', 'CallInitiatedAt') IS NOT NULL
    INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES ('20260706081042_SyncPendingModelChangesAuto', '10.0.8');

COMMIT TRANSACTION;
