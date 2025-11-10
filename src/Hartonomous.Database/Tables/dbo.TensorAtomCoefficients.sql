-- =============================================
-- Table: dbo.TensorAtomCoefficients
-- Description: Associates tensor atoms with parent tensor signatures (layer, tensor role) via coefficients.
--              Enables decomposition of large tensors into reusable tensor atoms with weighted contributions.
-- =============================================
CREATE TABLE [dbo].[TensorAtomCoefficients]
(
    [TensorAtomCoefficientId]  BIGINT           NOT NULL IDENTITY(1,1),
    [TensorAtomId]             BIGINT           NOT NULL,
    [ParentLayerId]            BIGINT           NOT NULL,
    [TensorRole]               NVARCHAR(128)    NULL,
    [Coefficient]              REAL             NOT NULL,

    CONSTRAINT [PK_TensorAtomCoefficients] PRIMARY KEY CLUSTERED ([TensorAtomCoefficientId] ASC),

    CONSTRAINT [FK_TensorAtomCoefficients_TensorAtoms] 
        FOREIGN KEY ([TensorAtomId]) 
        REFERENCES [dbo].[TensorAtoms]([TensorAtomId]) 
        ON DELETE CASCADE,

    CONSTRAINT [FK_TensorAtomCoefficients_ModelLayers] 
        FOREIGN KEY ([ParentLayerId]) 
        REFERENCES [dbo].[ModelLayers]([LayerId]) 
        ON DELETE CASCADE
);

-- Composite index for efficient lookup by tensor atom, parent layer, and role
GO

CREATE NONCLUSTERED INDEX [IX_TensorAtomCoefficients_Lookup]
    ON [dbo].[TensorAtomCoefficients]([TensorAtomId] ASC, [ParentLayerId] ASC, [TensorRole] ASC);
GO