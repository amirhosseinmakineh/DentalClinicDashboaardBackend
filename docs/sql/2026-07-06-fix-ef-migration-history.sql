/*
Emergency fix when backend crashes on startup with:
  Column name 'AttendanceConfirmationStatus' in table 'Reservations' is specified more than once.

Cause: manual SQL (2026-06-29-secretary-reservation-workflow.sql) already added reservation
columns, but EF migrations were not recorded in __EFMigrationsHistory.

Run AFTER backing up the database. Safe to re-run (idempotent).
*/

BEGIN TRANSACTION;

IF NOT EXISTS (SELECT 1 FROM __EFMigrationsHistory WHERE MigrationId = '20260625075520_AddSomerTables')
   AND COL_LENGTH('Reservations', 'PatientUserId') IS NOT NULL
    INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES ('20260625075520_AddSomerTables', '10.0.8');

IF NOT EXISTS (SELECT 1 FROM __EFMigrationsHistory WHERE MigrationId = '20260626185415_CreateSomeTable2')
   AND COL_LENGTH('Reservations', 'AttendanceConfirmationStatus') IS NOT NULL
    INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES ('20260626185415_CreateSomeTable2', '10.0.8');

IF NOT EXISTS (SELECT 1 FROM __EFMigrationsHistory WHERE MigrationId = '20260628195930_CreateSomeTables')
   AND COL_LENGTH('Reservations', 'SecondaryPhoneNumber') IS NOT NULL
   AND COL_LENGTH('LeadAssignments', 'PatientCity') IS NOT NULL
    INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES ('20260628195930_CreateSomeTables', '10.0.8');

IF NOT EXISTS (SELECT 1 FROM __EFMigrationsHistory WHERE MigrationId = '20260706081042_SyncPendingModelChangesAuto')
   AND COL_LENGTH('Users', 'LastSeenAt') IS NOT NULL
    INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES ('20260706081042_SyncPendingModelChangesAuto', '10.0.8');

COMMIT TRANSACTION;
