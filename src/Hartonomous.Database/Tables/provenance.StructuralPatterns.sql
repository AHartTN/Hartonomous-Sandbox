-- =============================================
-- StructuralPatterns: N-Gram Pattern Detection
-- GREENFIELD IMPLEMENTATION - Part of core schema
-- =============================================
-- Purpose: Track repeating text patterns for promotion to atoms
-- Strategy: "Short-term memory" for ingestion optimization
-- Promotion: When Frequency >= threshold, create permanent Atom
-- =============================================
CREATE TABLE [provenance].[StructuralPatterns]
(
    [PatternId]         BIGINT IDENTITY(1,1) NOT NULL,
    [TenantId]          INT NOT NULL DEFAULT 0,
    [PatternHash]       BINARY(32) NOT NULL,
    [PatternType]       NVARCHAR(50) NOT NULL, -- 'N-Gram', 'Phrase', 'Code-Block', 'Sentence'
    [PatternText]       NVARCHAR(MAX) NULL, -- Original pattern text
    [Frequency]         BIGINT NOT NULL DEFAULT 1,
    [AvgImportance]     FLOAT NULL,
    [LastSeen]          DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
    [FirstSeen]         DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
    [ShouldPromote]     BIT NOT NULL DEFAULT 0, -- When TRUE, create permanent Atom
    [PromotedAtomId]    BIGINT NULL, -- Reference to promoted Atom

    CONSTRAINT [PK_StructuralPatterns] PRIMARY KEY CLUSTERED ([PatternId] ASC),
    CONSTRAINT [UX_StructuralPatterns_Hash_Tenant] 
        UNIQUE NONCLUSTERED ([PatternHash], [TenantId]),
    CONSTRAINT [FK_StructuralPatterns_PromotedAtom] 
        FOREIGN KEY ([PromotedAtomId]) REFERENCES [dbo].[Atom]([AtomId])
);
GO

-- Index for hot-path pattern lookup during ingestion
CREATE NONCLUSTERED INDEX [IX_StructuralPatterns_Frequency]
ON [provenance].[StructuralPatterns]([TenantId], [Frequency] DESC)
INCLUDE ([PatternHash], [ShouldPromote], [PromotedAtomId])
WHERE [ShouldPromote] = 1;
GO

-- Index for pattern type queries
CREATE NONCLUSTERED INDEX [IX_StructuralPatterns_Type]
ON [provenance].[StructuralPatterns]([TenantId], [PatternType], [Frequency] DESC)
INCLUDE ([PatternText], [LastSeen]);
GO
