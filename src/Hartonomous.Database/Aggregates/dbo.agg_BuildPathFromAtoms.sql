IF OBJECT_ID('dbo.agg_BuildPathFromAtoms', 'AF') IS NOT NULL DROP AGGREGATE dbo.agg_BuildPathFromAtoms;
GO
CREATE AGGREGATE dbo.agg_BuildPathFromAtoms(
    @atomId BIGINT,
    @timestamp DATETIME
)
RETURNS GEOMETRY
EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.BuildPathFromAtoms];
GO