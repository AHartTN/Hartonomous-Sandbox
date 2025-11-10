-- =============================================
-- Table: dbo.TensorAtoms
-- Description: Represents reusable tensor atoms (kernels, basis vectors, attention head slices) 
--              derived from larger tensors. Enables decomposition and reuse of neural network 
--              components across models and layers.
-- =============================================
CREATE TABLE [dbo].[TensorAtoms]
(
    [TensorAtomId]        BIGINT           NOT NULL IDENTITY(1,1),
    [AtomId]              BIGINT           NOT NULL,
    [ModelId]             INT              NULL,
    [LayerId]             BIGINT           NULL,
    [AtomType]            NVARCHAR(128)    NOT NULL,
    [SpatialSignature]    GEOMETRY         NULL,
    [GeometryFootprint]   GEOMETRY         NULL,
    [Metadata]            NVARCHAR(MAX)    NULL, -- JSON column type applied via CHECK constraint
    [ImportanceScore]     REAL             NULL,
    [CreatedAt]           DATETIME2(7)     NOT NULL CONSTRAINT DF_TensorAtoms_CreatedAt DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT [PK_TensorAtoms] PRIMARY KEY CLUSTERED ([TensorAtomId] ASC),

    CONSTRAINT [FK_TensorAtoms_Atoms] 
        FOREIGN KEY ([AtomId]) 
        REFERENCES [dbo].[Atoms]([AtomId]) 
        ON DELETE CASCADE,

    CONSTRAINT [FK_TensorAtoms_Models] 
        FOREIGN KEY ([ModelId]) 
        REFERENCES [dbo].[Models]([ModelId]) 
        ON DELETE NO ACTION,

    CONSTRAINT [FK_TensorAtoms_ModelLayers] 
        FOREIGN KEY ([LayerId]) 
        REFERENCES [dbo].[ModelLayers]([LayerId]) 
        ON DELETE NO ACTION,

    CONSTRAINT [CK_TensorAtoms_Metadata_IsJson] 
        CHECK ([Metadata] IS NULL OR ISJSON([Metadata]) = 1)
);

-- Composite index for efficient querying by model, layer, and atom type
GO

CREATE NONCLUSTERED INDEX [IX_TensorAtoms_Model_Layer_Type]
    ON [dbo].[TensorAtoms]([ModelId] ASC, [LayerId] ASC, [AtomType] ASC);
GO