-- =============================================
-- Hartonomous Atom Substrate Core Schema
-- Aligns database objects with the Atom* entities in Hartonomous.Core
-- =============================================

USE Hartonomous;
GO

PRINT '============================================================';
PRINT 'HARTONOMOUS ATOM SUBSTRATE SCHEMA';
PRINT '============================================================';
GO

-- =============================================
-- Core Atoms table
-- =============================================
IF OBJECT_ID('dbo.Atoms', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Atoms
    (
        AtomId           BIGINT IDENTITY(1,1)    NOT NULL PRIMARY KEY,
        ContentHash      BINARY(32)              NOT NULL,
        Modality         NVARCHAR(64)            NOT NULL,
        Subtype          NVARCHAR(128)           NULL,
        SourceUri        NVARCHAR(1024)          NULL,
        SourceType       NVARCHAR(128)           NULL,
        CanonicalText    NVARCHAR(MAX)           NULL,
        PayloadLocator   NVARCHAR(1024)          NULL,
        Metadata         JSON                    NULL,
        CreatedAt        DATETIME2               NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedAt        DATETIME2               NULL,
        IsActive         BIT                     NOT NULL DEFAULT 1,
        ReferenceCount   BIGINT                  NOT NULL DEFAULT 0,
        SpatialKey       GEOMETRY                NULL
    );
    PRINT 'Table dbo.Atoms created.';
END
ELSE
BEGIN
    PRINT 'Table dbo.Atoms already exists. Ensure columns align with Hartonomous.Core.Entities.Atom.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_Atoms_ContentHash' AND object_id = OBJECT_ID('dbo.Atoms'))
BEGIN
    CREATE UNIQUE INDEX UX_Atoms_ContentHash ON dbo.Atoms(ContentHash);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Atoms_Modality' AND object_id = OBJECT_ID('dbo.Atoms'))
BEGIN
    CREATE INDEX IX_Atoms_Modality ON dbo.Atoms(Modality);
END
GO

-- =============================================
-- Atom Embeddings
-- =============================================
IF OBJECT_ID('dbo.AtomEmbeddings', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AtomEmbeddings
    (
        AtomEmbeddingId           BIGINT IDENTITY(1,1)  NOT NULL PRIMARY KEY,
        AtomId                    BIGINT                NOT NULL,
        ModelId                   INT                   NULL,
        EmbeddingType             NVARCHAR(128)         NOT NULL,
        Dimension                 INT                   NOT NULL DEFAULT 0,
        EmbeddingVector           VECTOR(1998)          NULL,
        UsesMaxDimensionPadding   BIT                   NOT NULL DEFAULT 0,
        SpatialGeometry           GEOMETRY              NULL,
        SpatialCoarse             GEOMETRY              NULL,
        Metadata                  JSON                  NULL,
        CreatedAt                 DATETIME2             NOT NULL DEFAULT SYSUTCDATETIME()
    );
    PRINT 'Table dbo.AtomEmbeddings created.';
END
ELSE
BEGIN
    PRINT 'Table dbo.AtomEmbeddings already exists. Validate schema manually.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_AtomEmbeddings_Atoms')
BEGIN
    ALTER TABLE dbo.AtomEmbeddings
        ADD CONSTRAINT FK_AtomEmbeddings_Atoms
            FOREIGN KEY (AtomId)
            REFERENCES dbo.Atoms(AtomId)
            ON DELETE CASCADE;
END
GO

IF OBJECT_ID('dbo.Models', 'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_AtomEmbeddings_Models')
    BEGIN
        ALTER TABLE dbo.AtomEmbeddings
            ADD CONSTRAINT FK_AtomEmbeddings_Models
                FOREIGN KEY (ModelId)
                REFERENCES dbo.Models(ModelId)
                ON DELETE NO ACTION;
    END
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AtomEmbeddings_Atom_Model_Type' AND object_id = OBJECT_ID('dbo.AtomEmbeddings'))
BEGIN
    CREATE INDEX IX_AtomEmbeddings_Atom_Model_Type
        ON dbo.AtomEmbeddings(AtomId, EmbeddingType, ModelId);
END
GO

-- =============================================
-- Atom Embedding Components (variable dimension support)
-- =============================================
IF OBJECT_ID('dbo.AtomEmbeddingComponents', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AtomEmbeddingComponents
    (
        AtomEmbeddingComponentId  BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        AtomEmbeddingId           BIGINT               NOT NULL,
        ComponentIndex            INT                  NOT NULL,
        ComponentValue            REAL                 NOT NULL
    );
    PRINT 'Table dbo.AtomEmbeddingComponents created.';
END
ELSE
BEGIN
    PRINT 'Table dbo.AtomEmbeddingComponents already exists. Validate schema manually.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_AtomEmbeddingComponents_AtomEmbeddings')
BEGIN
    ALTER TABLE dbo.AtomEmbeddingComponents
        ADD CONSTRAINT FK_AtomEmbeddingComponents_AtomEmbeddings
            FOREIGN KEY (AtomEmbeddingId)
            REFERENCES dbo.AtomEmbeddings(AtomEmbeddingId)
            ON DELETE CASCADE;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_AtomEmbeddingComponents_Component' AND object_id = OBJECT_ID('dbo.AtomEmbeddingComponents'))
