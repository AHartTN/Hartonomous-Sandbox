-- =============================================
-- Table: dbo.TenantAtoms
-- Description: Multi-tenant tracking for atom ownership
-- Purpose: Isolates atoms by tenant for multi-tenant deployments
-- =============================================

CREATE TABLE [dbo].[TenantAtom]
(
    [TenantId] INT NOT NULL,
    [AtomId] BIGINT NOT NULL,
    [CreatedAt] DATETIME2(7) NOT NULL CONSTRAINT [DF_TenantAtoms_CreatedAt] DEFAULT (SYSUTCDATETIME()),
    
    CONSTRAINT [PK_TenantAtoms] PRIMARY KEY CLUSTERED ([TenantId] ASC, [AtomId] ASC),
    
    CONSTRAINT [FK_TenantAtoms_Atoms] FOREIGN KEY ([AtomId])
        REFERENCES [dbo].[Atom]([AtomId]) ON DELETE CASCADE
);
GO
