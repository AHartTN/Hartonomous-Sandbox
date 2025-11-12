-- =============================================
-- Table: dbo.TensorAtoms
-- =============================================
-- Represents a reusable tensor atom (kernel, basis vector, etc.)
-- This table was previously managed by EF Core.
-- =============================================

IF OBJECT_ID('dbo.TensorAtoms', 'U') IS NOT NULL
    DROP TABLE dbo.TensorAtoms;
GO

CREATE TABLE dbo.TensorAtoms
(
    TensorAtomId        BIGINT          NOT NULL PRIMARY KEY,
    AtomId              BIGINT          NOT NULL,
    ModelId             INT             NULL,
    LayerId             BIGINT          NULL,
    AtomType            NVARCHAR(128)   NOT NULL,
    SpatialSignature    GEOMETRY        NULL,
    GeometryFootprint   GEOMETRY        NULL,
    Metadata            NVARCHAR(MAX)   NULL, -- Storing as NVARCHAR(MAX) and ensuring it's valid JSON via CHECK constraint
    ImportanceScore     REAL            NULL,
    CreatedAt           DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),

    CONSTRAINT FK_TensorAtoms_Atoms FOREIGN KEY (AtomId) REFERENCES dbo.Atoms(AtomId) ON DELETE CASCADE,
    CONSTRAINT FK_TensorAtoms_Models FOREIGN KEY (ModelId) REFERENCES dbo.Models(ModelId) ON DELETE NO ACTION,
    CONSTRAINT FK_TensorAtoms_ModelLayers FOREIGN KEY (LayerId) REFERENCES dbo.ModelLayers(LayerId) ON DELETE NO ACTION,
    CONSTRAINT CK_TensorAtoms_Metadata_IsJson CHECK (Metadata IS NULL OR ISJSON(Metadata) = 1)
);
GO

CREATE INDEX IX_TensorAtoms_Model_Layer_Type ON dbo.TensorAtoms(ModelId, LayerId, AtomType);
GO

PRINT 'Created table dbo.TensorAtoms';
GO
