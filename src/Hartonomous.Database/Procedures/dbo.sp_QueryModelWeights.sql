CREATE PROCEDURE dbo.sp_QueryModelWeights
    @ModelId INT,
    @LayerIdx INT = NULL,
    @atom_type NVARCHAR(128) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        ta.TensorAtomId,
        ml.LayerIdx,
        ml.LayerType,
        ta.AtomType,
        ta.ImportanceScore,
        ta.Metadata AS TensorMetadata,
        a.Modality,
        a.Subtype,
        a.SourceType,
        coeff.ParentLayerId,
        mlParent.LayerIdx AS ParentLayerIdx,
        coeff.TensorRole,
        coeff.Coefficient
    FROM dbo.TensorAtoms AS ta
    INNER JOIN dbo.ModelLayers AS ml ON ml.LayerId = ta.LayerId
    INNER JOIN dbo.Atoms AS a ON a.AtomId = ta.AtomId
    LEFT JOIN dbo.TensorAtomCoefficients AS coeff ON coeff.TensorAtomId = ta.TensorAtomId
    LEFT JOIN dbo.ModelLayers AS mlParent ON mlParent.LayerId = coeff.ParentLayerId
    WHERE ta.ModelId = @ModelId
      AND (@LayerIdx IS NULL OR ml.LayerIdx = @LayerIdx)
      AND (@atom_type IS NULL OR ta.AtomType = @atom_type)
    ORDER BY ml.LayerIdx, ta.ImportanceScore DESC, ta.TensorAtomId, coeff.TensorRole;
END