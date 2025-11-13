-- =============================================
-- sp_ReconstructText: Reconstruct text from atomic characters/tokens
-- Uses spatial query to get characters in order
-- =============================================
CREATE OR ALTER PROCEDURE [dbo].[sp_ReconstructText]
    @textAtomId BIGINT,
    @startPosition INT = 0,
    @length INT = NULL  -- NULL = entire document
AS
BEGIN
    SET NOCOUNT ON;

    -- Get document metadata
    DECLARE @totalLength INT;
    SELECT @totalLength = JSON_VALUE(Metadata, '$.length')
    FROM dbo.Atoms
    WHERE AtomId = @textAtomId;

    -- Default to entire document if length not specified
    IF @length IS NULL
        SET @length = @totalLength - @startPosition;

    -- Return characters in order
    SELECT 
        ac.DimensionX AS Position,
        a.CanonicalText AS Character,
        STRING_AGG(a.CanonicalText, '') WITHIN GROUP (ORDER BY ac.DimensionX) 
            OVER (ORDER BY ac.DimensionX ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW) AS CumulativeText
    FROM dbo.AtomCompositions ac
    INNER JOIN dbo.Atoms a ON ac.ComponentAtomId = a.AtomId
    WHERE ac.SourceAtomId = @textAtomId
      AND ac.ComponentType = 'text-token'
      AND ac.DimensionX >= @startPosition
      AND ac.DimensionX < (@startPosition + @length)
    ORDER BY ac.DimensionX;

    -- Return document metadata
    SELECT 
        AtomId AS TextAtomId,
        @totalLength AS TotalLength,
        JSON_VALUE(Metadata, '$.encoding') AS Encoding,
        JSON_VALUE(Metadata, '$.language') AS Language,
        CreatedAt
    FROM dbo.Atoms
    WHERE AtomId = @textAtomId;
END;
GO
