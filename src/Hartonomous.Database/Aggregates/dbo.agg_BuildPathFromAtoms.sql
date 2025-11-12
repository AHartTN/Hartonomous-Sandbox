CREATE AGGREGATE [dbo].[agg_BuildPathFromAtoms]
    (@atomId BIGINT)
RETURNS NVARCHAR (MAX)
EXTERNAL NAME [SqlClrFunctions].[PathBuilderAggregate];