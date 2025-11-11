CREATE TABLE dbo.TenantSecurityPolicy (
    TenantId INT NOT NULL,
    AllowUnsafeClr BIT NOT NULL DEFAULT 0,
    AllowShellCommands BIT NOT NULL DEFAULT 0,
    AllowFileSystemAccess BIT NOT NULL DEFAULT 0,
    AllowNetworkAccess BIT NOT NULL DEFAULT 0,
    ShellCommandWhitelist JSON,
    FilePathWhitelist JSON,
    NetworkEndpointWhitelist JSON,
    AuditLevel TINYINT NOT NULL DEFAULT 2,
    LogRetentionDays INT NOT NULL DEFAULT 90,
    CreatedUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CreatedBy NVARCHAR(256),
    UpdatedBy NVARCHAR(256),
    CONSTRAINT PK_TenantSecurityPolicy PRIMARY KEY (TenantId),
    CONSTRAINT CK_TenantSecurityPolicy_AuditLevel CHECK (AuditLevel BETWEEN 0 AND 3),
    INDEX IX_TenantSecurityPolicy_TenantId (TenantId) INCLUDE (AllowUnsafeClr, AllowShellCommands, AllowFileSystemAccess)
);