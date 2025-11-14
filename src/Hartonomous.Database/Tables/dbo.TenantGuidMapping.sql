-- =============================================
-- Table: dbo.TenantGuidMapping
-- Description: Maps Azure Entra External ID tenant GUIDs to internal integer tenant IDs
-- Purpose: Provides safe, consistent GUID-to-INT mapping for multi-tenant isolation
-- Replaces: Unsafe GetHashCode() approach in HttpContextTenantContext
-- =============================================

CREATE TABLE [dbo].[TenantGuidMapping]
(
    [TenantId] INT NOT NULL IDENTITY(1,1),
    [TenantGuid] UNIQUEIDENTIFIER NOT NULL,
    [TenantName] NVARCHAR(200) NULL,
    [IsActive] BIT NOT NULL CONSTRAINT [DF_TenantGuidMapping_IsActive] DEFAULT 1,
    [CreatedAt] DATETIME2(7) NOT NULL CONSTRAINT [DF_TenantGuidMapping_CreatedAt] DEFAULT (SYSUTCDATETIME()),
    [CreatedBy] NVARCHAR(100) NULL,
    [ModifiedAt] DATETIME2(7) NULL,
    [ModifiedBy] NVARCHAR(100) NULL,

    CONSTRAINT [PK_TenantGuidMapping] PRIMARY KEY CLUSTERED ([TenantId] ASC),
    CONSTRAINT [UQ_TenantGuidMapping_TenantGuid] UNIQUE NONCLUSTERED ([TenantGuid])
);
GO

-- Index for reverse lookups (INT -> GUID)
CREATE NONCLUSTERED INDEX [IX_TenantGuidMapping_IsActive]
    ON [dbo].[TenantGuidMapping]([IsActive])
    INCLUDE ([TenantId], [TenantGuid], [TenantName]);
GO

EXEC sys.sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'Maps Azure Entra External ID tenant GUIDs to internal integer tenant IDs. Replaces unsafe GetHashCode() approach. Each Azure AD tenant GUID gets a stable, unique integer ID for use throughout the system.',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE',  @level1name = N'TenantGuidMapping';
GO
