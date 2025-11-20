-- =============================================
-- PendingActions: OODA Loop Action Queue
-- Tracks autonomous system decisions awaiting approval or execution
-- =============================================
CREATE TABLE dbo.PendingActions (
    ActionId BIGINT IDENTITY(1,1) NOT NULL,
    ActionType NVARCHAR(100) NOT NULL,
    SqlStatement NVARCHAR(MAX) NULL,
    Description NVARCHAR(MAX) NULL,
    Parameters json NULL,  -- JSON parameters for action execution
    Priority INT NOT NULL DEFAULT 5,  -- 1-10 priority ranking
    Status NVARCHAR(50) NOT NULL DEFAULT 'PendingApproval',
    RiskLevel NVARCHAR(20) NOT NULL DEFAULT 'medium',
    EstimatedImpact NVARCHAR(20) NULL,
    CreatedUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    ApprovedUtc DATETIME2 NULL,
    ApprovedBy NVARCHAR(128) NULL,
    ExecutedUtc DATETIME2 NULL,
    ResultJson json NULL,  -- JSON for action execution results
    ErrorMessage NVARCHAR(MAX) NULL,
    CONSTRAINT PK_PendingActions PRIMARY KEY CLUSTERED (ActionId),
    INDEX IX_PendingActions_Status (Status),
    INDEX IX_PendingActions_Priority (Priority DESC, CreatedUtc DESC) WHERE Status = 'PendingApproval',
    INDEX IX_PendingActions_Created (CreatedUtc DESC)
);