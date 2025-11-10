-- =============================================
-- Table: dbo.ModelLayers
-- Description: Represents layers in decomposed neural network models.
--              Stores weights as GEOMETRY (LINESTRING ZM) for variable-dimension tensors with spatial indexing.
--              X = index, Y = weight value, Z = importance/gradient, M = iteration/depth.
-- =============================================
CREATE TABLE [dbo].[ModelLayers]
(
    [LayerId]                  BIGINT           NOT NULL IDENTITY(1,1),
    [ModelId]                  INT              NOT NULL,
    [LayerIdx]                 INT              NOT NULL,
    [LayerName]                NVARCHAR(100)    NULL,
    [LayerType]                NVARCHAR(50)     NULL,
    [WeightsGeometry]          GEOMETRY         NULL,
    [TensorShape]              NVARCHAR(200)    NULL,
    [TensorDtype]              NVARCHAR(20)     NULL CONSTRAINT DF_ModelLayers_TensorDtype DEFAULT ('float32'),
    [QuantizationType]         NVARCHAR(20)     NULL,
    [QuantizationScale]        FLOAT            NULL,
    [QuantizationZeroPoint]    FLOAT            NULL,
    [Parameters]               NVARCHAR(MAX)    NULL, -- JSON column type applied via CHECK constraint
    [ParameterCount]           BIGINT           NULL,
    [ZMin]                     FLOAT            NULL,
    [ZMax]                     FLOAT            NULL,
    [MMin]                     FLOAT            NULL,
    [MMax]                     FLOAT            NULL,
    [MortonCode]               BIGINT           NULL,
    [PreviewPointCount]        INT              NULL,
    [CacheHitRate]             FLOAT            NULL CONSTRAINT DF_ModelLayers_CacheHitRate DEFAULT (0.0),
    [AvgComputeTimeMs]         FLOAT            NULL,
    [LayerAtomId]              BIGINT           NULL,

    CONSTRAINT [PK_ModelLayers] PRIMARY KEY CLUSTERED ([LayerId] ASC),

    CONSTRAINT [FK_ModelLayers_Models] 
        FOREIGN KEY ([ModelId]) 
        REFERENCES [dbo].[Models]([ModelId]) 
        ON DELETE CASCADE,

    CONSTRAINT [FK_ModelLayers_Atoms] 
        FOREIGN KEY ([LayerAtomId]) 
        REFERENCES [dbo].[Atoms]([AtomId]) 
        ON DELETE SET NULL,

    CONSTRAINT [CK_ModelLayers_Parameters_IsJson] 
        CHECK ([Parameters] IS NULL OR ISJSON([Parameters]) = 1)
);
GO

CREATE NONCLUSTERED INDEX [IX_ModelLayers_ModelId_LayerIdx]
    ON [dbo].[ModelLayers]([ModelId] ASC, [LayerIdx] ASC);
GO

CREATE NONCLUSTERED INDEX [IX_ModelLayers_LayerType]
    ON [dbo].[ModelLayers]([LayerType] ASC);
GO

CREATE NONCLUSTERED INDEX [IX_ModelLayers_Z_Range]
    ON [dbo].[ModelLayers]([ModelId] ASC, [ZMin] ASC, [ZMax] ASC);
GO

CREATE NONCLUSTERED INDEX [IX_ModelLayers_M_Range]
    ON [dbo].[ModelLayers]([ModelId] ASC, [MMin] ASC, [MMax] ASC);
GO

CREATE NONCLUSTERED INDEX [IX_ModelLayers_Morton]
    ON [dbo].[ModelLayers]([MortonCode] ASC);
GO

CREATE NONCLUSTERED INDEX [IX_ModelLayers_LayerAtomId]
    ON [dbo].[ModelLayers]([LayerAtomId] ASC);
GO