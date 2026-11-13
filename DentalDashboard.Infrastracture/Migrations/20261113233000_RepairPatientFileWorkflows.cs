using DentalDashboard.Infrastracture.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DentalDashboard.Infrastracture.Migrations;

[DbContext(typeof(DentalContext))]
[Migration("20261113233000_RepairPatientFileWorkflows")]
public partial class RepairPatientFileWorkflows : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Repair missing table/columns when an earlier migration was recorded but schema drifted.
        // All operations are guarded to preserve existing patient data.
        migrationBuilder.Sql(@"IF OBJECT_ID(N'[dbo].[PatientFileWorkflows]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[PatientFileWorkflows] (
        [PatientFileId] bigint NOT NULL,
        [ConsultationCompleted] bit NOT NULL CONSTRAINT DF_PatientFileWorkflows_ConsultationCompleted DEFAULT (0),
        [FinancialCaseId] uniqueidentifier NULL,
        [ApprovedAtUtc] datetime2 NULL,
        [ApprovedByUserId] uniqueidentifier NULL,
        [ApprovedFinanceHash] nvarchar(64) NULL,
        [TreatmentConfirmedAtUtc] datetime2 NULL,
        [TreatmentConfirmedByUserId] uniqueidentifier NULL,
        [FinanceConfirmedAtUtc] datetime2 NULL,
        [FinanceConfirmedByUserId] uniqueidentifier NULL,
        [ProfileConfirmedAtUtc] datetime2 NULL,
        [ProfileConfirmedByUserId] uniqueidentifier NULL,
        CONSTRAINT [PK_PatientFileWorkflows] PRIMARY KEY ([PatientFileId])
    );
END;

IF COL_LENGTH(N'dbo.PatientFileWorkflows', N'ConsultationCompleted') IS NULL
    ALTER TABLE [dbo].[PatientFileWorkflows] ADD [ConsultationCompleted] bit NOT NULL CONSTRAINT DF_PatientFileWorkflows_ConsultationCompleted DEFAULT (0);

IF COL_LENGTH(N'dbo.PatientFileWorkflows', N'FinancialCaseId') IS NULL
    ALTER TABLE [dbo].[PatientFileWorkflows] ADD [FinancialCaseId] uniqueidentifier NULL;

IF COL_LENGTH(N'dbo.PatientFileWorkflows', N'ApprovedAtUtc') IS NULL
    ALTER TABLE [dbo].[PatientFileWorkflows] ADD [ApprovedAtUtc] datetime2 NULL;

IF COL_LENGTH(N'dbo.PatientFileWorkflows', N'ApprovedByUserId') IS NULL
    ALTER TABLE [dbo].[PatientFileWorkflows] ADD [ApprovedByUserId] uniqueidentifier NULL;

IF COL_LENGTH(N'dbo.PatientFileWorkflows', N'ApprovedFinanceHash') IS NULL
    ALTER TABLE [dbo].[PatientFileWorkflows] ADD [ApprovedFinanceHash] nvarchar(64) NULL;

IF COL_LENGTH(N'dbo.PatientFileWorkflows', N'TreatmentConfirmedAtUtc') IS NULL
    ALTER TABLE [dbo].[PatientFileWorkflows] ADD [TreatmentConfirmedAtUtc] datetime2 NULL;

IF COL_LENGTH(N'dbo.PatientFileWorkflows', N'TreatmentConfirmedByUserId') IS NULL
    ALTER TABLE [dbo].[PatientFileWorkflows] ADD [TreatmentConfirmedByUserId] uniqueidentifier NULL;

IF COL_LENGTH(N'dbo.PatientFileWorkflows', N'FinanceConfirmedAtUtc') IS NULL
    ALTER TABLE [dbo].[PatientFileWorkflows] ADD [FinanceConfirmedAtUtc] datetime2 NULL;

IF COL_LENGTH(N'dbo.PatientFileWorkflows', N'FinanceConfirmedByUserId') IS NULL
    ALTER TABLE [dbo].[PatientFileWorkflows] ADD [FinanceConfirmedByUserId] uniqueidentifier NULL;

IF COL_LENGTH(N'dbo.PatientFileWorkflows', N'ProfileConfirmedAtUtc') IS NULL
    ALTER TABLE [dbo].[PatientFileWorkflows] ADD [ProfileConfirmedAtUtc] datetime2 NULL;

IF COL_LENGTH(N'dbo.PatientFileWorkflows', N'ProfileConfirmedByUserId') IS NULL
    ALTER TABLE [dbo].[PatientFileWorkflows] ADD [ProfileConfirmedByUserId] uniqueidentifier NULL;

IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE parent_object_id = OBJECT_ID(N'dbo.PatientFileWorkflows') AND type = 'PK')
    ALTER TABLE [dbo].[PatientFileWorkflows] ADD CONSTRAINT [PK_PatientFileWorkflows] PRIMARY KEY ([PatientFileId]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.PatientFileWorkflows') AND name = N'IX_PatientFileWorkflows_ApprovedByUserId')
    CREATE INDEX [IX_PatientFileWorkflows_ApprovedByUserId] ON [dbo].[PatientFileWorkflows] ([ApprovedByUserId]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.PatientFileWorkflows') AND name = N'IX_PatientFileWorkflows_FinancialCaseId')
    CREATE UNIQUE INDEX [IX_PatientFileWorkflows_FinancialCaseId] ON [dbo].[PatientFileWorkflows] ([FinancialCaseId]) WHERE [FinancialCaseId] IS NOT NULL;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.PatientFileWorkflows') AND name = N'IX_PatientFileWorkflows_TreatmentConfirmedByUserId')
    CREATE INDEX [IX_PatientFileWorkflows_TreatmentConfirmedByUserId] ON [dbo].[PatientFileWorkflows] ([TreatmentConfirmedByUserId]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.PatientFileWorkflows') AND name = N'IX_PatientFileWorkflows_FinanceConfirmedByUserId')
    CREATE INDEX [IX_PatientFileWorkflows_FinanceConfirmedByUserId] ON [dbo].[PatientFileWorkflows] ([FinanceConfirmedByUserId]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.PatientFileWorkflows') AND name = N'IX_PatientFileWorkflows_ProfileConfirmedByUserId')
    CREATE INDEX [IX_PatientFileWorkflows_ProfileConfirmedByUserId] ON [dbo].[PatientFileWorkflows] ([ProfileConfirmedByUserId]);

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE parent_object_id = OBJECT_ID(N'dbo.PatientFileWorkflows') AND name = N'FK_PatientFileWorkflows_PatientFiles_PatientFileId')
    ALTER TABLE [dbo].[PatientFileWorkflows] WITH CHECK ADD CONSTRAINT [FK_PatientFileWorkflows_PatientFiles_PatientFileId] FOREIGN KEY ([PatientFileId]) REFERENCES [dbo].[PatientFiles] ([Id]) ON DELETE NO ACTION;

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE parent_object_id = OBJECT_ID(N'dbo.PatientFileWorkflows') AND name = N'FK_PatientFileWorkflows_PatientFinancialCases_FinancialCaseId')
    ALTER TABLE [dbo].[PatientFileWorkflows] WITH CHECK ADD CONSTRAINT [FK_PatientFileWorkflows_PatientFinancialCases_FinancialCaseId] FOREIGN KEY ([FinancialCaseId]) REFERENCES [dbo].[PatientFinancialCases] ([Id]) ON DELETE NO ACTION;

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE parent_object_id = OBJECT_ID(N'dbo.PatientFileWorkflows') AND name = N'FK_PatientFileWorkflows_Users_ApprovedByUserId')
    ALTER TABLE [dbo].[PatientFileWorkflows] WITH CHECK ADD CONSTRAINT [FK_PatientFileWorkflows_Users_ApprovedByUserId] FOREIGN KEY ([ApprovedByUserId]) REFERENCES [dbo].[Users] ([Id]) ON DELETE NO ACTION;

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE parent_object_id = OBJECT_ID(N'dbo.PatientFileWorkflows') AND name = N'FK_PatientFileWorkflows_Users_TreatmentConfirmedByUserId')
    ALTER TABLE [dbo].[PatientFileWorkflows] WITH CHECK ADD CONSTRAINT [FK_PatientFileWorkflows_Users_TreatmentConfirmedByUserId] FOREIGN KEY ([TreatmentConfirmedByUserId]) REFERENCES [dbo].[Users] ([Id]) ON DELETE NO ACTION;

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE parent_object_id = OBJECT_ID(N'dbo.PatientFileWorkflows') AND name = N'FK_PatientFileWorkflows_Users_FinanceConfirmedByUserId')
    ALTER TABLE [dbo].[PatientFileWorkflows] WITH CHECK ADD CONSTRAINT [FK_PatientFileWorkflows_Users_FinanceConfirmedByUserId] FOREIGN KEY ([FinanceConfirmedByUserId]) REFERENCES [dbo].[Users] ([Id]) ON DELETE NO ACTION;

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE parent_object_id = OBJECT_ID(N'dbo.PatientFileWorkflows') AND name = N'FK_PatientFileWorkflows_Users_ProfileConfirmedByUserId')
    ALTER TABLE [dbo].[PatientFileWorkflows] WITH CHECK ADD CONSTRAINT [FK_PatientFileWorkflows_Users_ProfileConfirmedByUserId] FOREIGN KEY ([ProfileConfirmedByUserId]) REFERENCES [dbo].[Users] ([Id]) ON DELETE NO ACTION;
");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Deliberately no destructive rollback: this migration repairs potentially existing data.
    }
}
