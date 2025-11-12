CREATE AGGREGATE dbo.agg_BuildPathFromAtoms(
    @atomId BIGINT,
    @timestamp DATETIME
)
RETURNS GEOMETRY
EXTERNAL NAME SqlClrFunctions.[SqlClrFunctions.BuildPathFromAtoms];