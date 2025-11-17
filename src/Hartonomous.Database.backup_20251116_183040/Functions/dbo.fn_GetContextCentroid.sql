CREATE FUNCTION dbo.fn_GetContextCentroid(@atom_ids NVARCHAR(MAX))
RETURNS TABLE
AS
RETURN
(
    SELECT
        dbo.fn_CreateSpatialPoint(
            AVG(ae.SpatialKey.STX),
            AVG(ae.SpatialKey.STY),
            AVG(CAST(COALESCE(ae.SpatialKey.Z, 0) AS FLOAT))
        ) AS ContextCentroid,
        COUNT(*) AS AtomCount
    FROM dbo.AtomEmbeddings ae
    WHERE ae.AtomId IN (SELECT CAST(value AS BIGINT) FROM STRING_SPLIT(@atom_ids, ','))
      AND ae.SpatialKey IS NOT NULL
);