CREATE TABLE [dbo].[Atoms] (
    [AtomId]          BIGINT           NOT NULL IDENTITY,
    [ContentHash]     BINARY (32)      NOT NULL,
    [Modality]        NVARCHAR (64)    NOT NULL,
    [Subtype]         NVARCHAR (128)   NULL,
    [SourceUri]       NVARCHAR (1024)  NULL,
    [SourceType]      NVARCHAR (128)   NULL,
    
    -- Atomic payload (SMALL - typically 1-64 bytes for true atoms)
    [AtomicValue]     VARBINARY (64)   NULL,  -- Raw bytes: char, RGB, float32, etc.
    [CanonicalText]   NVARCHAR (256)   NULL,  -- Text representation (reduced from MAX)
    
    -- Legacy fields (for backward compatibility during migration)
    [Content]         NVARCHAR (MAX)   NULL,
    [ContentType]     NVARCHAR (128)   NULL,
    [PayloadLocator]  NVARCHAR (1024)  NULL,
    [ComponentStream] VARBINARY (MAX)  NULL,  -- Deprecated: use atomic decomposition instead
    
    -- Metadata
    [Metadata]        JSON    NULL,
    [CreatedAt]       DATETIME2 (7)    NOT NULL DEFAULT (SYSUTCDATETIME()),
    [CreatedUtc]      DATETIME2 (7)    NOT NULL DEFAULT (SYSUTCDATETIME()),
    [UpdatedAt]       DATETIME2 (7)    NULL,
    
    -- Management
    [IsActive]        BIT              NOT NULL DEFAULT CAST(1 AS BIT),
    [IsDeleted]       BIT              NOT NULL DEFAULT CAST(0 AS BIT),
    [TenantId]        INT              NOT NULL DEFAULT CAST(0 AS INT),
    [ReferenceCount]  BIGINT           NOT NULL DEFAULT CAST(0 AS BIGINT),
    
    -- Spatial indexing (multi-dimensional coordinates)
    [SpatialKey]      GEOMETRY         NULL,  -- Position, color space, tensor coords, etc.
    [SpatialGeography] GEOGRAPHY       NULL,  -- True geospatial data (lat/lon)
    
    CONSTRAINT [PK_Atoms] PRIMARY KEY CLUSTERED ([AtomId] ASC),
    CONSTRAINT [UQ_Atoms_ContentHash] UNIQUE NONCLUSTERED ([ContentHash]),
    INDEX [IX_Atoms_Modality_Subtype] NONCLUSTERED ([Modality], [Subtype]),
    INDEX [IX_Atoms_References] NONCLUSTERED ([ReferenceCount] DESC),
    INDEX [IX_Atoms_TenantActive] NONCLUSTERED ([TenantId], [IsActive], [IsDeleted])
);
