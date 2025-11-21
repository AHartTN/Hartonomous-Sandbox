CREATE OR ALTER PROCEDURE dbo.sp_CompareModelKnowledge
    @ModelAId INT,
    @ModelBId INT
AS
BEGIN
    SET NOCOUNT ON;

    PRINT 'KNOWLEDGE COMPARISON: Model ' + CAST(@ModelAId AS NVARCHAR(20)) + ' vs Model ' + CAST(@ModelBId AS NVARCHAR(20));

    SELECT
        'Model A' AS model_name,
        @ModelAId AS model_id,
        COUNT(*) AS total_tensor_atoms,
        AVG(CAST(ImportanceScore AS FLOAT)) AS avg_importance,
        STDEV(CAST(ImportanceScore AS FLOAT)) AS stdev_importance,
        MIN(ImportanceScore) AS min_importance,
        MAX(ImportanceScore) AS max_importance
    FROM dbo.TensorAtom
    WHERE ModelId = @ModelAId

    UNION ALL

    SELECT
        'Model B' AS model_name,
        @ModelBId AS model_id,
        COUNT(*) AS total_tensor_atoms,
        AVG(CAST(ImportanceScore AS FLOAT)) AS avg_importance,
        STDEV(CAST(ImportanceScore AS FLOAT)) AS stdev_importance,
        MIN(ImportanceScore) AS min_importance,
        MAX(ImportanceScore) AS max_importance
    FROM dbo.TensorAtom
    WHERE ModelId = @ModelBId;

    SELECT
        'Layer Comparison' AS analysis_type,
        COALESCE(a.LayerIdx, b.LayerIdx) AS layer_idx,
        a.LayerType AS model_a_type,
        b.LayerType AS model_b_type,
        a.ParameterCount AS model_a_parameters,
        b.ParameterCount AS model_b_parameters,
        a.TensorShape AS model_a_shape,
        b.TensorShape AS model_b_shape
    FROM dbo.ModelLayer AS a
    FULL OUTER JOIN dbo.ModelLayer AS b
        ON a.LayerIdx = b.LayerIdx
       AND a.ModelId = @ModelAId
       AND b.ModelId = @ModelBId
    WHERE a.ModelId = @ModelAId
       OR b.ModelId = @ModelBId
    ORDER BY COALESCE(a.LayerIdx, b.LayerIdx);

    SELECT
        'Coefficient Coverage' AS analysis_type,
        stats.model_id,
        stats.total_coefficients,
        stats.avg_value,
        stats.max_value,
        stats.min_value
    FROM (
        SELECT
            ta.ModelId AS model_id,
            COUNT(tc.Coefficient) AS total_coefficients,
            AVG(CAST(tc.Coefficient AS FLOAT)) AS avg_value,
            MAX(tc.Coefficient) AS max_value,
            MIN(tc.Coefficient) AS min_value
        FROM dbo.TensorAtom AS ta
        LEFT JOIN dbo.TensorAtomCoefficient AS tc ON tc.TensorAtomId = ta.TensorAtomId
        WHERE ta.ModelId IN (@ModelAId, @ModelBId)
        GROUP BY ta.ModelId
    ) AS stats
    ORDER BY stats.model_id;

    PRINT '✓ Knowledge comparison complete';
END