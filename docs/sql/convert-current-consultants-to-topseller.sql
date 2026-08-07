/*
  Manual operation only. TopSeller (Gold) is ConsultantLevel = 3.
  Authorization remains the existing Consultant role; UserRoles are used only to
  restrict the target set and are not modified.
*/
SET XACT_ABORT ON;
BEGIN TRANSACTION;

-- Preview: only active, non-deleted consultant accounts that would change.
SELECT cp.Id AS ConsultantProfileId,
       cp.UserId,
       cp.ConsultantLevel AS CurrentConsultantLevel,
       u.IsActive,
       cp.IsDeleted
FROM ConsultantProfiles cp
INNER JOIN Users u ON u.Id = cp.UserId
WHERE cp.IsDeleted = 0
  AND u.IsDeleted = 0
  AND u.IsActive = 1
  AND cp.ConsultantLevel <> 3
  AND EXISTS (
      SELECT 1
      FROM UserRoles ur
      INNER JOIN Roles r ON r.Id = ur.RoleId
      WHERE ur.UserId = u.Id
        AND ur.IsDeleted = 0
        AND r.IsDeleted = 0
        AND r.RoleName = 'Consultant');

UPDATE cp
SET cp.ConsultantLevel = 3,
    cp.TopSellerStartedAt = DATEADD(MINUTE, -210,
        CAST(DATEADD(MINUTE, 210, GETUTCDATE()) AS date)),
    cp.TopSellerLastEvaluatedPeriodStart = NULL,
    cp.TopSellerLastEvaluatedAt = NULL,
    cp.TopSellerRewardLevel = 0,
    cp.UpdatedAt = GETUTCDATE()
FROM ConsultantProfiles cp
INNER JOIN Users u ON u.Id = cp.UserId
WHERE cp.IsDeleted = 0
  AND u.IsDeleted = 0
  AND u.IsActive = 1
  AND cp.ConsultantLevel <> 3
  AND EXISTS (
      SELECT 1
      FROM UserRoles ur
      INNER JOIN Roles r ON r.Id = ur.RoleId
      WHERE ur.UserId = u.Id
        AND ur.IsDeleted = 0
        AND r.IsDeleted = 0
        AND r.RoleName = 'Consultant');

-- Validation before choosing COMMIT or ROLLBACK.
SELECT cp.ConsultantLevel, COUNT(*) AS ConsultantCount
FROM ConsultantProfiles cp
INNER JOIN Users u ON u.Id = cp.UserId
WHERE cp.IsDeleted = 0 AND u.IsDeleted = 0 AND u.IsActive = 1
GROUP BY cp.ConsultantLevel;

-- Review the result, then execute exactly one of the following manually:
-- COMMIT TRANSACTION;
-- ROLLBACK TRANSACTION;
