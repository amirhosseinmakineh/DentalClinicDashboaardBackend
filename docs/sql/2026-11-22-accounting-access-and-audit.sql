-- Execute manually against the existing clinic database. No financial balances are changed.
SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF OBJECT_ID(N'[dbo].[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [dbo].[__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL PRIMARY KEY,
        [ProductVersion] nvarchar(32) NOT NULL
    );
END;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122100449_AddAccountingAccessAndAudit'
)
BEGIN

                    IF NOT EXISTS (SELECT 1 FROM [Roles] WHERE [RoleName] = N'Accountant')
                        INSERT INTO [Roles] ([RoleName], [CreatedAt], [IsDeleted]) VALUES (N'Accountant', SYSUTCDATETIME(), 0);
                
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122100449_AddAccountingAccessAndAudit'
)
BEGIN
    CREATE TABLE [AccountantPermissions] (
        [Id] bigint NOT NULL IDENTITY,
        [UserId] uniqueidentifier NOT NULL,
        [Permission] nvarchar(100) NOT NULL,
        [GrantedByUserId] uniqueidentifier NOT NULL,
        [GrantedAtUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_AccountantPermissions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AccountantPermissions_Users_GrantedByUserId] FOREIGN KEY ([GrantedByUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_AccountantPermissions_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122100449_AddAccountingAccessAndAudit'
)
BEGIN
    CREATE TABLE [AccountingAuditEntries] (
        [Id] bigint NOT NULL IDENTITY,
        [ActorUserId] uniqueidentifier NOT NULL,
        [OccurredAtUtc] datetime2 NOT NULL,
        [Operation] nvarchar(100) NOT NULL,
        [EntityType] nvarchar(100) NOT NULL,
        [EntityId] nvarchar(150) NOT NULL,
        [ChangesJson] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_AccountingAuditEntries] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AccountingAuditEntries_Users_ActorUserId] FOREIGN KEY ([ActorUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122100449_AddAccountingAccessAndAudit'
)
BEGIN
    CREATE INDEX [IX_AccountantPermissions_GrantedByUserId] ON [AccountantPermissions] ([GrantedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122100449_AddAccountingAccessAndAudit'
)
BEGIN
    CREATE UNIQUE INDEX [IX_AccountantPermissions_UserId_Permission] ON [AccountantPermissions] ([UserId], [Permission]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122100449_AddAccountingAccessAndAudit'
)
BEGIN
    CREATE INDEX [IX_AccountingAuditEntries_ActorUserId] ON [AccountingAuditEntries] ([ActorUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122100449_AddAccountingAccessAndAudit'
)
BEGIN
    CREATE INDEX [IX_AccountingAuditEntries_EntityType_EntityId] ON [AccountingAuditEntries] ([EntityType], [EntityId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122100449_AddAccountingAccessAndAudit'
)
BEGIN
    CREATE INDEX [IX_AccountingAuditEntries_OccurredAtUtc] ON [AccountingAuditEntries] ([OccurredAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261122100449_AddAccountingAccessAndAudit'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261122100449_AddAccountingAccessAndAudit', N'10.0.8');
END;

COMMIT;
GO

