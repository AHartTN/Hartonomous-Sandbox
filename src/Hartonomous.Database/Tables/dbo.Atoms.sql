-- Core atomic storage with temporal support and strict schema-level governance
CREATE TABLE [dbo].[Atoms] (
    [AtomId]          BIGINT           IDENTITY (1, 1) NOT NULL,
    [Modality]        VARCHAR(50)      NOT NULL,
    [Subtype]         VARCHAR(50)      NULL,
    [ContentHash]     BINARY(32)       NOT NULL,
    
    -- Schema-level governance: Max 64 bytes enforces atomic decomposition
    [AtomicValue]     VARBINARY(64)    NULL,

    -- Temporal columns
    [CreatedAt]       DATETIME2(7)     GENERATED ALWAYS AS ROW START NOT NULL,
    [ModifiedAt]      DATETIME2(7)     GENERATED ALWAYS AS ROW END NOT NULL,
    
    -- Reference counting for deduplication
    [ReferenceCount]  BIGINT           NOT NULL DEFAULT 1,
    
    CONSTRAINT [PK_Atoms] PRIMARY KEY CLUSTERED ([AtomId] ASC),
    CONSTRAINT [UX_Atoms_ContentHash] UNIQUE NONCLUSTERED ([ContentHash] ASC),
    PERIOD FOR SYSTEM_TIME ([CreatedAt], [ModifiedAt])
)
WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [dbo].[AtomsHistory]));
GO

-- Indexes created as separate index definition files in /Indexes folder
-- IX_Atoms_Modality
-- IX_Atoms_ReferenceCount
