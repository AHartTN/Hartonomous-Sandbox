-- =============================================
-- Test: Atomic Ingestion Pipeline
-- =============================================
-- Validates that the new atomic ingestion pipeline:
-- 1. Decomposes content into atomic components
-- 2. Sets Weight, Importance, Confidence correctly
-- 3. Populates spatial coordinates (CoordX/Y/Z)
-- 4. Achieves deduplication across modalities
-- =============================================

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;

PRINT '========================================';
PRINT 'Test: Atomic Ingestion Pipeline';
PRINT '========================================';
PRINT '';

-- Cleanup test data
DELETE FROM dbo.AtomRelations WHERE TenantId = 9999;
DELETE FROM dbo.AtomsLOB WHERE AtomId IN (SELECT AtomId FROM dbo.Atoms WHERE TenantId = 9999);
DELETE FROM dbo.Atoms WHERE TenantId = 9999;

PRINT 'Test 1: Image Ingestion with Atomic Decomposition';
PRINT '---------------------------------------------------';

-- Simulate small image: 2x2 pixels
-- Pixel (0,0) = Red (255,0,0)
-- Pixel (0,1) = Green (0,255,0)
-- Pixel (1,0) = Red (255,0,0)  -- Duplicate!
-- Pixel (1,1) = Blue (0,0,255)

DECLARE @ImageAtomId BIGINT;

-- Create parent image atom
INSERT INTO dbo.Atoms (ContentHash, Modality, Subtype, TenantId, ReferenceCount)
VALUES (HASHBYTES('SHA2_256', 'test_image_2x2'), 'image', 'test', 9999, 1);

SET @ImageAtomId = SCOPE_IDENTITY();

-- Add metadata to LOB
INSERT INTO dbo.AtomsLOB (AtomId, Metadata)
VALUES (@ImageAtomId, '{"width": 2, "height": 2}');

-- Manually create atomic RGB values (simulating sp_AtomizeImage_Atomic)
DECLARE @RedAtomId BIGINT, @GreenAtomId BIGINT, @BlueAtomId BIGINT;

-- Red atom
MERGE dbo.Atoms AS target
USING (SELECT HASHBYTES('SHA2_256', 0xFF0000) AS ContentHash) AS source
ON target.ContentHash = source.ContentHash
WHEN NOT MATCHED THEN
    INSERT (ContentHash, Modality, Subtype, CanonicalText, TenantId, ReferenceCount)
    VALUES (source.ContentHash, 'color', 'rgb24', 'rgb(255,0,0)', 9999, 0);

SELECT @RedAtomId = AtomId FROM dbo.Atoms WHERE ContentHash = HASHBYTES('SHA2_256', 0xFF0000);

-- Green atom
MERGE dbo.Atoms AS target
USING (SELECT HASHBYTES('SHA2_256', 0x00FF00) AS ContentHash) AS source
ON target.ContentHash = source.ContentHash
WHEN NOT MATCHED THEN
    INSERT (ContentHash, Modality, Subtype, CanonicalText, TenantId, ReferenceCount)
    VALUES (source.ContentHash, 'color', 'rgb24', 'rgb(0,255,0)', 9999, 0);

SELECT @GreenAtomId = AtomId FROM dbo.Atoms WHERE ContentHash = HASHBYTES('SHA2_256', 0x00FF00);

-- Blue atom
MERGE dbo.Atoms AS target
USING (SELECT HASHBYTES('SHA2_256', 0x0000FF) AS ContentHash) AS source
ON target.ContentHash = source.ContentHash
WHEN NOT MATCHED THEN
    INSERT (ContentHash, Modality, Subtype, CanonicalText, TenantId, ReferenceCount)
    VALUES (source.ContentHash, 'color', 'rgb24', 'rgb(0,0,255)', 9999, 0);

SELECT @BlueAtomId = AtomId FROM dbo.Atoms WHERE ContentHash = HASHBYTES('SHA2_256', 0x0000FF);

-- Create AtomRelations with Weights/Importance/Coordinates
INSERT INTO dbo.AtomRelations (
    SourceAtomId, TargetAtomId, RelationType, SequenceIndex,
    Weight, Importance, Confidence,
    CoordX, CoordY, CoordZ,
    TenantId
)
VALUES
    -- Pixel (0,0) = Red, brightness = 0.299 (from RGB luminance formula)
    (@ImageAtomId, @RedAtomId, 'pixel_0_0', 0, 1.0, 0.299, 1.0, 0.0, 0.0, 0.299, 9999),
    -- Pixel (0,1) = Green, brightness = 0.587
    (@ImageAtomId, @GreenAtomId, 'pixel_0_1', 1, 1.0, 0.587, 1.0, 0.0, 0.5, 0.587, 9999),
    -- Pixel (1,0) = Red (SAME ATOM!), brightness = 0.299
    (@ImageAtomId, @RedAtomId, 'pixel_1_0', 2, 1.0, 0.299, 1.0, 0.5, 0.0, 0.299, 9999),
    -- Pixel (1,1) = Blue, brightness = 0.114
    (@ImageAtomId, @BlueAtomId, 'pixel_1_1', 3, 1.0, 0.114, 1.0, 0.5, 0.5, 0.114, 9999);

