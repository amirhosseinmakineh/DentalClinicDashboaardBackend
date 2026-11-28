-- Execute manually after the 2026-11-22 accounting SQL. Adds reminder preferences and delivery history only.
SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261124070753_AddAccountingDueReminders'
)
BEGIN
    CREATE TABLE [AccountingReminders] (
        [Id] bigint NOT NULL IDENTITY,
        [UserId] uniqueidentifier NOT NULL,
        [Kind] int NOT NULL,
        [TargetId] bigint NOT NULL,
        [Enabled] bit NOT NULL,
        [UpdatedAtUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_AccountingReminders] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AccountingReminders_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261124070753_AddAccountingDueReminders'
)
BEGIN
    CREATE TABLE [AccountingReminderDeliveries] (
        [Id] bigint NOT NULL IDENTITY,
        [ReminderId] bigint NOT NULL,
        [DueDate] date NOT NULL,
        [ReminderDate] date NOT NULL,
        [LeaseId] uniqueidentifier NULL,
        [LeaseUntilUtc] datetime2 NULL,
        [SentAtUtc] datetime2 NULL,
        [Attempts] int NOT NULL,
        CONSTRAINT [PK_AccountingReminderDeliveries] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AccountingReminderDeliveries_AccountingReminders_ReminderId] FOREIGN KEY ([ReminderId]) REFERENCES [AccountingReminders] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261124070753_AddAccountingDueReminders'
)
BEGIN
    CREATE INDEX [IX_AccountingReminderDeliveries_LeaseId] ON [AccountingReminderDeliveries] ([LeaseId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261124070753_AddAccountingDueReminders'
)
BEGIN
    CREATE UNIQUE INDEX [IX_AccountingReminderDeliveries_ReminderId_DueDate_ReminderDate] ON [AccountingReminderDeliveries] ([ReminderId], [DueDate], [ReminderDate]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261124070753_AddAccountingDueReminders'
)
BEGIN
    CREATE INDEX [IX_AccountingReminders_Enabled_Id] ON [AccountingReminders] ([Enabled], [Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261124070753_AddAccountingDueReminders'
)
BEGIN
    CREATE UNIQUE INDEX [IX_AccountingReminders_UserId_Kind_TargetId] ON [AccountingReminders] ([UserId], [Kind], [TargetId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261124070753_AddAccountingDueReminders'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261124070753_AddAccountingDueReminders', N'10.0.8');
END;

COMMIT;
GO

