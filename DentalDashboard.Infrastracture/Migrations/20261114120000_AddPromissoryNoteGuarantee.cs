using DentalDashboard.Infrastracture.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace DentalDashboard.Infrastracture.Migrations;

[DbContext(typeof(DentalContext))]
[Migration("20261114120000_AddPromissoryNoteGuarantee")]
public sealed class AddPromissoryNoteGuarantee : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
        IF COL_LENGTH(N'dbo.PatientPromissoryNotes', N'IsGuarantee') IS NULL
            ALTER TABLE [dbo].[PatientPromissoryNotes] ADD [IsGuarantee] bit NOT NULL
                CONSTRAINT [DF_PatientPromissoryNotes_IsGuarantee] DEFAULT (0);
        """);

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropColumn("IsGuarantee", "PatientPromissoryNotes");
}
