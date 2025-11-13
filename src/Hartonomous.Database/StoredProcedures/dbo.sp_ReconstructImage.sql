-- =============================================
-- sp_ReconstructImage: Reconstruct image from atomic pixels
-- Uses spatial query to get pixels and rebuild image data
-- =============================================
CREATE OR ALTER PROCEDURE [dbo].[sp_ReconstructImage]
    @imageAtomId BIGINT,
    @includeMetadata BIT = 0
AS
BEGIN
    SET NOCOUNT ON;

    -- Get image dimensions from metadata
    DECLARE @width INT, @height INT;
    
    SELECT 
        @width = JSON_VALUE(Metadata, '$.width'),
        @height = JSON_VALUE(Metadata, '$.height')
    FROM dbo.Atoms
    WHERE AtomId = @imageAtomId;

    -- Return pixel data ordered by position
    SELECT 
        ac.DimensionX AS X,
        ac.DimensionY AS Y,
        CAST(SUBSTRING(p.RgbaBytes, 1, 1) AS INT) AS R,
        CAST(SUBSTRING(p.RgbaBytes, 2, 1) AS INT) AS G,
        CAST(SUBSTRING(p.RgbaBytes, 3, 1) AS INT) AS B,
        CAST(SUBSTRING(p.RgbaBytes, 4, 1) AS INT) AS A,
        CASE WHEN @includeMetadata = 1 THEN p.PixelHash ELSE NULL END AS PixelHash,
        CASE WHEN @includeMetadata = 1 THEN ac.Metadata ELSE NULL END AS Metadata
    FROM dbo.AtomCompositions ac
    INNER JOIN dbo.Atoms a ON ac.ComponentAtomId = a.AtomId
    INNER JOIN dbo.AtomicPixels p ON p.PixelHash = a.ContentHash
    WHERE ac.SourceAtomId = @imageAtomId
      AND ac.ComponentType = 'pixel'
    ORDER BY ac.DimensionY, ac.DimensionX;  -- Row-major order

    -- Return image metadata
    SELECT 
        AtomId AS ImageAtomId,
        @width AS Width,
        @height AS Height,
        (@width * @height) AS TotalPixels,
        JSON_VALUE(Metadata, '$.format') AS Format,
        JSON_VALUE(Metadata, '$.colorSpace') AS ColorSpace,
        CreatedAt
    FROM dbo.Atoms
    WHERE AtomId = @imageAtomId;
END;
GO
