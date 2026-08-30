using DentalDashboard.Infrastracture.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DentalDashboard.Infrastracture.Migrations;

[DbContext(typeof(DentalContext))]
[Migration("20260830200000_ConvertPatientFinanceKeysToGuid")]
public partial class ConvertPatientFinanceKeysToGuid : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
IF EXISTS (
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[dbo].[PatientFinancialCases]')
      AND name = N'Id'
      AND system_type_id = TYPE_ID(N'bigint'))
BEGIN
    EXEC sp_executesql N'
    CREATE TABLE [dbo].[PatientFinancialCases_Guid] (
        [Id] uniqueidentifier NOT NULL,
        [LegacyId] bigint NOT NULL,
        [PatientId] uniqueidentifier NOT NULL,
        [Service] int NOT NULL,
        [TotalAmount] decimal(18,2) NOT NULL,
        [AgreementType] int NOT NULL,
        [Status] int NOT NULL,
        [CreatedByUserId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL
    );

    INSERT INTO [dbo].[PatientFinancialCases_Guid]
        ([Id], [LegacyId], [PatientId], [Service], [TotalAmount], [AgreementType], [Status],
         [CreatedByUserId], [CreatedAt], [UpdatedAt], [IsDeleted], [DeletedAt])
    SELECT NEWID(), c.[Id], p.[UserId], c.[Service], c.[TotalAmount], c.[AgreementType], c.[Status],
           c.[CreatedBySecretaryUserId], c.[CreatedAt], c.[UpdatedAt], c.[IsDeleted], c.[DeletedAt]
    FROM [dbo].[PatientFinancialCases] c
    INNER JOIN [dbo].[PatientProfiles] p ON p.[Id] = c.[PatientId];

    CREATE TABLE [dbo].[PatientCheques_Guid] (
        [Id] bigint IDENTITY NOT NULL, [PatientFinancialCaseId] uniqueidentifier NOT NULL,
        [Amount] decimal(18,2) NOT NULL, [SayadNumber] nvarchar(32) NOT NULL,
        [OwnerName] nvarchar(200) NOT NULL, [DueDate] datetime2 NOT NULL, [Status] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL, [UpdatedAt] datetime2 NULL, [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL
    );
    SET IDENTITY_INSERT [dbo].[PatientCheques_Guid] ON;
    INSERT INTO [dbo].[PatientCheques_Guid]
    SELECT c.[Id], m.[Id], c.[Amount], c.[SayadNumber], c.[OwnerName], c.[DueDate], c.[Status],
           c.[CreatedAt], c.[UpdatedAt], c.[IsDeleted], c.[DeletedAt]
    FROM [dbo].[PatientCheques] c
    INNER JOIN [dbo].[PatientFinancialCases_Guid] m ON m.[LegacyId] = c.[PatientFinancialCaseId];
    SET IDENTITY_INSERT [dbo].[PatientCheques_Guid] OFF;

    CREATE TABLE [dbo].[PatientPromissoryNotes_Guid] (
        [Id] bigint IDENTITY NOT NULL, [PatientFinancialCaseId] uniqueidentifier NOT NULL,
        [SerialNumber] nvarchar(64) NOT NULL, [Amount] decimal(18,2) NOT NULL,
        [DueDate] datetime2 NOT NULL, [Status] int NOT NULL, [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL, [IsDeleted] bit NOT NULL, [DeletedAt] datetime2 NULL
    );
    SET IDENTITY_INSERT [dbo].[PatientPromissoryNotes_Guid] ON;
    INSERT INTO [dbo].[PatientPromissoryNotes_Guid]
    SELECT n.[Id], m.[Id], n.[SerialNumber], n.[Amount], n.[DueDate], n.[Status],
           n.[CreatedAt], n.[UpdatedAt], n.[IsDeleted], n.[DeletedAt]
    FROM [dbo].[PatientPromissoryNotes] n
    INNER JOIN [dbo].[PatientFinancialCases_Guid] m ON m.[LegacyId] = n.[PatientFinancialCaseId];
    SET IDENTITY_INSERT [dbo].[PatientPromissoryNotes_Guid] OFF;

    CREATE TABLE [dbo].[PatientDebts_Guid] (
        [Id] bigint IDENTITY NOT NULL, [PatientFinancialCaseId] uniqueidentifier NOT NULL,
        [Amount] decimal(18,2) NOT NULL, [SourceType] int NOT NULL, [SourceId] bigint NOT NULL,
        [Status] int NOT NULL, [DueDate] datetime2 NOT NULL, [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL, [IsDeleted] bit NOT NULL, [DeletedAt] datetime2 NULL
    );
    SET IDENTITY_INSERT [dbo].[PatientDebts_Guid] ON;
    INSERT INTO [dbo].[PatientDebts_Guid]
    SELECT d.[Id], m.[Id], d.[Amount], d.[SourceType], d.[SourceId], d.[Status], d.[DueDate],
           d.[CreatedAt], d.[UpdatedAt], d.[IsDeleted], d.[DeletedAt]
    FROM [dbo].[PatientDebts] d
    INNER JOIN [dbo].[PatientFinancialCases_Guid] m ON m.[LegacyId] = d.[PatientFinancialCaseId];
    SET IDENTITY_INSERT [dbo].[PatientDebts_Guid] OFF;

    CREATE TABLE [dbo].[PatientFinancialTransactions_Guid] (
        [Id] bigint IDENTITY NOT NULL, [PatientFinancialCaseId] uniqueidentifier NOT NULL,
        [Amount] decimal(18,2) NOT NULL, [Type] int NOT NULL, [SourceType] int NOT NULL,
        [SourceId] bigint NOT NULL, [CreatedByUserId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL, [UpdatedAt] datetime2 NULL, [IsDeleted] bit NOT NULL,
        [DeletedAt] datetime2 NULL
    );
    SET IDENTITY_INSERT [dbo].[PatientFinancialTransactions_Guid] ON;
    INSERT INTO [dbo].[PatientFinancialTransactions_Guid]
    SELECT t.[Id], m.[Id], t.[Amount], t.[Type], t.[SourceType], t.[SourceId],
           t.[CreatedBySecretaryUserId], t.[CreatedAt], t.[UpdatedAt], t.[IsDeleted], t.[DeletedAt]
    FROM [dbo].[PatientFinancialTransactions] t
    INNER JOIN [dbo].[PatientFinancialCases_Guid] m ON m.[LegacyId] = t.[PatientFinancialCaseId];
    SET IDENTITY_INSERT [dbo].[PatientFinancialTransactions_Guid] OFF;

    DROP TABLE [dbo].[PatientFinancialTransactions];
    DROP TABLE [dbo].[PatientDebts];
    DROP TABLE [dbo].[PatientPromissoryNotes];
    DROP TABLE [dbo].[PatientCheques];
    DROP TABLE [dbo].[PatientFinancialCases];

    ALTER TABLE [dbo].[PatientFinancialCases_Guid] DROP COLUMN [LegacyId];
    EXEC sp_rename N''dbo.PatientFinancialCases_Guid'', N''PatientFinancialCases'';
    EXEC sp_rename N''dbo.PatientCheques_Guid'', N''PatientCheques'';
    EXEC sp_rename N''dbo.PatientPromissoryNotes_Guid'', N''PatientPromissoryNotes'';
    EXEC sp_rename N''dbo.PatientDebts_Guid'', N''PatientDebts'';
    EXEC sp_rename N''dbo.PatientFinancialTransactions_Guid'', N''PatientFinancialTransactions'';

    ALTER TABLE [dbo].[PatientFinancialCases] ADD CONSTRAINT [PK_PatientFinancialCases] PRIMARY KEY ([Id]);
    ALTER TABLE [dbo].[PatientFinancialCases] ADD CONSTRAINT [FK_PatientFinancialCases_Users_PatientId]
        FOREIGN KEY ([PatientId]) REFERENCES [dbo].[Users] ([Id]);
    ALTER TABLE [dbo].[PatientFinancialCases] ADD CONSTRAINT [FK_PatientFinancialCases_Users_CreatedByUserId]
        FOREIGN KEY ([CreatedByUserId]) REFERENCES [dbo].[Users] ([Id]);
    CREATE INDEX [IX_PatientFinancialCases_PatientId] ON [dbo].[PatientFinancialCases] ([PatientId]);

    ALTER TABLE [dbo].[PatientCheques] ADD CONSTRAINT [PK_PatientCheques] PRIMARY KEY ([Id]);
    ALTER TABLE [dbo].[PatientCheques] ADD CONSTRAINT [FK_PatientCheques_PatientFinancialCases_PatientFinancialCaseId]
        FOREIGN KEY ([PatientFinancialCaseId]) REFERENCES [dbo].[PatientFinancialCases] ([Id]);
    CREATE INDEX [IX_PatientCheques_PatientFinancialCaseId_DueDate_Status]
        ON [dbo].[PatientCheques] ([PatientFinancialCaseId], [DueDate], [Status]);

    ALTER TABLE [dbo].[PatientPromissoryNotes] ADD CONSTRAINT [PK_PatientPromissoryNotes] PRIMARY KEY ([Id]);
    ALTER TABLE [dbo].[PatientPromissoryNotes] ADD CONSTRAINT [FK_PatientPromissoryNotes_PatientFinancialCases_PatientFinancialCaseId]
        FOREIGN KEY ([PatientFinancialCaseId]) REFERENCES [dbo].[PatientFinancialCases] ([Id]);
    CREATE INDEX [IX_PatientPromissoryNotes_PatientFinancialCaseId_DueDate_Status]
        ON [dbo].[PatientPromissoryNotes] ([PatientFinancialCaseId], [DueDate], [Status]);

    ALTER TABLE [dbo].[PatientDebts] ADD CONSTRAINT [PK_PatientDebts] PRIMARY KEY ([Id]);
    ALTER TABLE [dbo].[PatientDebts] ADD CONSTRAINT [FK_PatientDebts_PatientFinancialCases_PatientFinancialCaseId]
        FOREIGN KEY ([PatientFinancialCaseId]) REFERENCES [dbo].[PatientFinancialCases] ([Id]);
    CREATE UNIQUE INDEX [IX_PatientDebts_SourceType_SourceId] ON [dbo].[PatientDebts] ([SourceType], [SourceId]);
    CREATE INDEX [IX_PatientDebts_PatientFinancialCaseId_Status_DueDate]
        ON [dbo].[PatientDebts] ([PatientFinancialCaseId], [Status], [DueDate]);

    ALTER TABLE [dbo].[PatientFinancialTransactions] ADD CONSTRAINT [PK_PatientFinancialTransactions] PRIMARY KEY ([Id]);
    ALTER TABLE [dbo].[PatientFinancialTransactions] ADD CONSTRAINT [FK_PatientFinancialTransactions_PatientFinancialCases_PatientFinancialCaseId]
        FOREIGN KEY ([PatientFinancialCaseId]) REFERENCES [dbo].[PatientFinancialCases] ([Id]);
    ALTER TABLE [dbo].[PatientFinancialTransactions] ADD CONSTRAINT [FK_PatientFinancialTransactions_Users_CreatedByUserId]
        FOREIGN KEY ([CreatedByUserId]) REFERENCES [dbo].[Users] ([Id]);
    CREATE INDEX [IX_PatientFinancialTransactions_PatientFinancialCaseId]
        ON [dbo].[PatientFinancialTransactions] ([PatientFinancialCaseId]);
    CREATE UNIQUE INDEX [IX_PatientFinancialTransactions_SourceType_SourceId_Type]
        ON [dbo].[PatientFinancialTransactions] ([SourceType], [SourceId], [Type]);
    ';
END
""");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // The old bigint identifiers have no lossless representation after new
        // GUID financial cases are created, so this data migration is irreversible.
    }
}
