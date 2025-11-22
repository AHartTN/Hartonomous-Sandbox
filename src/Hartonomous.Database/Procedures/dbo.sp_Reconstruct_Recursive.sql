-- =============================================
-- sp_Reconstruct_Recursive: Rebuild Text from Atomic Composition
-- GREENFIELD IMPLEMENTATION - No legacy fallback logic
-- =============================================
-- Purpose: Walk the AtomComposition tree to reconstruct original text
-- Strategy: Recursive CTE traverses parent-child relationships
-- Performance: Uses SequenceIndex for correct ordering
-- =============================================
CREATE OR ALTER PROCEDURE [dbo].[sp_Reconstruct_Recursive]
    @AtomId BIGINT,
    @TenantId INT = 0,
    @MaxDepth INT = 10,
    @ReconstructedText NVARCHAR(MAX) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    -- Recursive CTE to walk the composition tree
    ;WITH CompositionTree AS (
        -- Base case: Direct children
        SELECT 
            ac.ComponentAtomId,
            ac.SequenceIndex,
            a.CanonicalText,
            a.Modality,
            1 AS Depth
        FROM dbo.AtomComposition ac
        INNER JOIN dbo.Atom a ON ac.ComponentAtomId = a.AtomId
        WHERE ac.ParentAtomId = @AtomId
          AND a.TenantId = @TenantId

        UNION ALL

        -- Recursive case: Children of children
        SELECT 
            ac.ComponentAtomId,
            ac.SequenceIndex,
            a.CanonicalText,
            a.Modality,
            ct.Depth + 1
        FROM dbo.AtomComposition ac
        INNER JOIN dbo.Atom a ON ac.ComponentAtomId = a.AtomId
        INNER JOIN CompositionTree ct ON ac.ParentAtomId = ct.ComponentAtomId
        WHERE ct.Depth < @MaxDepth
          AND a.TenantId = @TenantId
    )
    SELECT @ReconstructedText = STRING_AGG(
        CAST(CanonicalText AS NVARCHAR(MAX)), 
        ' '
    ) WITHIN GROUP (ORDER BY SequenceIndex)
    FROM CompositionTree
    WHERE Depth = (SELECT MIN(Depth) FROM CompositionTree WHERE CanonicalText IS NOT NULL)
      AND Modality = 'text';

    RETURN 0;
END
GO