BEGIN
    CREATE UNIQUE INDEX UX_AtomEmbeddingComponents_Component
        ON dbo.AtomEmbeddingComponents(AtomEmbeddingId, ComponentIndex);
END
GO

-- =============================================
-- Tensor atoms (decomposed tensors and coefficients)
-- =============================================
IF OBJECT_ID('dbo.TensorAtoms', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.TensorAtoms
    (
        TensorAtomId        BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        AtomId              BIGINT              NOT NULL,
        ModelId             INT                 NULL,
        LayerId             BIGINT              NULL,
        AtomType            NVARCHAR(128)       NOT NULL,
        SpatialSignature    GEOMETRY            NULL,
        GeometryFootprint   GEOMETRY            NULL,
        Metadata            JSON                NULL,
        ImportanceScore     REAL                NULL,
        CreatedAt           DATETIME2           NOT NULL DEFAULT SYSUTCDATETIME()
    );
    PRINT 'Table dbo.TensorAtoms created.';
END
ELSE
BEGIN
    PRINT 'Table dbo.TensorAtoms already exists. Validate schema manually.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_TensorAtoms_Atoms')
BEGIN
    ALTER TABLE dbo.TensorAtoms
        ADD CONSTRAINT FK_TensorAtoms_Atoms
            FOREIGN KEY (AtomId)
            REFERENCES dbo.Atoms(AtomId)
            ON DELETE CASCADE;
END
GO

IF OBJECT_ID('dbo.Models', 'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_TensorAtoms_Models')
    BEGIN
        ALTER TABLE dbo.TensorAtoms
            ADD CONSTRAINT FK_TensorAtoms_Models
                FOREIGN KEY (ModelId)
                REFERENCES dbo.Models(ModelId)
                ON DELETE NO ACTION;
    END
END
GO

IF OBJECT_ID('dbo.ModelLayers', 'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_TensorAtoms_ModelLayers')
    BEGIN
        ALTER TABLE dbo.TensorAtoms
            ADD CONSTRAINT FK_TensorAtoms_ModelLayers
                FOREIGN KEY (LayerId)
                REFERENCES dbo.ModelLayers(LayerId)
                ON DELETE NO ACTION;
    END
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TensorAtoms_Model_Layer_Type' AND object_id = OBJECT_ID('dbo.TensorAtoms'))
BEGIN
    CREATE INDEX IX_TensorAtoms_Model_Layer_Type
        ON dbo.TensorAtoms(ModelId, LayerId, AtomType);
END
GO

IF OBJECT_ID('dbo.TensorAtomCoefficients', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.TensorAtomCoefficients
    (
        TensorAtomCoefficientId  BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        TensorAtomId             BIGINT              NOT NULL,
        ParentLayerId            BIGINT              NOT NULL,
        TensorRole               NVARCHAR(128)       NULL,
        Coefficient              REAL                NOT NULL
    );
    PRINT 'Table dbo.TensorAtomCoefficients created.';
END
ELSE
BEGIN
    PRINT 'Table dbo.TensorAtomCoefficients already exists. Validate schema manually.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_TensorAtomCoefficients_TensorAtoms')
BEGIN
    ALTER TABLE dbo.TensorAtomCoefficients
        ADD CONSTRAINT FK_TensorAtomCoefficients_TensorAtoms
            FOREIGN KEY (TensorAtomId)
            REFERENCES dbo.TensorAtoms(TensorAtomId)
            ON DELETE CASCADE;
END
GO

IF OBJECT_ID('dbo.ModelLayers', 'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_TensorAtomCoefficients_ModelLayers')
    BEGIN
        ALTER TABLE dbo.TensorAtomCoefficients
            ADD CONSTRAINT FK_TensorAtomCoefficients_ModelLayers
                FOREIGN KEY (ParentLayerId)
                REFERENCES dbo.ModelLayers(LayerId)
                ON DELETE CASCADE;
    END
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TensorAtomCoefficients_Lookup' AND object_id = OBJECT_ID('dbo.TensorAtomCoefficients'))
BEGIN
    CREATE INDEX IX_TensorAtomCoefficients_Lookup
        ON dbo.TensorAtomCoefficients(TensorAtomId, ParentLayerId, TensorRole);
END
GO

-- =============================================
-- Atom relations graph
-- =============================================
IF OBJECT_ID('dbo.AtomRelations', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AtomRelations
    (
        AtomRelationId    BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        SourceAtomId      BIGINT              NOT NULL,
        TargetAtomId      BIGINT              NOT NULL,
        RelationType      NVARCHAR(128)       NOT NULL,
        Weight            REAL                NULL,
        SpatialExpression GEOMETRY            NULL,
        Metadata          JSON                NULL,
        CreatedAt         DATETIME2           NOT NULL DEFAULT SYSUTCDATETIME()
    );
    PRINT 'Table dbo.AtomRelations created.';
END
ELSE
BEGIN
    PRINT 'Table dbo.AtomRelations already exists. Validate schema manually.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_AtomRelations_SourceAtom')
