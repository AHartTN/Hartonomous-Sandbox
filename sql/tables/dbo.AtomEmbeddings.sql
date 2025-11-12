-- =============================================
-- Table: dbo.AtomEmbeddings
-- =============================================
-- Represents an embedding associated with an atom.
-- This table was previously managed by EF Core.
-- =============================================

IF OBJECT_ID('dbo.AtomEmbeddings', 'U') IS NOT NULL
    DROP TABLE dbo.AtomEmbeddings;
GO

CREATE TABLE dbo.AtomEmbeddings
(
    AtomEmbeddingId         BIGINT          IDENTITY(1,1) NOT NULL PRIMARY KEY,
    AtomId                  BIGINT          NOT NULL,
    ModelId                 INT             NULL,
    EmbeddingType           NVARCHAR(128)   NOT NULL,
    Dimension               INT             NOT NULL DEFAULT 0,
    EmbeddingVector         VECTOR(1998)    NULL,
    UsesMaxDimensionPadding BIT             NOT NULL DEFAULT 0,
    SpatialProjX            FLOAT           NULL,
    SpatialProjY            FLOAT           NULL,
    SpatialProjZ            FLOAT           NULL,
    SpatialGeometry         GEOMETRY        NULL,
    SpatialCoarse           GEOMETRY        NULL,
    SpatialBucket           INT             NOT NULL,
    SpatialBucketX          INT             NULL,
    SpatialBucketY          INT             NULL,
    SpatialBucketZ          INT             NOT NULL DEFAULT -2147483648, -- int.MinValue
    Metadata                NVARCHAR(MAX)   NULL,
    CreatedAt               DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
    LastUpdated             DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),

    CONSTRAINT FK_AtomEmbeddings_Atoms FOREIGN KEY (AtomId) REFERENCES dbo.Atoms(AtomId) ON DELETE CASCADE,
    CONSTRAINT FK_AtomEmbeddings_Models FOREIGN KEY (ModelId) REFERENCES dbo.Models(ModelId) ON DELETE NO ACTION,
    CONSTRAINT CK_AtomEmbeddings_Metadata_IsJson CHECK (Metadata IS NULL OR ISJSON(Metadata) = 1)
);
GO

CREATE INDEX IX_AtomEmbeddings_Atom_Model_Type ON dbo.AtomEmbeddings(AtomId, EmbeddingType, ModelId);
GO

CREATE INDEX IX_AtomEmbeddings_SpatialBucket ON dbo.AtomEmbeddings(SpatialBucketX, SpatialBucketY, SpatialBucketZ);
GO

PRINT 'Created table dbo.AtomEmbeddings';
GO