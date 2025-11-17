CREATE FUNCTION dbo.fn_ComputeSpatialBucket (
    @X FLOAT,
    @Y FLOAT,
    @Z FLOAT
)
RETURNS BIGINT
AS
BEGIN
    -- Locality-sensitive hash for spatial bucketing
    -- Buckets are 0.01 units (1% of normalized space)
    RETURN (
        (CAST(FLOOR(@X * 100) AS BIGINT) * 1000000) +
        (CAST(FLOOR(@Y * 100) AS BIGINT) * 1000) +
        (CAST(FLOOR(@Z * 100) AS BIGINT))
    );
END
