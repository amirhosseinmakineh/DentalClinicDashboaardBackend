BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113125109_AddPatientFileWorkflow'
)
BEGIN
    CREATE TABLE [PatientFileWorkflows] (
        [PatientFileId] bigint NOT NULL,
        [ConsultationCompleted] bit NOT NULL,
        [FinancialCaseId] uniqueidentifier NULL,
        [ApprovedAtUtc] datetime2 NULL,
        [ApprovedByUserId] uniqueidentifier NULL,
        [ApprovedFinanceHash] nvarchar(64) NULL,
        CONSTRAINT [PK_PatientFileWorkflows] PRIMARY KEY ([PatientFileId]),
        CONSTRAINT [FK_PatientFileWorkflows_PatientFiles_PatientFileId] FOREIGN KEY ([PatientFileId]) REFERENCES [PatientFiles] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PatientFileWorkflows_PatientFinancialCases_FinancialCaseId] FOREIGN KEY ([FinancialCaseId]) REFERENCES [PatientFinancialCases] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PatientFileWorkflows_Users_ApprovedByUserId] FOREIGN KEY ([ApprovedByUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113125109_AddPatientFileWorkflow'
)
BEGIN
    CREATE INDEX [IX_PatientFileWorkflows_ApprovedByUserId] ON [PatientFileWorkflows] ([ApprovedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113125109_AddPatientFileWorkflow'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_PatientFileWorkflows_FinancialCaseId] ON [PatientFileWorkflows] ([FinancialCaseId]) WHERE [FinancialCaseId] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113125109_AddPatientFileWorkflow'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261113125109_AddPatientFileWorkflow', N'10.0.8');
END;

COMMIT;
GO

