/* Fix PushSubscriptions table for production RegisterPushToken 500 errors. */

IF OBJECT_ID(N'PushSubscriptions', N'U') IS NULL
BEGIN
    CREATE TABLE PushSubscriptions (
        Id bigint IDENTITY(1,1) NOT NULL PRIMARY KEY,
        UserId uniqueidentifier NOT NULL,
        Endpoint nvarchar(2000) NOT NULL,
        P256dh nvarchar(512) NOT NULL,
        Auth nvarchar(256) NOT NULL,
        CreatedAt datetime2 NOT NULL,
        UpdatedAt datetime2 NULL,
        IsDeleted bit NOT NULL DEFAULT 0,
        DeletedAt datetime2 NULL,
        CONSTRAINT FK_PushSubscriptions_Users_UserId
            FOREIGN KEY (UserId) REFERENCES Users(Id)
    );
    CREATE INDEX IX_PushSubscriptions_UserId ON PushSubscriptions(UserId);
END

IF COL_LENGTH('PushSubscriptions', 'UserId1') IS NOT NULL
BEGIN
    IF EXISTS (
        SELECT 1 FROM sys.foreign_keys
        WHERE name = 'FK_PushSubscriptions_Users_UserId1'
    )
        ALTER TABLE PushSubscriptions DROP CONSTRAINT FK_PushSubscriptions_Users_UserId1;

    IF EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE name = 'IX_PushSubscriptions_UserId1'
          AND object_id = OBJECT_ID('PushSubscriptions')
    )
        DROP INDEX IX_PushSubscriptions_UserId1 ON PushSubscriptions;

    ALTER TABLE PushSubscriptions DROP COLUMN UserId1;
END

IF COL_LENGTH('PushSubscriptions', 'Endpoint') IS NOT NULL
    ALTER TABLE PushSubscriptions ALTER COLUMN Endpoint nvarchar(2000) NOT NULL;

IF COL_LENGTH('PushSubscriptions', 'P256dh') IS NOT NULL
    ALTER TABLE PushSubscriptions ALTER COLUMN P256dh nvarchar(512) NOT NULL;

IF COL_LENGTH('PushSubscriptions', 'Auth') IS NOT NULL
    ALTER TABLE PushSubscriptions ALTER COLUMN Auth nvarchar(256) NOT NULL;

IF NOT EXISTS (
    SELECT 1 FROM __EFMigrationsHistory
    WHERE MigrationId = '20260709075918_AddPushNotificationEntity'
)
   AND OBJECT_ID(N'PushSubscriptions', N'U') IS NOT NULL
    INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES ('20260709075918_AddPushNotificationEntity', '10.0.8');

IF NOT EXISTS (
    SELECT 1 FROM __EFMigrationsHistory
    WHERE MigrationId = '20260709103000_FixPushSubscriptionForeignKey'
)
   AND COL_LENGTH('PushSubscriptions', 'UserId1') IS NULL
    INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES ('20260709103000_FixPushSubscriptionForeignKey', '10.0.8');

IF NOT EXISTS (
    SELECT 1 FROM __EFMigrationsHistory
    WHERE MigrationId = '20260710120000_ExpandPushSubscriptionEndpoint'
)
   AND COL_LENGTH('PushSubscriptions', 'Endpoint') IS NOT NULL
    INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES ('20260710120000_ExpandPushSubscriptionEndpoint', '10.0.8');
