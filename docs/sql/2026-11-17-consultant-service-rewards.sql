SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF OBJECT_ID(N'[dbo].[__EFMigrationsHistory]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117101842_AddConsultantServiceRewardRules'
)
BEGIN
    ALTER TABLE [Reservations] ADD [RewardConsultantProfileId] bigint NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117101842_AddConsultantServiceRewardRules'
)
BEGIN
    ALTER TABLE [Reservations] ADD [ServiceRewardSnapshotJson] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117101842_AddConsultantServiceRewardRules'
)
BEGIN
    CREATE TABLE [ConsultantRewardExecutions] (
        [Id] bigint NOT NULL IDENTITY,
        [FinancialCaseId] uniqueidentifier NOT NULL,
        [ReservationId] bigint NOT NULL,
        [ConsultantProfileId] bigint NOT NULL,
        [ServiceId] int NOT NULL,
        [ToothId] int NOT NULL,
        [PerformedAtUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_ConsultantRewardExecutions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ConsultantRewardExecutions_ConsultantProfiles_ConsultantProfileId] FOREIGN KEY ([ConsultantProfileId]) REFERENCES [ConsultantProfiles] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ConsultantRewardExecutions_PatientFinancialCases_FinancialCaseId] FOREIGN KEY ([FinancialCaseId]) REFERENCES [PatientFinancialCases] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ConsultantRewardExecutions_Reservations_ReservationId] FOREIGN KEY ([ReservationId]) REFERENCES [Reservations] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117101842_AddConsultantServiceRewardRules'
)
BEGIN
    CREATE TABLE [ConsultantServiceRewardRules] (
        [Id] bigint NOT NULL IDENTITY,
        [ServiceId] int NOT NULL,
        [Basis] nvarchar(16) NOT NULL,
        [UnitsPerReward] int NOT NULL,
        [RemainderMode] nvarchar(16) NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [CreatedByUserId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_ConsultantServiceRewardRules] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117101842_AddConsultantServiceRewardRules'
)
BEGIN
    CREATE INDEX [IX_Reservations_RewardConsultantProfileId] ON [Reservations] ([RewardConsultantProfileId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117101842_AddConsultantServiceRewardRules'
)
BEGIN
    CREATE INDEX [IX_ConsultantRewardExecutions_ConsultantProfileId] ON [ConsultantRewardExecutions] ([ConsultantProfileId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117101842_AddConsultantServiceRewardRules'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ConsultantRewardExecutions_FinancialCaseId_ServiceId_ToothId] ON [ConsultantRewardExecutions] ([FinancialCaseId], [ServiceId], [ToothId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117101842_AddConsultantServiceRewardRules'
)
BEGIN
    CREATE INDEX [IX_ConsultantRewardExecutions_ReservationId] ON [ConsultantRewardExecutions] ([ReservationId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117101842_AddConsultantServiceRewardRules'
)
BEGIN
    CREATE INDEX [IX_ConsultantServiceRewardRules_ServiceId_Id] ON [ConsultantServiceRewardRules] ([ServiceId], [Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117101842_AddConsultantServiceRewardRules'
)
BEGIN
    ALTER TABLE [Reservations] ADD CONSTRAINT [FK_Reservations_ConsultantProfiles_RewardConsultantProfileId] FOREIGN KEY ([RewardConsultantProfileId]) REFERENCES [ConsultantProfiles] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261117101842_AddConsultantServiceRewardRules'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261117101842_AddConsultantServiceRewardRules', N'10.0.8');
END;

COMMIT;
GO

