using DentalDashboard.Infrastracture.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DentalDashboard.Infrastracture.Migrations;

/// <summary>
/// Repairs databases on which the first GUID finance migration was already
/// recorded while PatientId was still the bigint PatientProfiles key.
/// </summary>
[DbContext(typeof(DentalContext))]
[Migration("20260830201000_ConvertPatientFinancePatientIdToGuid")]
public partial class ConvertPatientFinancePatientIdToGuid : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Keep this in a separate migration from ConvertPatientFinanceKeysToGuid:
        // that migration may already be present in __EFMigrationsHistory after it
        // did nothing on the partially converted (Guid Id, bigint PatientId) shape.
        migrationBuilder.Sql("""
IF EXISTS (
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[dbo].[PatientFinancialCases]')
      AND name = N'Id'
      AND system_type_id = TYPE_ID(N'uniqueidentifier'))
AND EXISTS (
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[dbo].[PatientFinancialCases]')
      AND name = N'PatientId'
      AND system_type_id = TYPE_ID(N'bigint'))
BEGIN
    EXEC sp_executesql N'
    ALTER TABLE [dbo].[PatientFinancialCases]
        DROP CONSTRAINT [FK_PatientFinancialCases_PatientProfiles_PatientId];
    DROP INDEX [IX_PatientFinancialCases_PatientId]
        ON [dbo].[PatientFinancialCases];

    ALTER TABLE [dbo].[PatientFinancialCases]
        ADD [PatientUserId] uniqueidentifier NULL;

    UPDATE c
    SET [PatientUserId] = p.[UserId]
    FROM [dbo].[PatientFinancialCases] c
    INNER JOIN [dbo].[PatientProfiles] p ON p.[Id] = c.[PatientId];

    IF EXISTS (
        SELECT 1 FROM [dbo].[PatientFinancialCases]
        WHERE [PatientUserId] IS NULL)
        THROW 51000, ''A financial case references a missing patient profile; PatientId cannot be converted.'', 1;

    ALTER TABLE [dbo].[PatientFinancialCases] DROP COLUMN [PatientId];
    EXEC sp_rename
        N''dbo.PatientFinancialCases.PatientUserId'', N''PatientId'', N''COLUMN'';
    ALTER TABLE [dbo].[PatientFinancialCases]
        ALTER COLUMN [PatientId] uniqueidentifier NOT NULL;

    ALTER TABLE [dbo].[PatientFinancialCases]
        ADD CONSTRAINT [FK_PatientFinancialCases_Users_PatientId]
        FOREIGN KEY ([PatientId]) REFERENCES [dbo].[Users] ([Id]);
    CREATE INDEX [IX_PatientFinancialCases_PatientId]
        ON [dbo].[PatientFinancialCases] ([PatientId]);
    ';
END

IF COL_LENGTH(N'[dbo].[PatientFinancialCases]', N'CreatedBySecretaryUserId') IS NOT NULL
   AND COL_LENGTH(N'[dbo].[PatientFinancialCases]', N'CreatedByUserId') IS NULL
BEGIN
    EXEC sp_rename N'dbo.PatientFinancialCases.CreatedBySecretaryUserId',
                   N'CreatedByUserId', N'COLUMN';
END

IF COL_LENGTH(N'[dbo].[PatientFinancialTransactions]', N'CreatedBySecretaryUserId') IS NOT NULL
   AND COL_LENGTH(N'[dbo].[PatientFinancialTransactions]', N'CreatedByUserId') IS NULL
BEGIN
    EXEC sp_rename N'dbo.PatientFinancialTransactions.CreatedBySecretaryUserId',
                   N'CreatedByUserId', N'COLUMN';
END
""");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Mapping a user GUID back to one of potentially several profile rows
        // is not guaranteed to be lossless.
    }
}
