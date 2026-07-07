-- Fix consultant check-in/check-out 500 errors after Attendance table centralization.
-- Attendances.CheckInTime and CheckOutTime must be nullable so consultants can
-- check in without a checkout time and re-check in after checkout the same day.

IF COL_LENGTH('Attendances', 'CheckInTime') IS NOT NULL
    ALTER TABLE Attendances ALTER COLUMN CheckInTime time NULL;

IF COL_LENGTH('Attendances', 'CheckOutTime') IS NOT NULL
    ALTER TABLE Attendances ALTER COLUMN CheckOutTime time NULL;

IF NOT EXISTS (
    SELECT 1
    FROM __EFMigrationsHistory
    WHERE MigrationId = '20260707053300_MakeAttendanceTimesNullable'
)
BEGIN
    INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES ('20260707053300_MakeAttendanceTimesNullable', '10.0.8');
END
