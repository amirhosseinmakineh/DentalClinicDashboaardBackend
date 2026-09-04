using DentalDashboard.Infrastracture.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
namespace DentalDashboard.Infrastracture.Migrations;

[DbContext(typeof(DentalContext))]
[Migration("20260830180000_AddPatientFinance")]
public partial class AddPatientFinance : Migration
{
 protected override void Up(MigrationBuilder m)
 {
  m.Sql("""
CREATE TABLE [PatientFinancialCases] ([Id] uniqueidentifier NOT NULL CONSTRAINT [PK_PatientFinancialCases] PRIMARY KEY,[PatientId] uniqueidentifier NOT NULL,[Service] int NOT NULL,[TotalAmount] decimal(18,2) NOT NULL,[AgreementType] int NOT NULL,[Status] int NOT NULL,[CreatedByUserId] uniqueidentifier NOT NULL,[CreatedAt] datetime2 NOT NULL,[UpdatedAt] datetime2 NULL,[IsDeleted] bit NOT NULL,[DeletedAt] datetime2 NULL,CONSTRAINT [FK_PatientFinancialCases_Users_PatientId] FOREIGN KEY ([PatientId]) REFERENCES [Users]([Id]),CONSTRAINT [FK_PatientFinancialCases_Users_CreatedByUserId] FOREIGN KEY ([CreatedByUserId]) REFERENCES [Users]([Id]));
CREATE INDEX [IX_PatientFinancialCases_PatientId] ON [PatientFinancialCases]([PatientId]);
CREATE TABLE [PatientCheques] ([Id] bigint IDENTITY NOT NULL CONSTRAINT [PK_PatientCheques] PRIMARY KEY,[PatientFinancialCaseId] uniqueidentifier NOT NULL,[Amount] decimal(18,2) NOT NULL,[SayadNumber] nvarchar(32) NOT NULL,[OwnerName] nvarchar(200) NOT NULL,[DueDate] datetime2 NOT NULL,[Status] int NOT NULL,[CreatedAt] datetime2 NOT NULL,[UpdatedAt] datetime2 NULL,[IsDeleted] bit NOT NULL,[DeletedAt] datetime2 NULL,CONSTRAINT [FK_PatientCheques_PatientFinancialCases_PatientFinancialCaseId] FOREIGN KEY ([PatientFinancialCaseId]) REFERENCES [PatientFinancialCases]([Id]));
CREATE INDEX [IX_PatientCheques_PatientFinancialCaseId_DueDate_Status] ON [PatientCheques]([PatientFinancialCaseId],[DueDate],[Status]);
CREATE TABLE [PatientPromissoryNotes] ([Id] bigint IDENTITY NOT NULL CONSTRAINT [PK_PatientPromissoryNotes] PRIMARY KEY,[PatientFinancialCaseId] uniqueidentifier NOT NULL,[SerialNumber] nvarchar(64) NOT NULL,[Amount] decimal(18,2) NOT NULL,[DueDate] datetime2 NOT NULL,[Status] int NOT NULL,[CreatedAt] datetime2 NOT NULL,[UpdatedAt] datetime2 NULL,[IsDeleted] bit NOT NULL,[DeletedAt] datetime2 NULL,CONSTRAINT [FK_PatientPromissoryNotes_PatientFinancialCases_PatientFinancialCaseId] FOREIGN KEY ([PatientFinancialCaseId]) REFERENCES [PatientFinancialCases]([Id]));
CREATE INDEX [IX_PatientPromissoryNotes_PatientFinancialCaseId_DueDate_Status] ON [PatientPromissoryNotes]([PatientFinancialCaseId],[DueDate],[Status]);
CREATE TABLE [PatientDebts] ([Id] bigint IDENTITY NOT NULL CONSTRAINT [PK_PatientDebts] PRIMARY KEY,[PatientFinancialCaseId] uniqueidentifier NOT NULL,[Amount] decimal(18,2) NOT NULL,[SourceType] int NOT NULL,[SourceId] bigint NOT NULL,[Status] int NOT NULL,[DueDate] datetime2 NOT NULL,[CreatedAt] datetime2 NOT NULL,[UpdatedAt] datetime2 NULL,[IsDeleted] bit NOT NULL,[DeletedAt] datetime2 NULL,CONSTRAINT [FK_PatientDebts_PatientFinancialCases_PatientFinancialCaseId] FOREIGN KEY ([PatientFinancialCaseId]) REFERENCES [PatientFinancialCases]([Id]));
CREATE UNIQUE INDEX [IX_PatientDebts_SourceType_SourceId] ON [PatientDebts]([SourceType],[SourceId]); CREATE INDEX [IX_PatientDebts_PatientFinancialCaseId_Status_DueDate] ON [PatientDebts]([PatientFinancialCaseId],[Status],[DueDate]);
CREATE TABLE [PatientFinancialTransactions] ([Id] bigint IDENTITY NOT NULL CONSTRAINT [PK_PatientFinancialTransactions] PRIMARY KEY,[PatientFinancialCaseId] uniqueidentifier NOT NULL,[Amount] decimal(18,2) NOT NULL,[Type] int NOT NULL,[SourceType] int NOT NULL,[SourceId] bigint NOT NULL,[CreatedByUserId] uniqueidentifier NOT NULL,[CreatedAt] datetime2 NOT NULL,[UpdatedAt] datetime2 NULL,[IsDeleted] bit NOT NULL,[DeletedAt] datetime2 NULL,CONSTRAINT [FK_PatientFinancialTransactions_PatientFinancialCases_PatientFinancialCaseId] FOREIGN KEY ([PatientFinancialCaseId]) REFERENCES [PatientFinancialCases]([Id]),CONSTRAINT [FK_PatientFinancialTransactions_Users_CreatedByUserId] FOREIGN KEY ([CreatedByUserId]) REFERENCES [Users]([Id]));
CREATE INDEX [IX_PatientFinancialTransactions_PatientFinancialCaseId] ON [PatientFinancialTransactions]([PatientFinancialCaseId]); CREATE UNIQUE INDEX [IX_PatientFinancialTransactions_SourceType_SourceId_Type] ON [PatientFinancialTransactions]([SourceType],[SourceId],[Type]);
""");
 }
 protected override void Down(MigrationBuilder m)=>m.Sql("DROP TABLE [PatientFinancialTransactions]; DROP TABLE [PatientDebts]; DROP TABLE [PatientPromissoryNotes]; DROP TABLE [PatientCheques]; DROP TABLE [PatientFinancialCases];");
}
