CREATE TABLE [dbo].[AutonomousImprovementHistory] (
    [ImprovementId]   UNIQUEIDENTIFIER NOT NULL DEFAULT (NEWID()),
    
    -- Core improvement tracking
    [ImprovementType] NVARCHAR (100)   NULL,         -- Type of improvement (FeedbackWeightAdjustment, CodeGeneration, etc.)
    [TargetEntity]    NVARCHAR (128)   NULL,         -- Entity being improved (AtomRelation, Procedure, etc.)
    [TargetId]        BIGINT           NULL,         -- ID of the target entity
    
    -- Code generation fields (for autonomous code improvements)
    [AnalysisResults] NVARCHAR (MAX)   NULL,         -- Changed from NOT NULL to NULL
    [GeneratedCode]   NVARCHAR (MAX)   NULL,         -- Changed from NOT NULL to NULL
    [TargetFile]      NVARCHAR (512)   NULL,         -- Changed from NOT NULL to NULL
    
    -- Change tracking
    [ChangeType]      NVARCHAR (50)    NULL,         -- Changed from NOT NULL to NULL
    [OldValue]        NVARCHAR (MAX)   NULL,         -- Previous value before change
    [NewValue]        NVARCHAR (MAX)   NULL,         -- New value after change
    
    -- Risk and impact assessment
    [RiskLevel]       NVARCHAR (20)    NULL,         -- Changed from NOT NULL to NULL
    [EstimatedImpact] NVARCHAR (20)    NULL,
    
    -- Execution metadata
    [GitCommitHash]   NVARCHAR (64)    NULL,
    [ApprovedBy]      NVARCHAR (128)   NULL,         -- Who approved/triggered the change
    [ExecutedAt]      DATETIME2 (7)    NULL,         -- When the improvement was executed
    [Success]         BIT              NULL,         -- Whether the improvement succeeded
    [Notes]           NVARCHAR (MAX)   NULL,         -- Additional notes or context
    [ErrorMessage]    NVARCHAR (MAX)   NULL,
    
    -- Success metrics (for code improvements)
    [SuccessScore]    DECIMAL (5, 4)   NULL,
    [TestsPassed]     INT              NULL,
    [TestsFailed]     INT              NULL,
    [PerformanceDelta]DECIMAL (10, 4)  NULL,
    
    -- Deployment tracking (for code improvements)
    [WasDeployed]     BIT              NOT NULL DEFAULT (0),
    [WasRolledBack]   BIT              NOT NULL DEFAULT (0),
    [StartedAt]       DATETIME2 (7)    NOT NULL DEFAULT (SYSUTCDATETIME()),
    [CompletedAt]     DATETIME2 (7)    NULL,
    [RolledBackAt]    DATETIME2 (7)    NULL,
    
    CONSTRAINT [PK_AutonomousImprovementHistory] PRIMARY KEY CLUSTERED ([ImprovementId] ASC),
    CONSTRAINT [CK_AutonomousImprovement_SuccessScore] CHECK ([SuccessScore]>=(0) AND [SuccessScore]<=(1))
);
