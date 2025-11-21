-- =============================================
-- Table: dbo.TenantGuidMapping
-- Description: Maps Azure Entra tenant GUIDs to internal integer tenant IDs
-- Purpose: Replaces unsafe GetHashCode() approach with stable, unique integer IDs
-- =============================================

CREATE TABLE [dbo].[TenantGuidMapping]
(
    [TenantId] INT NOT NULL IDENTITY(1,1),
    [TenantGuid] UNIQUEIDENTIFIER NOT NULL,
    [TenantName] NVARCHAR(200) NOT NULL,
    [IsActive] BIT NOT NULL CONSTRAINT [DF_TenantGuidMapping_IsActive] DEFAULT 1,
    [CreatedAt] DATETIME2(7) NOT NULL CONSTRAINT [DF_TenantGuidMapping_CreatedAt] DEFAULT (SYSUTCDATETIME()),
    [CreatedBy] NVARCHAR(100) NULL,
    [ModifiedAt] DATETIME2(7) NULL,
    [ModifiedBy] NVARCHAR(100) NULL,

    CONSTRAINT [PK_TenantGuidMapping] PRIMARY KEY CLUSTERED ([TenantId] ASC),
    CONSTRAINT [UQ_TenantGuidMapping_TenantGuid] UNIQUE NONCLUSTERED ([TenantGuid])
);
GO

CREATE NONCLUSTERED INDEX [IX_TenantGuidMapping_IsActive]
    ON [dbo].[TenantGuidMapping]([IsActive])
    INCLUDE ([TenantId], [TenantGuid], [TenantName]);
GO

EXEC sys.sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'Maps Azure Entra External ID tenant GUIDs to stable internal integer tenant IDs. Replaces unsafe GetHashCode() approach with ACID-compliant GUID-to-INT mapping.',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE',  @level1name = N'TenantGuidMapping';
GO
