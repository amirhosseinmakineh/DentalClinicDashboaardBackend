BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024171020_WebsiteConsultationRequests'
)
BEGIN
    IF COL_LENGTH('LeadAssignments', 'SourceType') IS NULL
        THROW 51000, 'Existing lead-source migration must be applied before Website consultation upgrade.', 1;
    IF OBJECT_ID('SecretarySaleServices', 'U') IS NULL
        THROW 51000, 'Existing clinic service catalog must be installed before Website consultation upgrade.', 1;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024171020_WebsiteConsultationRequests'
)
BEGIN
    ALTER TABLE [LeadAssignments] ADD [ConsultationDescription] nvarchar(1000) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024171020_WebsiteConsultationRequests'
)
BEGIN
    ALTER TABLE [LeadAssignments] ADD [ConsultationRequestHash] nvarchar(64) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024171020_WebsiteConsultationRequests'
)
BEGIN
    ALTER TABLE [LeadAssignments] ADD [ConsultationRequestId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024171020_WebsiteConsultationRequests'
)
BEGIN
    CREATE TABLE [LeadRequestedService] (
        [LeadAssignmentId] bigint NOT NULL,
        [ServiceId] bigint NOT NULL,
        CONSTRAINT [PK_LeadRequestedService] PRIMARY KEY ([LeadAssignmentId], [ServiceId]),
        CONSTRAINT [FK_LeadRequestedService_LeadAssignments_LeadAssignmentId] FOREIGN KEY ([LeadAssignmentId]) REFERENCES [LeadAssignments] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_LeadRequestedService_SecretarySaleServices_ServiceId] FOREIGN KEY ([ServiceId]) REFERENCES [SecretarySaleServices] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024171020_WebsiteConsultationRequests'
)
BEGIN
    CREATE INDEX [IX_LeadRequestedService_ServiceId] ON [LeadRequestedService] ([ServiceId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024171020_WebsiteConsultationRequests'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_LeadAssignments_ConsultationRequestId] ON [LeadAssignments] ([ConsultationRequestId]) WHERE [ConsultationRequestId] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024171020_WebsiteConsultationRequests'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261024171020_WebsiteConsultationRequests', N'10.0.8');
END;

COMMIT;
GO

