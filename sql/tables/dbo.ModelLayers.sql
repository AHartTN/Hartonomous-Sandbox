-- =============================================
-- Table: dbo.ModelLayers
-- =============================================
-- Represents a single layer in a decomposed neural network model.
-- This table was previously managed by EF Core.
-- =============================================

IF OBJECT_ID('dbo.ModelLayers', 'U') IS NOT NULL
    DROP TABLE dbo.ModelLayers;
GO

CREATE TABLE dbo.ModelLayers
(
    LayerId              BIGINT          IDENTITY(1,1) NOT NULL PRIMARY KEY,
    ModelId              INT             NOT NULL,
    LayerIdx             INT             NOT NULL,
    LayerName            NVARCHAR(100)   NULL,
    LayerType            NVARCHAR(50)    NULL,
    WeightsGeometry      GEOMETRY        NULL,
    TensorShape          NVARCHAR(200)   NULL,
    TensorDtype          NVARCHAR(20)    NOT NULL DEFAULT 'float32',
    QuantizationType     NVARCHAR(20)    NULL,
    QuantizationScale    FLOAT           NULL,
    QuantizationZeroPoint FLOAT          NULL,
    Parameters           NVARCHAR(MAX)   NULL,
    ParameterCount       BIGINT          NULL,
    ZMin                 FLOAT           NULL,
    ZMax                 FLOAT           NULL,
    MMin                 FLOAT           NULL,
    MMax                 FLOAT           NULL,
    MortonCode           BIGINT          NULL,
    PreviewPointCount    INT             NULL,
    CacheHitRate         FLOAT           NULL DEFAULT 0.0,
    AvgComputeTimeMs     FLOAT           NULL,
    LayerAtomId          BIGINT          NULL,

    CONSTRAINT FK_ModelLayers_Models FOREIGN KEY (ModelId) REFERENCES dbo.Models(ModelId) ON DELETE CASCADE,
    CONSTRAINT FK_ModelLayers_Atoms FOREIGN KEY (LayerAtomId) REFERENCES dbo.Atoms(AtomId) ON DELETE SET NULL,
    CONSTRAINT CK_ModelLayers_Parameters_IsJson CHECK (Parameters IS NULL OR ISJSON(Parameters) = 1)
);
GO

CREATE INDEX IX_ModelLayers_ModelId_LayerIdx ON dbo.ModelLayers(ModelId, LayerIdx);
GO

CREATE INDEX IX_ModelLayers_LayerType ON dbo.ModelLayers(LayerType);
GO

CREATE INDEX IX_ModelLayers_Z_Range ON dbo.ModelLayers(ModelId, ZMin, ZMax);
GO

CREATE INDEX IX_ModelLayers_M_Range ON dbo.ModelLayers(ModelId, MMin, MMax);
GO

CREATE INDEX IX_ModelLayers_Morton ON dbo.ModelLayers(MortonCode);
GO

PRINT 'Created table dbo.ModelLayers';
GO