BEGIN
    ALTER TABLE dbo.AtomRelations
        ADD CONSTRAINT FK_AtomRelations_SourceAtom
            FOREIGN KEY (SourceAtomId)
            REFERENCES dbo.Atoms(AtomId)
            ON DELETE NO ACTION;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_AtomRelations_TargetAtom')
BEGIN
    ALTER TABLE dbo.AtomRelations
        ADD CONSTRAINT FK_AtomRelations_TargetAtom
            FOREIGN KEY (TargetAtomId)
            REFERENCES dbo.Atoms(AtomId)
            ON DELETE NO ACTION;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AtomRelations_Source_Target_Type' AND object_id = OBJECT_ID('dbo.AtomRelations'))
BEGIN
    CREATE INDEX IX_AtomRelations_Source_Target_Type
        ON dbo.AtomRelations(SourceAtomId, TargetAtomId, RelationType);
END
GO

-- =============================================
-- Ingestion jobs and deduplication policies
-- =============================================
IF OBJECT_ID('dbo.IngestionJobs', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.IngestionJobs
    (
        IngestionJobId BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        PipelineName   NVARCHAR(256)        NOT NULL,
        StartedAt      DATETIME2            NOT NULL DEFAULT SYSUTCDATETIME(),
        CompletedAt    DATETIME2            NULL,
        Status         NVARCHAR(64)         NULL,
        SourceUri      NVARCHAR(1024)       NULL,
        Metadata       JSON                 NULL
    );
    PRINT 'Table dbo.IngestionJobs created.';
END
ELSE
BEGIN
    PRINT 'Table dbo.IngestionJobs already exists. Validate schema manually.';
END
GO

IF OBJECT_ID('dbo.IngestionJobAtoms', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.IngestionJobAtoms
    (
        IngestionJobAtomId BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        IngestionJobId     BIGINT               NOT NULL,
        AtomId             BIGINT               NOT NULL,
        WasDuplicate       BIT                  NOT NULL DEFAULT 0,
        Notes              NVARCHAR(1024)       NULL
    );
    PRINT 'Table dbo.IngestionJobAtoms created.';
END
ELSE
BEGIN
    PRINT 'Table dbo.IngestionJobAtoms already exists. Validate schema manually.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_IngestionJobAtoms_IngestionJobs')
BEGIN
    ALTER TABLE dbo.IngestionJobAtoms
        ADD CONSTRAINT FK_IngestionJobAtoms_IngestionJobs
            FOREIGN KEY (IngestionJobId)
            REFERENCES dbo.IngestionJobs(IngestionJobId)
            ON DELETE CASCADE;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_IngestionJobAtoms_Atoms')
BEGIN
    ALTER TABLE dbo.IngestionJobAtoms
        ADD CONSTRAINT FK_IngestionJobAtoms_Atoms
            FOREIGN KEY (AtomId)
            REFERENCES dbo.Atoms(AtomId)
            ON DELETE NO ACTION;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_IngestionJobAtoms_Job_Atom' AND object_id = OBJECT_ID('dbo.IngestionJobAtoms'))
BEGIN
    CREATE INDEX IX_IngestionJobAtoms_Job_Atom
        ON dbo.IngestionJobAtoms(IngestionJobId, AtomId);
END
GO

IF OBJECT_ID('dbo.DeduplicationPolicies', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.DeduplicationPolicies
    (
        DeduplicationPolicyId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        PolicyName            NVARCHAR(128)     NOT NULL,
        SemanticThreshold     FLOAT             NULL,
        SpatialThreshold      FLOAT             NULL,
        Metadata              JSON              NULL,
        IsActive              BIT               NOT NULL DEFAULT 1,
        CreatedAt             DATETIME2         NOT NULL DEFAULT SYSUTCDATETIME()
    );
    PRINT 'Table dbo.DeduplicationPolicies created.';
END
ELSE
BEGIN
    PRINT 'Table dbo.DeduplicationPolicies already exists. Validate schema manually.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_DeduplicationPolicies_Name' AND object_id = OBJECT_ID('dbo.DeduplicationPolicies'))
BEGIN
    CREATE UNIQUE INDEX UX_DeduplicationPolicies_Name
        ON dbo.DeduplicationPolicies(PolicyName);
END
GO

IF NOT EXISTS (
        SELECT 1
        FROM dbo.DeduplicationPolicies
        WHERE PolicyName = 'default')
BEGIN
    INSERT INTO dbo.DeduplicationPolicies (PolicyName, SemanticThreshold, SpatialThreshold, Metadata, IsActive)
    VALUES ('default', 0.95, NULL, NULL, 1);
    PRINT 'Seeded default deduplication policy (cosine >= 0.95).';
END
GO

PRINT 'Hartonomous atom substrate schema verification complete.';
PRINT 'Ensure EF Core migrations remain in sync with this script.';
PRINT '============================================================';
GO
