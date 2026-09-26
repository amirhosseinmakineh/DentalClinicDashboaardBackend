using System;
using DentalDashboard.Infrastracture.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DentalDashboard.Infrastracture.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(DentalContext))]
    [Migration("20260709103000_FixPushSubscriptionForeignKey")]
    public partial class FixPushSubscriptionForeignKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // This migration existed without MigrationAttribute in earlier deployments, so its
            // changes may already have been applied manually even though it is absent from
            // __EFMigrationsHistory. Check every object to support both database states.
            migrationBuilder.Sql(
                """
                IF EXISTS (
                    SELECT 1
                    FROM sys.foreign_keys
                    WHERE name = N'FK_PushSubscriptions_Users_UserId1'
                      AND parent_object_id = OBJECT_ID(N'[dbo].[PushSubscriptions]'))
                BEGIN
                    ALTER TABLE [dbo].[PushSubscriptions]
                        DROP CONSTRAINT [FK_PushSubscriptions_Users_UserId1];
                END;

                IF EXISTS (
                    SELECT 1
                    FROM sys.indexes
                    WHERE name = N'IX_PushSubscriptions_UserId1'
                      AND object_id = OBJECT_ID(N'[dbo].[PushSubscriptions]'))
                BEGIN
                    DROP INDEX [IX_PushSubscriptions_UserId1]
                        ON [dbo].[PushSubscriptions];
                END;

                IF COL_LENGTH(N'dbo.PushSubscriptions', N'UserId1') IS NOT NULL
                BEGIN
                    ALTER TABLE [dbo].[PushSubscriptions] DROP COLUMN [UserId1];
                END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "UserId1",
                table: "PushSubscriptions",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_PushSubscriptions_UserId1",
                table: "PushSubscriptions",
                column: "UserId1");

            migrationBuilder.AddForeignKey(
                name: "FK_PushSubscriptions_Users_UserId1",
                table: "PushSubscriptions",
                column: "UserId1",
                principalTable: "Users",
                principalColumn: "Id");
        }
    }
}
