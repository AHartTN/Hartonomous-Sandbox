-- =============================================
-- Table: dbo.TensorAtomCoefficients
-- =============================================
-- Associates a tensor atom with a parent tensor signature via a coefficient.
-- This table was previously managed by EF Core.
-- =============================================

IF OBJECT_ID('dbo.TensorAtomCoefficients', 'U') IS NOT NULL
    DROP TABLE dbo.TensorAtomCoefficients;
GO

CREATE TABLE dbo.TensorAtomCoefficients
(
    TensorAtomCoefficientId BIGINT          IDENTITY(1,1) NOT NULL PRIMARY KEY,
    TensorAtomId            BIGINT          NOT NULL,
    ParentLayerId           BIGINT          NOT NULL,
    TensorRole              NVARCHAR(128)   NULL,
    Coefficient             REAL            NOT NULL,

    CONSTRAINT FK_TensorAtomCoefficients_TensorAtom FOREIGN KEY (TensorAtomId) REFERENCES dbo.TensorAtoms(TensorAtomId) ON DELETE CASCADE,
    CONSTRAINT FK_TensorAtomCoefficients_ModelLayers FOREIGN KEY (ParentLayerId) REFERENCES dbo.ModelLayers(LayerId) ON DELETE CASCADE
);
GO

CREATE INDEX IX_TensorAtomCoefficients_Lookup ON dbo.TensorAtomCoefficients(TensorAtomId, ParentLayerId, TensorRole);
GO

PRINT 'Created table dbo.TensorAtomCoefficients';
GO
