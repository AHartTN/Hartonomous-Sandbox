-- =============================================
-- PendingActions: OODA Loop Action Queue
-- Tracks autonomous system decisions awaiting approval or execution
-- =============================================
CREATE TABLE dbo.PendingActions (
    ActionId BIGINT IDENTITY(1,1) NOT NULL,
    ActionType NVARCHAR(100) NOT NULL,
    
    -- Target entity tracking
    TargetEntity NVARCHAR(128) NULL,      -- Entity type (InferenceRequest, AtomRelation, etc.)
    TargetId BIGINT NULL,                 -- ID of the target entity
    
    -- Action details
    SqlStatement NVARCHAR(MAX) NULL,
    Description NVARCHAR(MAX) NULL,
    Parameters json NULL,                 -- JSON parameters for action execution
    Metadata json NULL,                   -- Additional metadata (user info, context, etc.)
    
    -- Priority and risk
    Priority NVARCHAR(20) NOT NULL DEFAULT 'Medium',  -- Changed from INT to NVARCHAR to match sp_ProcessFeedback
    Status NVARCHAR(50) NOT NULL DEFAULT 'PendingApproval',
    RiskLevel NVARCHAR(20) NOT NULL DEFAULT 'medium',
    EstimatedImpact NVARCHAR(20) NULL,
    
    -- Timestamps
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),  -- Added CreatedAt alias for CreatedUtc
    CreatedUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    ApprovedUtc DATETIME2 NULL,
    ApprovedBy NVARCHAR(128) NULL,
    ExecutedUtc DATETIME2 NULL,
    
    -- Execution results
    ResultJson json NULL,                 -- JSON for action execution results
    ErrorMessage NVARCHAR(MAX) NULL,
    
    CONSTRAINT PK_PendingActions PRIMARY KEY CLUSTERED (ActionId),
    INDEX IX_PendingActions_Status (Status),
    INDEX IX_PendingActions_Priority (CreatedUtc DESC) WHERE Status = 'PendingApproval',
    INDEX IX_PendingActions_Created (CreatedUtc DESC),
    INDEX IX_PendingActions_TargetEntity (TargetEntity, TargetId)
);