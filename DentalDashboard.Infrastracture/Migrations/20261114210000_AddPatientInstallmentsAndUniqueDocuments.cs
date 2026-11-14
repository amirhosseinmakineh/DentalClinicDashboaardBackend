using DentalDashboard.Infrastracture.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
namespace DentalDashboard.Infrastracture.Migrations;

[DbContext(typeof(DentalContext))]
[Migration("20261114210000_AddPatientInstallmentsAndUniqueDocuments")]
public partial class AddPatientInstallmentsAndUniqueDocuments : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
IF OBJECT_ID(N'dbo.PatientInstallments', N'U') IS NULL
BEGIN
 CREATE TABLE dbo.PatientInstallments (
  Id bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_PatientInstallments PRIMARY KEY,
  PatientFinancialCaseId uniqueidentifier NOT NULL,
  Amount decimal(18,2) NOT NULL,
  DueDate datetime2 NOT NULL,
  PaidAtUtc datetime2 NULL,
  PaidByUserId uniqueidentifier NULL,
  IsDeleted bit NOT NULL CONSTRAINT DF_PatientInstallments_IsDeleted DEFAULT(0),
  CONSTRAINT FK_PatientInstallments_PatientFinancialCases FOREIGN KEY (PatientFinancialCaseId)
   REFERENCES dbo.PatientFinancialCases(Id)
 );
END;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.PatientInstallments') AND name = N'IX_PatientInstallments_PatientFinancialCaseId_DueDate')
 CREATE INDEX IX_PatientInstallments_PatientFinancialCaseId_DueDate ON dbo.PatientInstallments(PatientFinancialCaseId,DueDate);
IF EXISTS (SELECT 1 FROM dbo.PatientCheques WHERE IsDeleted = 0 GROUP BY LTRIM(RTRIM(SayadNumber)) HAVING COUNT(*) > 1)
 THROW 51001, 'Duplicate cheque numbers must be resolved before creating unique index.', 1;
IF EXISTS (SELECT 1 FROM dbo.PatientPromissoryNotes WHERE IsDeleted = 0 GROUP BY LTRIM(RTRIM(SerialNumber)) HAVING COUNT(*) > 1)
 THROW 51002, 'Duplicate promissory note numbers must be resolved before creating unique index.', 1;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.PatientCheques') AND name = N'UX_PatientCheques_SayadNumber_Active')
 CREATE UNIQUE INDEX UX_PatientCheques_SayadNumber_Active ON dbo.PatientCheques(SayadNumber) WHERE IsDeleted = 0;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.PatientPromissoryNotes') AND name = N'UX_PatientPromissoryNotes_SerialNumber_Active')
 CREATE UNIQUE INDEX UX_PatientPromissoryNotes_SerialNumber_Active ON dbo.PatientPromissoryNotes(SerialNumber) WHERE IsDeleted = 0;
");
    }
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
DROP INDEX IF EXISTS UX_PatientCheques_SayadNumber_Active ON dbo.PatientCheques;
DROP INDEX IF EXISTS UX_PatientPromissoryNotes_SerialNumber_Active ON dbo.PatientPromissoryNotes;
DROP TABLE IF EXISTS dbo.PatientInstallments;
");
    }
}
