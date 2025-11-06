-- Tenant security policy table for multi-tenant CLR security controls
-- Enables tenant-specific whitelisting of unsafe CLR operations (shell commands, file I/O)
-- Used by clr_ExecuteShellCommand, clr_WriteFile, etc. to enforce security boundaries

CREATE TABLE dbo.TenantSecurityPolicy (
    TenantId INT PRIMARY KEY,
    
    -- CLR unsafe operation permissions
    AllowUnsafeClr BIT NOT NULL DEFAULT 0,
    AllowShellCommands BIT NOT NULL DEFAULT 0,
    AllowFileSystemAccess BIT NOT NULL DEFAULT 0,
    AllowNetworkAccess BIT NOT NULL DEFAULT 0,
    
    -- Whitelists (JSON arrays)
    ShellCommandWhitelist NVARCHAR(MAX), -- ["git", "python", "dotnet"]
    FilePathWhitelist NVARCHAR(MAX), -- ["/var/hartonomous/*", "D:\\Hartonomous\\*"]
    NetworkEndpointWhitelist NVARCHAR(MAX), -- ["https://api.openai.com", "bolt://localhost:7687"]
    
    -- Audit and logging
    AuditLevel TINYINT NOT NULL DEFAULT 2, -- 0=None, 1=Errors, 2=All, 3=Verbose
    LogRetentionDays INT NOT NULL DEFAULT 90,
    
    -- Metadata
    CreatedUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedUtc DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CreatedBy NVARCHAR(256),
    UpdatedBy NVARCHAR(256)
);
GO

-- Index for fast tenant lookup
CREATE INDEX IX_TenantSecurityPolicy_TenantId 
ON dbo.TenantSecurityPolicy(TenantId)
INCLUDE (AllowUnsafeClr, AllowShellCommands, AllowFileSystemAccess);
GO

-- CHECK constraints for JSON validity
ALTER TABLE dbo.TenantSecurityPolicy
ADD CONSTRAINT CK_TenantSecurityPolicy_ShellCommandWhitelist
CHECK (ShellCommandWhitelist IS NULL OR ISJSON(ShellCommandWhitelist) = 1);
GO

ALTER TABLE dbo.TenantSecurityPolicy
ADD CONSTRAINT CK_TenantSecurityPolicy_FilePathWhitelist
CHECK (FilePathWhitelist IS NULL OR ISJSON(FilePathWhitelist) = 1);
GO

ALTER TABLE dbo.TenantSecurityPolicy
ADD CONSTRAINT CK_TenantSecurityPolicy_NetworkEndpointWhitelist
CHECK (NetworkEndpointWhitelist IS NULL OR ISJSON(NetworkEndpointWhitelist) = 1);
GO

-- CHECK constraint for audit level
ALTER TABLE dbo.TenantSecurityPolicy
ADD CONSTRAINT CK_TenantSecurityPolicy_AuditLevel
CHECK (AuditLevel BETWEEN 0 AND 3);
GO

-- Insert default tenant (TenantId = 0 = system/local development)
INSERT INTO dbo.TenantSecurityPolicy (
    TenantId,
    AllowUnsafeClr,
    AllowShellCommands,
    AllowFileSystemAccess,
    AllowNetworkAccess,
    ShellCommandWhitelist,
    FilePathWhitelist,
    NetworkEndpointWhitelist,
    AuditLevel,
    CreatedBy
) VALUES (
    0, -- System tenant
    1, -- AllowUnsafeClr
    1, -- AllowShellCommands
    1, -- AllowFileSystemAccess
    1, -- AllowNetworkAccess
    '["git", "python", "dotnet", "pwsh", "powershell", "cmd"]', -- ShellCommandWhitelist
    '["D:\\\\Hartonomous\\\\*", "D:\\\\Repositories\\\\*", "/var/hartonomous/*"]', -- FilePathWhitelist
    '["https://api.openai.com", "bolt://localhost:7687", "http://localhost:*"]', -- NetworkEndpointWhitelist
    3, -- Verbose logging for development
    'SYSTEM'
);
GO
