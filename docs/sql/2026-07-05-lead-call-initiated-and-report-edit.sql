-- Add call initiation tracking for realtime leads
IF COL_LENGTH('LeadAssignments', 'CallInitiatedAt') IS NULL
BEGIN
    ALTER TABLE LeadAssignments ADD CallInitiatedAt DATETIME2 NULL;
END

GO
