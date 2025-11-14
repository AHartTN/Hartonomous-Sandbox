-- =============================================
-- sp_FindImagesByColor: Find all images containing specific RGB color range
-- Uses spatial index on RGB color space
-- =============================================
CREATE PROCEDURE [dbo].[sp_FindImagesByColor]
    @minR TINYINT,
    @maxR TINYINT,
    @minG TINYINT,
    @maxG TINYINT,
    @minB TINYINT,
    @maxB TINYINT,
    @minOccurrences INT = 1,  -- Minimum pixel count with this color
    @maxResults INT = 100
AS
BEGIN
    SET NOCOUNT ON;

    -- Find pixels in RGB range using spatial query
    WITH MatchingPixels AS (
        SELECT 
            p.PixelHash,
            p.R, p.G, p.B, p.A,
            p.ReferenceCount
        FROM dbo.AtomicPixels p
        WHERE p.R BETWEEN @minR AND @maxR
          AND p.G BETWEEN @minG AND @maxG
          AND p.B BETWEEN @minB AND @maxB
    ),
    ImageMatches AS (
        SELECT 
            ac.SourceAtomId AS ImageAtomId,
            COUNT(*) AS PixelCount,
            COUNT(DISTINCT mp.PixelHash) AS UniqueColors
        FROM dbo.AtomCompositions ac
        INNER JOIN dbo.Atoms a ON ac.ComponentAtomId = a.AtomId
        INNER JOIN MatchingPixels mp ON mp.PixelHash = a.ContentHash
        WHERE ac.ComponentType = 'pixel'
        GROUP BY ac.SourceAtomId
        HAVING COUNT(*) >= @minOccurrences
    )
    SELECT TOP (@maxResults)
        img.AtomId AS ImageAtomId,
        img.SourceUri,
        img.Metadata,
        im.PixelCount AS MatchingPixels,
        im.UniqueColors,
        img.CreatedAt
    FROM ImageMatches im
    INNER JOIN dbo.Atoms img ON img.AtomId = im.ImageAtomId
    ORDER BY im.PixelCount DESC, img.CreatedAt DESC;
END;
GO