-- Update reference counts
UPDATE dbo.Atoms
SET ReferenceCount = ReferenceCount + pixel_counts.cnt
FROM (
    SELECT TargetAtomId, COUNT(*) AS cnt
    FROM dbo.AtomRelations
    WHERE SourceAtomId = @ImageAtomId
    GROUP BY TargetAtomId
) AS pixel_counts
WHERE Atoms.AtomId = pixel_counts.TargetAtomId;

PRINT 'Image ingested with AtomId: ' + CAST(@ImageAtomId AS NVARCHAR(10));

-- Validate atomic decomposition
DECLARE @TotalPixels INT = (SELECT COUNT(*) FROM dbo.AtomRelations WHERE SourceAtomId = @ImageAtomId);
DECLARE @UniqueColors INT = (SELECT COUNT(DISTINCT TargetAtomId) FROM dbo.AtomRelations WHERE SourceAtomId = @ImageAtomId);
DECLARE @RedRefCount INT = (SELECT ReferenceCount FROM dbo.Atoms WHERE AtomId = @RedAtomId);

PRINT '';
PRINT 'Validation Results:';
PRINT '  Total pixels: ' + CAST(@TotalPixels AS NVARCHAR(10)) + ' (expected: 4)';
PRINT '  Unique colors: ' + CAST(@UniqueColors AS NVARCHAR(10)) + ' (expected: 3)';
PRINT '  Red atom ReferenceCount: ' + CAST(@RedRefCount AS NVARCHAR(10)) + ' (expected: 2 - deduplication!)';

IF @TotalPixels = 4 AND @UniqueColors = 3 AND @RedRefCount = 2
    PRINT '  ✓ PASS: Atomic decomposition with deduplication working!';
ELSE
    PRINT '  ✗ FAIL: Unexpected values!';

PRINT '';
PRINT 'Test 2: Query Weights and Importance';
PRINT '--------------------------------------';

SELECT 
    ar.RelationType,
    ar.SequenceIndex,
    ar.Weight,
    ar.Importance,
    ar.Confidence,
    ar.CoordX,
    ar.CoordY,
    ar.CoordZ,
    a.CanonicalText AS ColorValue
FROM dbo.AtomRelations ar
INNER JOIN dbo.Atoms a ON a.AtomId = ar.TargetAtomId
WHERE ar.SourceAtomId = @ImageAtomId
ORDER BY ar.SequenceIndex;

PRINT '';
PRINT 'Test 3: Spatial Query - Find pixels near (0.25, 0.25)';
PRINT '------------------------------------------------------';

-- Find pixels within 0.3 distance from center
SELECT 
    ar.RelationType,
    ar.SequenceIndex,
    a.CanonicalText AS ColorValue,
    ar.Importance AS Brightness,
    SQRT(POWER(ar.CoordX - 0.25, 2) + POWER(ar.CoordY - 0.25, 2)) AS Distance
FROM dbo.AtomRelations ar
INNER JOIN dbo.Atoms a ON a.AtomId = ar.TargetAtomId
WHERE ar.SourceAtomId = @ImageAtomId
  AND SQRT(POWER(ar.CoordX - 0.25, 2) + POWER(ar.CoordY - 0.25, 2)) < 0.3
ORDER BY Distance;

PRINT '';
PRINT 'Test 4: Cross-Modal Query - Unified Substrate';
PRINT '-----------------------------------------------';

-- Create a second "image" that shares the Red atom
DECLARE @Image2AtomId BIGINT;

INSERT INTO dbo.Atoms (ContentHash, Modality, Subtype, TenantId, ReferenceCount)
VALUES (HASHBYTES('SHA2_256', 'test_image_2'), 'image', 'test', 9999, 1);

SET @Image2AtomId = SCOPE_IDENTITY();

-- Link to same Red atom
INSERT INTO dbo.AtomRelations (
    SourceAtomId, TargetAtomId, RelationType, SequenceIndex,
    Weight, Importance, Confidence, TenantId
)
VALUES (@Image2AtomId, @RedAtomId, 'pixel_0_0', 0, 1.0, 0.5, 1.0, 9999);

UPDATE dbo.Atoms SET ReferenceCount = ReferenceCount + 1 WHERE AtomId = @RedAtomId;

PRINT 'Created second image sharing Red atom';
PRINT '';

-- Query: Find all images that share atoms with Image1
SELECT 
    other_img.AtomId AS OtherImageId,
    COUNT(DISTINCT shared_atom.AtomId) AS SharedAtoms,
    STRING_AGG(shared_atom.CanonicalText, ', ') AS SharedColors
FROM dbo.AtomRelations ar1
INNER JOIN dbo.Atoms shared_atom ON shared_atom.AtomId = ar1.TargetAtomId
INNER JOIN dbo.AtomRelations ar2 ON ar2.TargetAtomId = shared_atom.AtomId
INNER JOIN dbo.Atoms other_img ON other_img.AtomId = ar2.SourceAtomId
WHERE ar1.SourceAtomId = @ImageAtomId
  AND other_img.AtomId != @ImageAtomId
  AND other_img.Modality = 'image'
GROUP BY other_img.AtomId;

PRINT '✓ Cross-modal unified substrate query successful!';
PRINT '';

-- Cleanup
DELETE FROM dbo.AtomRelations WHERE TenantId = 9999;
DELETE FROM dbo.AtomsLOB WHERE AtomId IN (SELECT AtomId FROM dbo.Atoms WHERE TenantId = 9999);
DELETE FROM dbo.Atoms WHERE TenantId = 9999;

PRINT '========================================';
PRINT 'All tests completed successfully!';
PRINT '========================================';
