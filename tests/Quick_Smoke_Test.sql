-- Quick Smoke Test: Verify BackgroundJob table and trigger
-- Run this in SSMS connected to your Hartonomous database

USE Hartonomous;
GO

-- Step 1: Verify BackgroundJob table exists
SELECT COUNT(*) AS BackgroundJobTableExists 
FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = 'BackgroundJob';
-- Expected: 1

-- Step 2: Check current jobs
SELECT TOP 5 
    JobId,
    JobType,
    Status,
    Payload,
    CreatedAtUtc
FROM dbo.BackgroundJob
ORDER BY CreatedAtUtc DESC;

-- Step 3: Manually insert a test atom (will trigger job creation when we test)
DECLARE @TestAtomId BIGINT;

INSERT INTO dbo.Atom (
    TenantId,
    Modality,
    Subtype,
    ContentHash,
    CanonicalText,
    AtomicValue,
    CreatedAt
)
VALUES (
    0,
    'text',
    'test',
    HASHBYTES('SHA2_256', 'This is a test for embedding generation'),
    'This is a test for embedding generation',
    CAST('This is a test' AS VARBINARY(64)),
    GETUTCDATE()
);

SET @TestAtomId = SCOPE_IDENTITY();

SELECT @TestAtomId AS TestAtomId, 'Atom created successfully' AS Status;

-- Step 4: Check if we have the CLR functions deployed
SELECT 
    OBJECT_NAME(object_id) AS FunctionName,
    type_desc
FROM sys.objects
WHERE type IN ('FN', 'FS', 'FT', 'IF', 'TF')
    AND name LIKE '%Embedding%' OR name LIKE '%ProjectTo3D%' OR name LIKE '%Hilbert%'
ORDER BY name;

-- Expected: fn_ComputeEmbedding, fn_ProjectTo3D, clr_ComputeHilbertValue
