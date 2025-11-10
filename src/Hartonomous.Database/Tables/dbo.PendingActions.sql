USE Hartonomous;
GO

IF OBJECT_ID(N'dbo.PendingActions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PendingActions (
        ActionId BIGINT IDENTITY(1,1) NOT NULL,
        ActionType NVARCHAR(100) NOT NULL,
        SqlStatement NVARCHAR(MAX) NULL,
        Description NVARCHAR(MAX) NULL,
        Status NVARCHAR(50) NOT NULL DEFAULT 'PendingApproval',
        RiskLevel NVARCHAR(20) NOT NULL DEFAULT 'medium',
        EstimatedImpact NVARCHAR(20) NULL,
        CreatedUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        ApprovedUtc DATETIME2 NULL,
        ApprovedBy NVARCHAR(128) NULL,
        ExecutedUtc DATETIME2 NULL,
        ResultJson NVARCHAR(MAX) NULL,
        ErrorMessage NVARCHAR(MAX) NULL,

        CONSTRAINT PK_PendingActions PRIMARY KEY CLUSTERED (ActionId)
    );
END
GO
IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_PendingActions_Status'
      AND object_id = OBJECT_ID(N'dbo.PendingActions', N'U')
)
BEGIN
    CREATE INDEX IX_PendingActions_Status ON dbo.PendingActions (Status);
END
GO
IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_PendingActions_Created'
      AND object_id = OBJECT_ID(N'dbo.PendingActions', N'U')
)
BEGIN
    CREATE INDEX IX_PendingActions_Created ON dbo.PendingActions (CreatedUtc DESC);
END
GO

PRINT 'Created PendingActions table';
