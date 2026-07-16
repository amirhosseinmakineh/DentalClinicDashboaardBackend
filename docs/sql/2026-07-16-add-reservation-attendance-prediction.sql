-- Add AttendancePrediction column to Reservations for consultant reservation edits
IF COL_LENGTH('Reservations', 'AttendancePrediction') IS NULL
BEGIN
    ALTER TABLE Reservations
    ADD AttendancePrediction NVARCHAR(500) NULL;
END;
