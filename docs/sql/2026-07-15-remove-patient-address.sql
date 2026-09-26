/*
Fix patient profile creation failure after Address field was removed from the API.

Symptom: POST /api/Reservation/CompletePatientProfile returns
"خطا در تشکیل پرونده بیمار: Cannot insert the value NULL into column 'Address'..."

Cause: backend code no longer sends Address, but the PatientProfiles.Address
column still existed as NOT NULL in databases that had not applied the EF migration.

Run on SQL Server after backing up the database. Idempotent.
*/

IF COL_LENGTH('PatientProfiles', 'Address') IS NOT NULL
BEGIN
    ALTER TABLE PatientProfiles DROP COLUMN Address;
END;

IF NOT EXISTS (
    SELECT 1
    FROM __EFMigrationsHistory
    WHERE MigrationId = N'20260715120000_RemovePatientAddress'
)
BEGIN
    INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES (N'20260715120000_RemovePatientAddress', N'10.0.8');
END;
