-- Manual SQL Server rollout. Existing notes retain their previous payment behavior.
IF COL_LENGTH(N'dbo.PatientPromissoryNotes', N'IsGuarantee') IS NULL
BEGIN
    ALTER TABLE [dbo].[PatientPromissoryNotes]
        ADD [IsGuarantee] bit NOT NULL
            CONSTRAINT [DF_PatientPromissoryNotes_IsGuarantee] DEFAULT (0);
END;
