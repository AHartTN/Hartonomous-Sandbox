-- =============================================
-- Table: dbo.CICDBuilds
-- Description: Tracks CI/CD build history for autonomous improvement system
--              Used by sp_AutonomousImprovement for deployment tracking
-- =============================================
CREATE TABLE [dbo].[CICDBuild]
(
    [BuildId]           BIGINT              NOT NULL IDENTITY(1,1),
    [CommitHash]        NVARCHAR(40)        NOT NULL,
    [BranchName]        NVARCHAR(255)       NOT NULL,
    [BuildNumber]       NVARCHAR(50)        NULL,
    [Status]            NVARCHAR(50)        NOT NULL, -- 'Queued', 'InProgress', 'Success', 'Failed', 'Cancelled'
    [StartedAt]         DATETIME2(7)        NULL,
    [CompletedAt]       DATETIME2(7)        NULL,
    [DurationMs]        INT                 NULL,
    [BuildAgent]        NVARCHAR(255)       NULL,
    [TriggerType]       NVARCHAR(50)        NULL, -- 'Manual', 'Commit', 'PullRequest', 'Schedule', 'Autonomous'
    [BuildLogs]         NVARCHAR(MAX)       NULL,
    [ArtifactUrl]       NVARCHAR(500)       NULL,
    [TestsPassed]       INT                 NULL,
    [TestsFailed]       INT                 NULL,
    [CodeCoverage]      DECIMAL(5,2)        NULL,
    [DeployedAt]        DATETIME2(7)        NULL,
    [DeploymentStatus]  NVARCHAR(50)        NULL, -- 'NotDeployed', 'Deployed', 'DeploymentFailed', 'RolledBack'
    [CreatedAt]         DATETIME2(7)        NOT NULL CONSTRAINT DF_CICDBuilds_CreatedAt DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT [PK_CICDBuilds] PRIMARY KEY CLUSTERED ([BuildId] ASC),
    CONSTRAINT [CK_CICDBuilds_Status] CHECK ([Status] IN ('Queued', 'InProgress', 'Success', 'Failed', 'Cancelled')),
    CONSTRAINT [CK_CICDBuilds_DeploymentStatus] CHECK ([DeploymentStatus] IS NULL OR [DeploymentStatus] IN ('NotDeployed', 'Deployed', 'DeploymentFailed', 'RolledBack')),
    INDEX [IX_CICDBuilds_CommitHash] NONCLUSTERED ([CommitHash]),
    INDEX [IX_CICDBuilds_Status_StartedAt] NONCLUSTERED ([Status], [StartedAt] DESC),
    INDEX [IX_CICDBuilds_CreatedAt] NONCLUSTERED ([CreatedAt] DESC)
);
GO
