-- =============================================
-- Ingest Llama4 and Qwen3-Coder Models as Atoms
-- =============================================
-- Stores MODEL FILE PATHS (not the files themselves - they're too large)
-- Models are referenced by path and loaded on-demand for inference
-- =============================================

USE Hartonomous;
GO

SET QUOTED_IDENTIFIER ON;
GO

PRINT 'Registering Llama4 model (62.81 GB)...';

-- Llama4: Store metadata and PATH, not the 67GB file
DECLARE @llama4Path NVARCHAR(500) = 'D:\Models\blobs\sha256-9d507a36062c2845dd3bb3e93364e9abc1607118acd8650727a700f72fb126e5';
DECLARE @llama4Hash VARBINARY(32) = HASHBYTES('SHA2_256', CAST(@llama4Path AS VARBINARY(500)));

IF NOT EXISTS (SELECT 1 FROM dbo.Atoms WHERE ContentHash = @llama4Hash)
BEGIN
    INSERT INTO dbo.Atoms (
        ContentHash,
        Modality,
        Subtype,
        SourceUri,
        SourceType,
        PayloadLocator,  -- Store PATH, not data
        Metadata
    )
    VALUES (
        @llama4Hash,
        'model',
        'llm',
        'ollama://llama4:latest',
        'ollama_blob',
        @llama4Path,
        JSON_OBJECT(
            'model_name': 'llama4',
            'model_version': 'latest',
            'size_bytes': 67436800960,
            'size_gb': 62.81,
            'file_path': @llama4Path,
            'capabilities': JSON_ARRAY('reasoning', 'analysis', 'generation', 'autonomous_improvement'),
            'ollama_digest': 'sha256:9d507a36062c2845dd3bb3e93364e9abc1607118acd8650727a700f72fb126e5',
            'load_on_demand': 1,
            'registered_at': FORMAT(SYSUTCDATETIME(), 'yyyy-MM-ddTHH:mm:ssZ')
        )
    );
    
    PRINT 'SUCCESS: Llama4 model registered';
    PRINT '  Path: ' + @llama4Path;
    PRINT '  Size: 62.81 GB (loaded on-demand)';
END
ELSE
    PRINT 'Llama4 model already registered';
GO

PRINT '';
PRINT 'Registering Qwen3-Coder model (17.28 GB)...';

-- Qwen3-Coder: Store metadata and PATH
DECLARE @qwenPath NVARCHAR(500) = 'D:\Models\blobs\sha256-1194192cf2a187eb02722edcc3f77b11d21f537048ce04b67ccf8ba78863006a';
DECLARE @qwenHash VARBINARY(32) = HASHBYTES('SHA2_256', CAST(@qwenPath AS VARBINARY(500)));

IF NOT EXISTS (SELECT 1 FROM dbo.Atoms WHERE ContentHash = @qwenHash)
BEGIN
    INSERT INTO dbo.Atoms (
        ContentHash,
        Modality,
        Subtype,
        SourceUri,
        SourceType,
        PayloadLocator,  -- Store PATH, not data
        Metadata
    )
    VALUES (
        @qwenHash,
        'model',
        'code_llm',
        'ollama://qwen3-coder:30b',
        'ollama_blob',
        @qwenPath,
        JSON_OBJECT(
            'model_name': 'qwen3-coder',
            'model_version': '30b',
            'size_bytes': 18556688736,
            'size_gb': 17.28,
            'file_path': @qwenPath,
            'capabilities': JSON_ARRAY('code_generation', 'code_analysis', 'debugging', 'autonomous_code_gen'),
            'specialization': 'coding',
            'ollama_digest': 'sha256:1194192cf2a187eb02722edcc3f77b11d21f537048ce04b67ccf8ba78863006a',
            'load_on_demand': 1,
            'registered_at': FORMAT(SYSUTCDATETIME(), 'yyyy-MM-ddTHH:mm:ssZ')
        )
    );
    
    PRINT 'SUCCESS: Qwen3-Coder model registered';
    PRINT '  Path: ' + @qwenPath;
    PRINT '  Size: 17.28 GB (loaded on-demand)';
END
ELSE
    PRINT 'Qwen3-Coder model already registered';
GO

PRINT '';
PRINT '===========================================';
PRINT 'Model Registration Summary';
PRINT '===========================================';

-- Verify registration
SELECT 
    AtomId,
    Modality,
    Subtype,
    SourceUri,
    PayloadLocator AS ModelFilePath,
    CAST(JSON_VALUE(Metadata, '$.size_gb') AS DECIMAL(10,2)) AS SizeGB,
    JSON_VALUE(Metadata, '$.model_name') AS ModelName,
    JSON_VALUE(Metadata, '$.specialization') AS Specialization,
    CreatedAt
FROM dbo.Atoms
WHERE Modality = 'model'
ORDER BY CreatedAt DESC;

PRINT '';
PRINT 'Models registered with on-demand loading.';
PRINT 'Model files remain on disk, loaded into memory only when needed for inference.';
GO
