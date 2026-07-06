IF COL_LENGTH('Users', 'PushNotificationToken') IS NOT NULL
BEGIN
    ALTER TABLE Users ALTER COLUMN PushNotificationToken nvarchar(max) NULL;
END
