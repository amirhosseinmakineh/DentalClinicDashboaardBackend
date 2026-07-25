using DentalDashboard.Infrastracture.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DentalDashboard.Infrastracture.Migrations;

/// <summary>
/// Repairs production databases whose migration history and PushSubscriptions
/// schema drifted apart, and clears invalid zero lead limits created by older APIs.
/// </summary>
[DbContext(typeof(DentalContext))]
[Migration("20260725120000_RepairPushSubscriptionsAndLeadLimits")]
public sealed class RepairPushSubscriptionsAndLeadLimits : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            IF OBJECT_ID(N'[dbo].[PushSubscriptions]', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[PushSubscriptions] (
                    [Id] bigint IDENTITY(1,1) NOT NULL,
                    [UserId] uniqueidentifier NOT NULL,
                    [Endpoint] nvarchar(2000) NOT NULL,
                    [P256dh] nvarchar(512) NOT NULL,
                    [Auth] nvarchar(256) NOT NULL,
                    [CreatedAt] datetime2 NOT NULL,
                    [UpdatedAt] datetime2 NULL,
                    [IsDeleted] bit NOT NULL CONSTRAINT [DF_PushSubscriptions_IsDeleted] DEFAULT 0,
                    [DeletedAt] datetime2 NULL,
                    CONSTRAINT [PK_PushSubscriptions] PRIMARY KEY ([Id]),
                    CONSTRAINT [FK_PushSubscriptions_Users_UserId]
                        FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([Id])
                );

                CREATE INDEX [IX_PushSubscriptions_UserId]
                    ON [dbo].[PushSubscriptions] ([UserId]);
            END;

            IF COL_LENGTH(N'dbo.PushSubscriptions', N'UserId1') IS NOT NULL
            BEGIN
                IF EXISTS (
                    SELECT 1 FROM sys.foreign_keys
                    WHERE [name] = N'FK_PushSubscriptions_Users_UserId1'
                      AND [parent_object_id] = OBJECT_ID(N'[dbo].[PushSubscriptions]'))
                    ALTER TABLE [dbo].[PushSubscriptions]
                        DROP CONSTRAINT [FK_PushSubscriptions_Users_UserId1];

                IF EXISTS (
                    SELECT 1 FROM sys.indexes
                    WHERE [name] = N'IX_PushSubscriptions_UserId1'
                      AND [object_id] = OBJECT_ID(N'[dbo].[PushSubscriptions]'))
                    DROP INDEX [IX_PushSubscriptions_UserId1]
                        ON [dbo].[PushSubscriptions];

                ALTER TABLE [dbo].[PushSubscriptions] DROP COLUMN [UserId1];
            END;

            IF OBJECT_ID(N'[dbo].[PushSubscriptions]', N'U') IS NOT NULL
            BEGIN
                ALTER TABLE [dbo].[PushSubscriptions]
                    ALTER COLUMN [Endpoint] nvarchar(2000) NOT NULL;
                ALTER TABLE [dbo].[PushSubscriptions]
                    ALTER COLUMN [P256dh] nvarchar(512) NOT NULL;
                ALTER TABLE [dbo].[PushSubscriptions]
                    ALTER COLUMN [Auth] nvarchar(256) NOT NULL;
            END;

            UPDATE [dbo].[ConsultantProfiles]
            SET [LimitNumber] = NULL
            WHERE [LimitNumber] = 0;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // This migration repairs invalid production state. Reintroducing the
        // required shadow FK or zero limits would make inserts/pickups fail again.
    }
}
