-- =========================================================================================
-- BOLD MOVE: INGESTING "MATTER" INTO THE KERNEL
-- =========================================================================================
-- This script directs the Cognitive Kernel to consume a physical model file (GGUF).
-- It transforms the static file into active, queryable Tensor Atoms.
-- =========================================================================================

SET NOCOUNT ON;
USE [Hartonomous];
GO

PRINT '>>> INITIATING MODEL ATOMIZATION PROTOCOL >>>';

-- 1. REGISTER THE PHYSICAL ARTIFACT
-- We tell the kernel where the "Matter" resides in the physical realm.
-- NOTE: Ensure the SQL Service Account has READ permissions on this path.

DECLARE @ModelName NVARCHAR(256) = 'TinyLlama-1.1B-Chat-v1.0.Q4_K_M.gguf'; -- Check what is available
DECLARE @PhysicalPath NVARCHAR(MAX) = 'D:\Models\Something?\TinyLlama-1.1B-Chat-v1.0.Q4_K_M.gguf'; -- Adjust path as needed

PRINT '   -> Registering Model: ' + @ModelName;

MERGE dbo.Models AS target
USING (VALUES 
    (@ModelName, 'LLM', 'LocalFile', @PhysicalPath, '{"quantization": "Q4_K_M", "parameterCount": "1.1B", "architecture": "llama"}')
) AS source (Name, Type, Prov, Path, Meta)
ON target.ModelName = source.Name
WHEN MATCHED THEN 
    UPDATE SET SourceUri = source.Path, MetadataJson = source.Meta
WHEN NOT MATCHED THEN 
    INSERT (ModelName, ModelType, Provider, SourceUri, IsActive, MetadataJson) 
    VALUES (source.Name, source.Type, source.Prov, source.Path, 1, source.Meta);

DECLARE @ModelId INT = (SELECT ModelId FROM dbo.Models WHERE ModelName = @ModelName);

-- 2. CONFIGURE THE INGESTION JOB
-- We don't just "read" it; we define *how* to break it down.
-- SVD Compression allows us to deduplicate common geometric structures across layers.

PRINT '   -> Configuring Ingestion Job (Strategy: SVD Decomposition)...';

DECLARE @JobId UNIQUEIDENTIFIER = NEWID();

INSERT INTO dbo.IngestionJobs (IngestionJobId, TenantId, JobType, Status, ConfigurationJson, CreatedAt)
VALUES (
    @JobId,
    1, -- Dev Tenant
    'ModelAtomization',
    'Pending',
    '{
        "targetModelId": ' + CAST(@ModelId AS NVARCHAR(10)) + ',
        "strategy": "SVD",
        "svdRank": 64,           -- Compress layers to Rank 64
        "storeRaw": false,       -- Do NOT store the raw heavy blobs, only the decomposed atoms
        "spatialProjection": true -- Map these weights into the 3D Hilbert Index
    }',
    SYSDATETIME()
);

-- 3. EXECUTE THE ATOMIZATION (Simulated or Real)
-- In a production environment, the Service Broker picks up the job.
-- Here, we force the execution via the Stored Procedure wrapper.

PRINT '   -> Triggering Nucleation (Breaking Model into Atoms)...';

BEGIN TRY
    -- This calls the CLR wrapper which opens the file stream
    EXEC dbo.sp_IngestModel 
        @ModelId = @ModelId,
        @FilePath = @PhysicalPath,
        @ComputeHilbert = 1; -- Automatically index the weights in 3D space
        
    PRINT '   [SUCCESS] Model ingestion command issued.';
END TRY
BEGIN CATCH
    PRINT '   [NOTE] Actual ingestion skipped (File might not exist in this context).';
    PRINT '          Error: ' + ERROR_MESSAGE();
    
    -- 4. SIMULATION (If file missing)
    -- We will manually inject a "Phantom Layer" to prove the schema works 
    -- even if you don't have the 4GB file right now.
    PRINT '   -> Simulating successful ingestion of Layer 1 (Attention Query Weights)...';
    
    DECLARE @PhantomHash BINARY(32) = HASHBYTES('SHA2_256', 'blk.0.attn_q.weight');
    
    -- The Atom (The Identity)
    INSERT INTO dbo.Atom (TenantId, Modality, ContentHash, ContentType, AtomicValue, ReferenceCount)
    VALUES (1, 'tensor', @PhantomHash, 'application/octet-stream', 0xCAFEBABE, 1);
    
    DECLARE @AtomId BIGINT = SCOPE_IDENTITY();
    
    -- The Tensor Metadata (The Shape)
    INSERT INTO dbo.TensorAtoms (AtomId, ModelId, LayerName, Shape, DataType, Sparsity)
    VALUES (@AtomId, @ModelId, 'blk.0.attn_q.weight', '[2048, 2048]', 'F32', 0.1);
    
    -- The Geometry (The Place in Space)
    -- We map this specific attention head to the "Logic" region of our coordinate system
    INSERT INTO dbo.AtomEmbedding (AtomId, ModelId, EmbeddingVector, Dimension, SpatialKey, HilbertValue)
    VALUES (
        @AtomId, 
        @ModelId, 
        0x00, -- Dummy vector
        4096,
        geometry::STPointFromText('POINT(5 5 5)', 0), -- Center of "Reasoning" cluster
        dbo.clr_ComputeHilbertValue(geometry::STPointFromText('POINT(5 5 5)', 0), 21)
    );

    PRINT '   [SIMULATION] Phantom Layer "blk.0.attn_q.weight" created successfully.';
END CATCH

-- 5. VERIFY THE RESULTS
-- Query the model not as a file, but as a dataset.

PRINT '--------------------------------------------------';
PRINT '>>> MODEL AUTOPSY >>>';

SELECT 
    m.ModelName,
    ta.LayerName,
    ta.Shape,
    a.ContentHash,
    ae.SpatialKey.ToString() as [Geometric Location]
FROM dbo.Models m
JOIN dbo.TensorAtoms ta ON m.ModelId = ta.ModelId
JOIN dbo.Atom a ON ta.AtomId = a.AtomId
LEFT JOIN dbo.AtomEmbedding ae ON a.AtomId = ae.AtomId
WHERE m.ModelId = @ModelId;

PRINT '<<< PROCESS COMPLETE <<<';
GO