-- Consultant average score model (default 100, no negative values)
-- Each ScoreLogs.ScoreValue is an event rating between 0 and 100.
-- ConsultantProfiles.CurrentScore = AVG(active ScoreLogs) or 100 when no logs exist.

UPDATE ConsultantProfiles
SET CurrentScore = 100
WHERE CurrentScore < 0 OR CurrentScore = 0;

GO

UPDATE ScoreLogs
SET ScoreValue = CASE Reason
    WHEN 1 THEN 95   -- SuccessfulCall
    WHEN 2 THEN 55   -- FailedCall
    WHEN 8 THEN 40   -- LateCall
    WHEN 9 THEN 65   -- NoAnswer
    WHEN 10 THEN 95  -- ReservationAttendanceConfirmed
    WHEN 11 THEN 55  -- ReservationAttendanceRejected
    WHEN 5 THEN CASE WHEN ScoreValue < 0 THEN 30 ELSE ScoreValue END -- ManagerReward
    WHEN 6 THEN CASE WHEN ScoreValue < 0 THEN ABS(ScoreValue) ELSE ScoreValue END -- ManagerPenalty legacy
    ELSE CASE WHEN ScoreValue < 0 THEN 50 WHEN ScoreValue > 100 THEN 100 ELSE ScoreValue END
END
WHERE ScoreValue < 0 OR ScoreValue > 100 OR Reason IN (1, 2, 8, 9, 10, 11);

GO

UPDATE cp
SET cp.CurrentScore = COALESCE(agg.AverageScore, 100)
FROM ConsultantProfiles cp
LEFT JOIN (
    SELECT
        sl.ConsultantProfileId,
        CAST(ROUND(AVG(CAST(sl.ScoreValue AS float)), 0) AS int) AS AverageScore
    FROM ScoreLogs sl
    WHERE sl.IsDeleted = 0
    GROUP BY sl.ConsultantProfileId
) agg ON agg.ConsultantProfileId = cp.Id;

GO

-- Query: consultant average scores with event count
SELECT
    cp.Id AS ConsultantProfileId,
    u.FirstName + N' ' + u.LastName AS ConsultantName,
    COUNT(sl.Id) AS EventCount,
    COALESCE(CAST(ROUND(AVG(CAST(sl.ScoreValue AS float)), 0) AS int), 100) AS AverageScore,
    cp.CurrentScore AS StoredCurrentScore
FROM ConsultantProfiles cp
INNER JOIN Users u ON u.Id = cp.UserId
LEFT JOIN ScoreLogs sl ON sl.ConsultantProfileId = cp.Id AND sl.IsDeleted = 0
WHERE cp.IsDeleted = 0
GROUP BY cp.Id, u.FirstName, u.LastName, cp.CurrentScore
ORDER BY AverageScore DESC, cp.Id;

GO
