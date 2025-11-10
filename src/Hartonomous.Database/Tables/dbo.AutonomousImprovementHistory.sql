-- =============================================
-- AutonomousImprovementHistory: Provenance tracking for self-modifications
-- =============================================

CREATE TABLE [dbo].[AutonomousImprovementHistory]
(
    [ImprovementId] UNIQUEIDENTIFIER NOT NULL DEFAULT (NEWID()),
    [AnalysisResults] NVARCHAR(MAX) NOT NULL,
    [GeneratedCode] NVARCHAR(MAX) NOT NULL,
    [TargetFile] NVARCHAR(512) NOT NULL,
    [ChangeType] NVARCHAR(50) NOT NULL,
    [RiskLevel] NVARCHAR(20) NOT NULL,
    [EstimatedImpact] NVARCHAR(20) NULL,
    [GitCommitHash] NVARCHAR(64) NULL,
    [SuccessScore] DECIMAL(5,4) NULL,
    [TestsPassed] INT NULL,
    [TestsFailed] INT NULL,
    [PerformanceDelta] DECIMAL(10,4) NULL,
    [ErrorMessage] NVARCHAR(MAX) NULL,
    [WasDeployed] BIT NOT NULL DEFAULT (0),
    [WasRolledBack] BIT NOT NULL DEFAULT (0),
    [StartedAt] DATETIME2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
    [CompletedAt] DATETIME2(7) NULL,
    [RolledBackAt] DATETIME2(7) NULL,
    
    CONSTRAINT [PK_AutonomousImprovementHistory] PRIMARY KEY CLUSTERED ([ImprovementId]),
    CONSTRAINT [CK_AutonomousImprovement_SuccessScore]
        CHECK ([SuccessScore] >= 0 AND [SuccessScore] <= 1)
);
GO

CREATE NONCLUSTERED INDEX [IX_AutonomousImprovement_StartedAt]
    ON [dbo].[AutonomousImprovementHistory]([StartedAt] DESC)
    INCLUDE ([ChangeType], [SuccessScore], [WasDeployed]);
GO

CREATE NONCLUSTERED INDEX [IX_AutonomousImprovement_SuccessScore]
    ON [dbo].[AutonomousImprovementHistory]([SuccessScore] DESC)
    WHERE [WasDeployed] = 1 AND [WasRolledBack] = 0;
GO

CREATE NONCLUSTERED INDEX [IX_AutonomousImprovement_Failures]
    ON [dbo].[AutonomousImprovementHistory]([ChangeType], [RiskLevel])
    INCLUDE ([ErrorMessage], [SuccessScore])
    WHERE [WasDeployed] = 1;
GO