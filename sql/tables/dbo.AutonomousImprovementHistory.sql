-- =============================================
-- AutonomousImprovementHistory: Provenance tracking for self-modifications
-- =============================================

USE Hartonomous;
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'AutonomousImprovementHistory' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.AutonomousImprovementHistory
    (
        ImprovementId UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
        AnalysisResults NVARCHAR(MAX) NOT NULL,
        GeneratedCode NVARCHAR(MAX) NOT NULL,
        TargetFile NVARCHAR(512) NOT NULL,
        ChangeType NVARCHAR(50) NOT NULL,        -- optimization, bugfix, feature
        RiskLevel NVARCHAR(20) NOT NULL,         -- low, medium, high
        EstimatedImpact NVARCHAR(20) NULL,       -- low, medium, high
        GitCommitHash NVARCHAR(64) NULL,
        SuccessScore DECIMAL(5,4) NULL,          -- 0.0000 to 1.0000
        TestsPassed INT NULL,
        TestsFailed INT NULL,
        PerformanceDelta DECIMAL(10,4) NULL,     -- Percentage change in performance
        ErrorMessage NVARCHAR(MAX) NULL,
        WasDeployed BIT NOT NULL DEFAULT 0,
        WasRolledBack BIT NOT NULL DEFAULT 0,
        StartedAt DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
        CompletedAt DATETIME2(7) NULL,
        RolledBackAt DATETIME2(7) NULL,
        
        CONSTRAINT PK_AutonomousImprovementHistory PRIMARY KEY CLUSTERED (ImprovementId),
        CONSTRAINT CK_AutonomousImprovement_SuccessScore CHECK (SuccessScore >= 0 AND SuccessScore <= 1)
    );

    -- Index for chronological queries
    CREATE NONCLUSTERED INDEX IX_AutonomousImprovement_StartedAt
        ON dbo.AutonomousImprovementHistory(StartedAt DESC)
        INCLUDE (ChangeType, SuccessScore, WasDeployed);

    -- Index for success analysis
    CREATE NONCLUSTERED INDEX IX_AutonomousImprovement_SuccessScore
        ON dbo.AutonomousImprovementHistory(SuccessScore DESC)
        WHERE WasDeployed = 1 AND WasRolledBack = 0;

    -- Index for failure analysis
    CREATE NONCLUSTERED INDEX IX_AutonomousImprovement_Failures
        ON dbo.AutonomousImprovementHistory(ChangeType, RiskLevel)
        INCLUDE (ErrorMessage, SuccessScore)
        WHERE WasDeployed = 1;

    PRINT 'Created dbo.AutonomousImprovementHistory table';
END
GO
