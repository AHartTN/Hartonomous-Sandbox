CREATE TABLE graph.AtomGraphNodes
(
    AtomId              BIGINT          NOT NULL,
    Modality            NVARCHAR(64)    NOT NULL,
    Subtype             NVARCHAR(64)    NULL,
    SourceType          NVARCHAR(128)   NULL,
    SourceUri           NVARCHAR(2048)  NULL,
    PayloadLocator      NVARCHAR(512)   NULL,
    CanonicalText       NVARCHAR(MAX)   NULL,
    Metadata            JSON   NULL,
    Semantics           JSON   NULL,
    SpatialKey          GEOMETRY        NULL,
    CreatedAt           DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAt           DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_AtomGraphNodes PRIMARY KEY CLUSTERED (AtomId),
    CONSTRAINT UX_AtomGraphNodes_AtomId UNIQUE NONCLUSTERED (AtomId)
) AS NODE;
GO