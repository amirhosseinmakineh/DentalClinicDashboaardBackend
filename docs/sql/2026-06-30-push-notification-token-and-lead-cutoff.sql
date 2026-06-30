IF COL_LENGTH('Users', 'PushNotificationToken') IS NULL
    ALTER TABLE Users ADD PushNotificationToken nvarchar(1000) NULL;
GO

-- Optional operational cleanup for the new policy:
-- keep only leads imported from the activation moment forward.
-- Set @ActivationTime to the exact time the policy starts before running.
DECLARE @ActivationTime datetime2 = '2026-06-30T00:00:00';

UPDATE LeadAssignments
SET IsDeleted = 1
WHERE CreatedAt < @ActivationTime
  AND IsDeleted = 0;
GO
