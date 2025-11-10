-- Tenant security policy table for multi-tenant CLR security controls
-- Enables tenant-specific whitelisting of unsafe CLR operations (shell commands, file I/O)
-- Used by clr_ExecuteShellCommand, clr_WriteFile, etc. to enforce security boundaries

CREATE TABLE [dbo].[TenantSecurityPolicy]
(
    [TenantId] INT NOT NULL,
    [AllowUnsafeClr] BIT NOT NULL DEFAULT (0),
    [AllowShellCommands] BIT NOT NULL DEFAULT (0),
    [AllowFileSystemAccess] BIT NOT NULL DEFAULT (0),
    [AllowNetworkAccess] BIT NOT NULL DEFAULT (0),
    [ShellCommandWhitelist] NVARCHAR(MAX) NULL,
    [FilePathWhitelist] NVARCHAR(MAX) NULL,
    [NetworkEndpointWhitelist] NVARCHAR(MAX) NULL,
    [AuditLevel] TINYINT NOT NULL DEFAULT (2),
    [LogRetentionDays] INT NOT NULL DEFAULT (90),
    [CreatedUtc] DATETIME2 NOT NULL DEFAULT (SYSUTCDATETIME()),
    [UpdatedUtc] DATETIME2 NOT NULL DEFAULT (SYSUTCDATETIME()),
    [CreatedBy] NVARCHAR(256) NULL,
    [UpdatedBy] NVARCHAR(256) NULL,
    
    CONSTRAINT [PK_TenantSecurityPolicy] PRIMARY KEY CLUSTERED ([TenantId]),
    CONSTRAINT [CK_TenantSecurityPolicy_ShellCommandWhitelist]
        CHECK ([ShellCommandWhitelist] IS NULL OR ISJSON([ShellCommandWhitelist]) = 1),
    CONSTRAINT [CK_TenantSecurityPolicy_FilePathWhitelist]
        CHECK ([FilePathWhitelist] IS NULL OR ISJSON([FilePathWhitelist]) = 1),
    CONSTRAINT [CK_TenantSecurityPolicy_NetworkEndpointWhitelist]
        CHECK ([NetworkEndpointWhitelist] IS NULL OR ISJSON([NetworkEndpointWhitelist]) = 1),
    CONSTRAINT [CK_TenantSecurityPolicy_AuditLevel]
        CHECK ([AuditLevel] BETWEEN 0 AND 3)
);
GO

CREATE NONCLUSTERED INDEX [IX_TenantSecurityPolicy_TenantId]
    ON [dbo].[TenantSecurityPolicy]([TenantId])
    INCLUDE ([AllowUnsafeClr], [AllowShellCommands], [AllowFileSystemAccess]);
GO