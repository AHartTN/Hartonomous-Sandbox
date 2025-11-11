-- =============================================
-- dbo.Atoms: The Core Metadata Table for All Atoms
-- =============================================
CREATE TABLE dbo.Atoms
(
    AtomId              BIGINT          IDENTITY(1,1) NOT NULL PRIMARY KEY,
    ContentHash         BINARY(32)      NOT NULL,
    Modality            NVARCHAR(64)    NOT NULL,
    Subtype             NVARCHAR(128)   NULL,
    SourceUri           NVARCHAR(1024)  NULL,
    SourceType          NVARCHAR(128)   NULL,
    CanonicalText       NVARCHAR(MAX)   NULL,
    PayloadLocator      NVARCHAR(1024)  NULL,
    Metadata            JSON            NULL,
    CreatedAt           DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAt           DATETIME2       NULL,
    TenantId            INT             NOT NULL DEFAULT 0,
    IsDeleted           BIT             NOT NULL DEFAULT 0,
    DeletedAt           DATETIME2       NULL,
    IsActive            BIT             NOT NULL DEFAULT 1,
    ReferenceCount      BIGINT          NOT NULL DEFAULT 0,
    SpatialKey          GEOMETRY        NULL,
    ComponentStream     VARBINARY(MAX)  NULL,
    INDEX UX_Atoms_ContentHash_TenantId UNIQUE (ContentHash, TenantId) WHERE (IsDeleted = 0),
    INDEX IX_Atoms_Modality_Subtype (Modality, Subtype)
);