-- Lead broadcast (Snapp-style) schema changes
-- Run against production DB before deploying backend with lead broadcast

IF COL_LENGTH('LeadAssignments', 'BroadcastStartedAt') IS NULL
BEGIN
    ALTER TABLE LeadAssignments ADD BroadcastStartedAt datetime2 NULL;
END

IF COL_LENGTH('LeadAssignments', 'ClaimedAt') IS NULL
BEGIN
    ALTER TABLE LeadAssignments ADD ClaimedAt datetime2 NULL;
END

IF COL_LENGTH('LeadAssignments', 'BroadcastExpiresAt') IS NULL
BEGIN
    ALTER TABLE LeadAssignments ADD BroadcastExpiresAt datetime2 NULL;
END

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_LeadAssignments_BroadcastExpiresAt'
      AND object_id = OBJECT_ID('LeadAssignments')
)
BEGIN
    CREATE INDEX IX_LeadAssignments_BroadcastExpiresAt
        ON LeadAssignments (BroadcastExpiresAt);
END

IF OBJECT_ID('LeadBroadcastDismissals', 'U') IS NULL
BEGIN
    CREATE TABLE LeadBroadcastDismissals (
        Id bigint NOT NULL IDENTITY(1,1),
        LeadAssignmentId bigint NOT NULL,
        ConsultantProfileId bigint NOT NULL,
        CreatedAt datetime2 NOT NULL,
        UpdatedAt datetime2 NULL,
        IsDeleted bit NOT NULL CONSTRAINT DF_LeadBroadcastDismissals_IsDeleted DEFAULT 0,
        DeletedAt datetime2 NULL,
        CONSTRAINT PK_LeadBroadcastDismissals PRIMARY KEY (Id),
        CONSTRAINT FK_LeadBroadcastDismissals_LeadAssignments_LeadAssignmentId
            FOREIGN KEY (LeadAssignmentId) REFERENCES LeadAssignments (Id) ON DELETE CASCADE,
        CONSTRAINT FK_LeadBroadcastDismissals_ConsultantProfiles_ConsultantProfileId
            FOREIGN KEY (ConsultantProfileId) REFERENCES ConsultantProfiles (Id) ON DELETE CASCADE
    );

    CREATE UNIQUE INDEX IX_LeadBroadcastDismissals_LeadAssignmentId_ConsultantProfileId
        ON LeadBroadcastDismissals (LeadAssignmentId, ConsultantProfileId);
END
