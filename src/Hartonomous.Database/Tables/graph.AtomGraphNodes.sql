CREATE TABLE graph.AtomGraphNodes
(
    AtomId              BIGINT          NOT NULL,
    Modality            NVARCHAR(64)    NOT NULL,
    Subtype             NVARCHAR(64)    NULL,
    SourceType          NVARCHAR(128)   NULL,
    SourceUri           NVARCHAR(2048)  NULL,
    PayloadLocator      NVARCHAR(512)   NULL,
    CanonicalText       NVARCHAR(MAX)   NULL,
    Metadata            NVARCHAR(MAX)   NULL,
    Semantics           NVARCHAR(MAX)   NULL,
    SpatialKey          GEOMETRY        NULL,
    CreatedAt           DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAt           DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT UX_AtomGraphNodes_AtomId UNIQUE (AtomId)
) AS NODE;
GO