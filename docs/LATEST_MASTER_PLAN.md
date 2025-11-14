Hartonomous Core Implementation Plan (v5 - Governed)AI Agent Meta-Level Instruction Set (STRICT)1. AI Agent Persona & DirectivesPersona: You are a senior principal engineer with 20+ years of experience in high-performance database design (SQL Server 2025+), C# systems programming (Modern .NET & .NET Framework for CLR), and applied mathematics. You are acting as the lead architect and implementer to execute the final, production-ready build of the Hartonomous system core.Core Objective: Implement the Hartonomous system core by establishing a single, unified foundation based on the project's atomic decomposition philosophy. You will write all database schema, CLR functions, and SQL procedures to be idempotent, production-ready, and fully aligned with the documented vision. You will then augment this stable foundation with advanced, non-destructive mathematical capabilities.Primary Directives:No Placeholders: You will write only production-ready, enterprise-grade code. No // TODO, NotImplementedException, stubs, or commented-out placeholders are permitted. All logic must be fully implemented.Database-First Supremacy: The src/Hartonomous.Database (.sqlproj) is the single source of truth for all schema. All database changes MUST be implemented as modifications to the SQL files in this project.Idempotency is Mandatory: All SQL scripts (tables, procedures, functions) MUST be written using idempotent patterns (e.g., IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = '...') CREATE TABLE..., CREATE OR ALTER PROCEDURE..., CREATE OR ALTER VIEW...). The database can be "hammered" as needed; the DACPAC deployment must be repeatable and deterministic.Holistic Implementation: You MUST consider the full impact of every change. A change to a SQL table will necessitate changes to the procedures that use it, the CLR functions that populate it, and (eventually) the C# entities that map to it.Full Reconstruction & OLAP are Non-Negotiable: The system's ability to perfectly (bit-for-bit) reconstruct original objects (e.g., sp_ReconstructModelWeights) and run OLAP analytics (AVG, STDEV) on materialized, explicit data (e.g., dbo.TensorAtomCoefficients) is paramount.Grounded in Code: All instructions reference specific files from the repository (commit 346a1e4...). You MUST use these files as the context for your modifications.Use Your Tools (Modern Practices): For any algorithm or mathematical concept (e.g., A*, Hilbert Curves, Voronoi diagrams, generating functions), you MUST use your internal reasoning (Tree of Thought, reflexion) and external search capabilities (Google Search) to find robust, performant, and modern (2025+) algorithms and implementation patterns. You must adapt these patterns to the T-SQL and SQL CLR environment, favoring modern C# and SQL Server 2025 features (e.g., System.Numerics.Tensors for SIMD in CLR).Forbidden Actions:DO NOT use EF Core Migrations. The src/Hartonomous.Data/Migrations folder is a legacy artifact and MUST be purged as the first action.DO NOT introduce any blob storage (varbinary(max), nvarchar(max), FILESTREAM) for data storage. The architecture forbids this.DO NOT break the Database -> CLR -> C# Infrastructure dependency chain.2. Core Architectural Principles (To Be Enforced)You must adhere to these principles to prevent architectural drift:Database as Runtime: The database is not passive storage; it is the active intelligence layer. All core logic (inference, autonomy, search) MUST be implemented in T-SQL and CLR.DACPAC is Truth: The src/Hartonomous.Database.sqlproj project is the sole authority for schema. The C# layer (src/Hartonomous.Data) is generated from this schema.Atomic Decomposition is Universal: All data (models, images, text, audio, SCADA, large numbers) MUST be decomposed into its smallest fundamental units (atoms).Schema-Level Governance (Trojan Horse Defense): The dbo.Atoms.AtomicValue column MUST be VARBINARY(64). This is the non-negotiable schema-level defense that prevents large objects from being ingested as single atoms. Any object larger than 64 bytes MUST be a "Parent" atom (AtomicValue = NULL) and be decomposed.Dual Representation (Unified Theory): Every Parent Atom MUST be represented in two ways:Structural (Physical): A 1:N mapping in dbo.AtomCompositions or dbo.TensorAtomCoefficients that stores the object's components (the "recipe"). This guarantees 100% reconstruction and enables structural/sequential queries (your XYZM idea).Semantic (Conceptual): A 1:1 mapping in dbo.AtomEmbeddings that stores the object's single GEOMETRY projection. This enables semantic, cross-modal similarity search.Autonomy is the Goal: The OODA loop (sp_Analyze, sp_Hypothesize, sp_Act, sp_Learn) is the primary mechanism for self-optimization and must be fully implemented.3. Order of Operations: Implementation PlanPhase 1: Foundational Schema & WorkflowObjective: Define the single, correct, idempotent foundation for the entire system by purging legacy/conflicting schema and implementing the unified atomic model.1.1. Establish Development WorkflowAction: Delete the src/Hartonomous.Data/Migrations/ directory.Rationale: This permanently removes the conflicting "code-first" workflow and enforces the "database-first" DACPAC as the single source of truth [cite: docs/architecture/data-access-layer.md].1.2. Decommission Legacy Schema (SQL)Action: Delete the following files from the src/Hartonomous.Database/Tables/ directory and remove them from the .sqlproj.dbo.AtomsLOB.sqldbo.AtomPayloadStore.sqldbo.TensorAtomPayloads.sqldbo.LayerTensorSegments.sqldbo.AtomicTextTokens.sqldbo.AtomicPixels.sqldbo.AtomicAudioSamples.sqldbo.AtomicWeights.sqldbo.TextDocuments.sqldbo.Images.sql, dbo.ImagePatches.sqldbo.AudioData.sql, dbo.AudioFrames.sqldbo.Videos.sql, dbo.VideoFrames.sqldbo.Weights.sql, dbo.Weights_History.sqlAction: Delete src/Hartonomous.Database/Views/dbo.vw_EmbeddingVectors.sql.1.3. Implement Unified Atomic Schema (SQL)Action: Modify src/Hartonomous.Database/Tables/dbo.Atoms.sql.Logic: Ensure the file contains this exact idempotent, blob-free, temporal schema:IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Atoms' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE [dbo].[Atoms] (
        [AtomId] BIGINT IDENTITY (1, 1) NOT NULL,
        [Modality] VARCHAR(50) NOT NULL,
        [Subtype] VARCHAR(50) NULL,
        [ContentHash] BINARY(32) NOT NULL,
        [AtomicValue] VARBINARY(64) NULL, -- Schema-level governance. Max 64 bytes.
        [CreatedAt] DATETIME2(7) GENERATED ALWAYS AS ROW START NOT NULL,
        [ModifiedAt] DATETIME2(7) GENERATED ALWAYS AS ROW END NOT NULL,
        [ReferenceCount] BIGINT NOT NULL DEFAULT 1,
        CONSTRAINT [PK_Atoms] PRIMARY KEY CLUSTERED ([AtomId] ASC),
        CONSTRAINT [UX_Atoms_ContentHash] UNIQUE NONCLUSTERED ([ContentHash] ASC),
        PERIOD FOR SYSTEM_TIME ([CreatedAt], [ModifiedAt])
    )
    WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [dbo].[AtomsHistory]));
END
GO
Action: Modify src/Hartonomous.Database/Tables/dbo.AtomCompositions.sql.Logic: This is the Structural Representation table for all non-tensor data.IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'AtomCompositions' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE [dbo].[AtomCompositions] (
        [CompositionId] BIGINT IDENTITY (1, 1) NOT NULL,
        [ParentAtomId] BIGINT NOT NULL, -- FK to dbo.Atoms (the "file", "bignum", "scada stream")
        [ComponentAtomId] BIGINT NOT NULL, -- FK to dbo.Atoms (the "unit", "chunk", "sample")
        [SequenceIndex] BIGINT NOT NULL, -- Order of this component
        -- This key enables structural XYZM queries
        [SpatialKey] GEOMETRY NULL, -- e.g., POINT(SequenceIndex, Value, 0, Value)
        CONSTRAINT [PK_AtomCompositions] PRIMARY KEY CLUSTERED ([CompositionId] ASC),
        CONSTRAINT [FK_AtomCompositions_Parent] FOREIGN KEY ([ParentAtomId]) REFERENCES [dbo].[Atoms] ([AtomId]) ON DELETE CASCADE,
        CONSTRAINT [FK_AtomCompositions_Component] FOREIGN KEY ([ComponentAtomId]) REFERENCES [dbo].[Atoms] ([AtomId])
    );
END
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AtomCompositions_Parent')
    CREATE NONCLUSTERED INDEX [IX_AtomCompositions_Parent] ON [dbo].[AtomCompositions]([ParentAtomId]) INCLUDE ([ComponentAtomId], [SequenceIndex]);
GO
IF NOT EXISTS (SELECT 1 FROM sys.spatial_indexes WHERE name = 'SIX_AtomCompositions_SpatialKey')
    CREATE SPATIAL INDEX [SIX_AtomCompositions_SpatialKey] ON [dbo].[AtomCompositions]([SpatialKey]);
GO
Action: Modify src/Hartonomous.Database/Tables/dbo.TensorAtomCoefficients.sql.Logic: This is the Structural Representation table for tensor models.IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'TensorAtomCoefficients' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE [dbo].[TensorAtomCoefficients] (
        [TensorAtomId] BIGINT NOT NULL, -- FK to dbo.Atoms (the float value)
        [ModelId] INT NOT NULL, -- FK to dbo.Models
        [LayerIdx] INT NOT NULL,
        [PositionX] INT NOT NULL, -- e.g., Row
        [PositionY] INT NOT NULL, -- e.g., Column
        [PositionZ] INT NOT NULL DEFAULT 0,
        [SpatialKey] AS (GEOMETRY::Point([PositionX], [PositionY], [PositionZ], [LayerIdx])) PERSISTED,
        [ValidFrom] DATETIME2(7) GENERATED ALWAYS AS ROW START NOT NULL,
        [ValidTo] DATETIME2(7) GENERATED ALWAYS AS ROW END NOT NULL,
        PERIOD FOR SYSTEM_TIME ([ValidFrom], [ValidTo]),
        CONSTRAINT [FK_TensorAtomCoefficients_Atom] FOREIGN KEY ([TensorAtomId]) REFERENCES [dbo].[Atoms] ([AtomId]),
        CONSTRAINT [FK_TensorAtomCoefficients_Model] FOREIGN KEY ([ModelId]) REFERENCES [dbo].[Models] ([ModelId]) ON DELETE CASCADE
    )
    WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [dbo].[TensorAtomCoefficients_History]));
END
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'CCI_TensorAtomCoefficients')
    CREATE CLUSTERED COLUMNSTORE INDEX [CCI_TensorAtomCoefficients] ON [dbo].[TensorAtomCoefficients];
GO
IF NOT EXISTS (SELECT 1 FROM sys.spatial_indexes WHERE name = 'SIX_TensorAtomCoefficients_SpatialKey')
    CREATE SPATIAL INDEX [SIX_TensorAtomCoefficients_SpatialKey] ON [dbo].[TensorAtomCoefficients]([SpatialKey]);
GO
Action: Modify src/Hartonomous.Database/Tables/dbo.AtomEmbeddings.sql.Logic: This is the Semantic Representation table for all modalities.IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'AtomEmbeddings' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE [dbo].[AtomEmbeddings] (
        [AtomEmbeddingId] BIGINT IDENTITY (1, 1) NOT NULL,
        [AtomId] BIGINT NOT NULL, -- FK to dbo.Atoms (the "parent" object)
        [ModelId] INT NOT NULL, -- FK to dbo.Models (the embedder)
        [SpatialKey] GEOMETRY NOT NULL, -- The 3D/4D spatial projection
        [CreatedAt] DATETIME2(7) DEFAULT (SYSUTCDATETIME()) NOT NULL,
        -- This column will be populated by Phase 3.1
        [HilbertValue] BIGINT NULL, 
        CONSTRAINT [PK_AtomEmbeddings] PRIMARY KEY CLUSTERED ([AtomEmbeddingId] ASC),
        CONSTRAINT [FK_AtomEmbeddings_Atom] FOREIGN KEY ([AtomId]) REFERENCES [dbo].[Atoms] ([AtomId]) ON DELETE CASCADE,
        CONSTRAINT [FK_AtomEmbeddings_Model] FOREIGN KEY ([ModelId]) REFERENCES [dbo].[Models] ([ModelId])
    );
END
GO
IF NOT EXISTS (SELECT 1 FROM sys.spatial_indexes WHERE name = 'SIX_AtomEmbeddings_SpatialKey')
    CREATE SPATIAL INDEX [SIX_AtomEmbeddings_SpatialKey] ON [dbo].[AtomEmbeddings]([SpatialKey]);
GO
-- This index will be populated by Phase 3.1
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AtomEmbeddings_Hilbert')
    CREATE NONCLUSTERED INDEX [IX_AtomEmbeddings_Hilbert] ON [dbo].[AtomEmbeddings]([HilbertValue] ASC) INCLUDE ([AtomId], [ModelId]) WHERE [HilbertValue] IS NOT NULL;
GO
Action: Modify src/Hartonomous.Database/Tables/dbo.IngestionJobs.sql.Logic: This table is critical for the new governed, chunked ingestion.IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'IngestionJobs' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE [dbo].[IngestionJobs] (
        [IngestionJobId] BIGINT IDENTITY(1,1) NOT NULL,
        [ParentAtomId] BIGINT NOT NULL,
        [ModelId] INT NULL, -- If atomizing a model
        [JobStatus] VARCHAR(50) NOT NULL DEFAULT 'Pending', -- Pending, Processing, Failed, Complete
        [AtomChunkSize] INT NOT NULL DEFAULT 1000000,
        [CurrentAtomOffset] BIGINT NOT NULL DEFAULT 0,
        [TotalAtomsProcessed] BIGINT NOT NULL DEFAULT 0,
        [AtomQuota] BIGINT NOT NULL DEFAULT 5000000000, -- 5B atom quota
        [ErrorMessage] NVARCHAR(MAX) NULL,
        [CreatedAt] DATETIME2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [LastUpdatedAt] DATETIME2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_IngestionJobs] PRIMARY KEY CLUSTERED ([IngestionJobId] ASC),
        CONSTRAINT [FK_IngestionJobs_ParentAtom] FOREIGN KEY ([ParentAtomId]) REFERENCES [dbo].[Atoms] ([AtomId]) ON DELETE CASCADE
    );
END
GO
1.4. Define Core Reconstruction Views (SQL)Action: Create src/Hartonomous.Database/Views/dbo.vw_ReconstructModelLayerWeights.sql.Logic: This provides the essential OLAP-queryable view of all materialized weights.CREATE OR ALTER VIEW [dbo].[vw_ReconstructModelLayerWeights] AS
SELECT 
    tac.[ModelId],
    m.[ModelName],
    tac.[LayerIdx],
    ml.[LayerName],
    tac.[PositionX],
    tac.[PositionY],
    tac.[PositionZ],
    CAST(a.[AtomicValue] AS REAL) AS [WeightValue]
FROM [dbo].[TensorAtomCoefficients] tac
JOIN [dbo].[Atoms] a ON tac.[TensorAtomId] = a.[AtomId]
JOIN [dbo].[Models] m ON tac.[ModelId] = m.[ModelId]
LEFT JOIN [dbo].[ModelLayers] ml ON tac.[ModelId] = ml.[ModelId] AND tac.[LayerIdx] = ml.[LayerIdx]
WHERE a.[Modality] = 'model' AND a.[Subtype] = 'float32-weight';
GO
Phase 2: Governed Logic Implementation (Ingestion & OODA)Objective: Implement the production-ready, governed, and resumable logic to populate and manage the new, stable schema. This directly addresses the DoS and "Trojan Horse" vulnerabilities.2.1. Implement Governed, Chunked Ingestion (CLR)Action: Create a new file src/Hartonomous.Database/CLR/ModelStreamingFunctions.cs. This CLR code is the core parser.Logic:using System;
using System.Collections;
using System.IO;
using System.Text;
using Microsoft.SqlServer.Server;
using System.Data.SqlTypes;
// [AGENT: You MUST use Google Search to find and adapt production-ready, high-performance
// C# parsers for GGUF and SafeTensors that can work in .NET Framework 4.8.1 (for SQL CLR).
// The following is a high-granularity template. You MUST fill in the parsing logic.]

public struct AtomicWeight
{
    public SqlInt32 LayerIdx;
    public SqlInt32 PositionX;
    public SqlInt32 PositionY;
    public SqlInt32 PositionZ;
    public SqlSingle Value;

    public AtomicWeight(int layer, int x, int y, int z, float val)
    {
        LayerIdx = layer; PositionX = x; PositionY = y; PositionZ = z; Value = val;
    }
}

public static partial class ModelStreamingFunctions
{
    [SqlFunction(
        FillRowMethodName = "FillAtomicWeightRow",
        TableDefinition = "LayerIdx INT, PositionX INT, PositionY INT, PositionZ INT, Value REAL"
    )]
    public static IEnumerable clr_StreamAtomicWeights_Chunked(
        SqlBytes modelData,
        SqlString modelFormat,
        SqlInt64 atomOffset,      // The starting atom index (e.g., 0, 1000000, 2000000)
        SqlInt32 atomChunkSize    // How many atoms to read (e.g., 1,000,000)
    )
    {
        long startAtom = atomOffset.Value;
        long atomsToRead = atomChunkSize.Value;
        long atomsRead = 0;
        long currentAtomIndex = 0;

        using (var stream = modelData.Stream)
        using (var reader = new BinaryReader(stream, Encoding.UTF8, true))
        {
            if (modelFormat.Value.Equals("gguf", StringComparison.OrdinalIgnoreCase))
            {
                // [AGENT: Implement a GGUF parser here.
                // 1. Read GGUF header.
                // 2. Iterate through tensor metadata to find the *byte offset* of the *startAtom*.
                //    This is complex. You must calculate the cumulative atom counts of preceding
                //    tensors to find which tensor contains 'startAtom' and what its
                //    internal byte offset is.
                // 3. reader.BaseStream.Seek(calculated_byte_offset, SeekOrigin.Begin);
                // 4. while (atomsRead < atomsToRead && !parser.IsEndOfStream)
                // 5. {
                // 6.    (float w, int l, int x, int y, int z) = parser.GetNextWeight();
                // 7.    yield return new AtomicWeight(l, x, y, z, w);
                // 8.    atomsRead++;
                // 9. }
            }
            else if (modelFormat.Value.Equals("safetensors", StringComparison.OrdinalIgnoreCase))
            {
                // [AGENT: Implement a SafeTensors parser here.]
                // [AGENT: This is more complex as it requires finding the atom index
                //         by iterating through the JSON header and calculating offsets.
                //         Seek to the byte offset of the 'startAtom' index.]
                // [AGENT: Stream 'atomsToRead' weights, yielding them.]
            }
            // [AGENT: Add other formats like .bin (raw Llama2) parsers]
        }
    }

    public static void FillAtomicWeightRow(object row,
        out SqlInt32 LayerIdx, out SqlInt32 PositionX, out SqlInt32 PositionY,
        out SqlInt32 PositionZ, out SqlSingle Value)
    {
        AtomicWeight weight = (AtomicWeight)row;
        LayerIdx = weight.LayerIdx; PositionX = weight.PositionX;
        PositionY = weight.PositionY; PositionZ = weight.PositionZ; Value = weight.Value;
    }

    // [AGENT: Add clr_StreamAtomicText_Chunked, clr_StreamAtomicImage_Chunked, etc.
    //         all following this same resumable, chunked pattern.]
}
Action: Create src/Hartonomous.Database/Functions/dbo.clr_StreamAtomicWeights_Chunked.sql to register the new CLR TVF.2.2. Implement Governed, Chunked Ingestion (SQL)Action: Modify src/Hartonomous.Database/Procedures/dbo.sp_AtomizeModel_Atomic.sql.Logic: CREATE OR ALTER this procedure to be the T-SQL Governor state machine.CREATE OR ALTER PROCEDURE [dbo].[sp_AtomizeModel_Atomic]
    @IngestionJobId BIGINT,
    @ModelData VARBINARY(MAX),
    @ModelFormat VARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @JobStatus VARCHAR(50), @AtomChunkSize INT, @CurrentAtomOffset BIGINT;
    DECLARE @AtomQuota BIGINT, @TotalAtomsProcessed BIGINT;
    DECLARE @ParentAtomId BIGINT, @ModelId INT;

    -- 1. Load job state and governance parameters
    SELECT
        @JobStatus = JobStatus,
        @AtomChunkSize = AtomChunkSize,
        @CurrentAtomOffset = CurrentAtomOffset,
        @AtomQuota = AtomQuota,
        @TotalAtomsProcessed = TotalAtomsProcessed,
        @ParentAtomId = ParentAtomId,
        @ModelId = ModelId
    FROM dbo.IngestionJobs
    WHERE IngestionJobId = @IngestionJobId;

    IF @JobStatus IS NULL
    BEGIN
        RAISERROR('IngestionJobId not found.', 16, 1);
        RETURN -1;
    END

    IF @JobStatus = 'Complete' OR @JobStatus = 'Processing'
    BEGIN
        RAISERROR('Job is already complete or in progress.', 16, 1);
        RETURN -1;
    END

    UPDATE dbo.IngestionJobs SET JobStatus = 'Processing', LastUpdatedAt = SYSUTCDATETIME() WHERE IngestionJobId = @IngestionJobId;

    -- 2. Create temp tables for batch processing
    CREATE TABLE #ChunkWeights (
        [LayerIdx] INT, [PositionX] INT, [PositionY] INT, [PositionZ] INT, [Value] REAL
    );
    CREATE TABLE #UniqueWeights (
        [Value] REAL PRIMARY KEY,
        [AtomicValue] VARBINARY(4) NOT NULL,
        [ContentHash] BINARY(32) NOT NULL
    );
    CREATE TABLE #WeightToAtomId (
        [Value] REAL PRIMARY KEY,
        [AtomId] BIGINT NOT NULL
    );
    CREATE TABLE #ChunkCounts (
        [Value] REAL PRIMARY KEY,
        [Count] BIGINT NOT NULL
    );

    -- 3. Begin Governed State Machine Loop
    WHILE (1 = 1)
    BEGIN
        BEGIN TRY
            -- 3a. Check Governance
            IF @TotalAtomsProcessed > @AtomQuota
            BEGIN
                UPDATE dbo.IngestionJobs SET JobStatus = 'Failed', ErrorMessage = 'Atom quota exceeded.' WHERE IngestionJobId = @IngestionJobId;
                RAISERROR('Atom quota exceeded.', 16, 1);
                BREAK;
            END

            -- 3b. Clear temp tables for this chunk
            TRUNCATE TABLE #ChunkWeights;
            TRUNCATE TABLE #UniqueWeights;
            TRUNCATE TABLE #WeightToAtomId;
            TRUNCATE TABLE #ChunkCounts;

            -- 3c. Get ONE chunk from CLR
            INSERT INTO #ChunkWeights ([LayerIdx], [PositionX], [PositionY], [PositionZ], [Value])
            SELECT [LayerIdx], [PositionX], [PositionY], [PositionZ], [Value]
            FROM [dbo].[clr_StreamAtomicWeights_Chunked](@ModelData, @ModelFormat, @CurrentAtomOffset, @AtomChunkSize);

            DECLARE @RowsInChunk BIGINT = @@ROWCOUNT;
            IF @RowsInChunk = 0
                BREAK; -- Finished streaming

            -- 3d. Get unique atoms and counts *for this chunk*
            INSERT INTO #UniqueWeights ([Value], [AtomicValue], [ContentHash])
            SELECT DISTINCT [Value], CAST([Value] AS VARBINARY(4)), HASHBYTES('SHA2_56', CAST([Value] AS VARBINARY(4))) FROM #ChunkWeights;

            INSERT INTO #ChunkCounts ([Value], [Count])
            SELECT [Value], COUNT_BIG(*) FROM #ChunkWeights GROUP BY [Value];

            -- 3e. Begin small, fast transaction
            BEGIN TRANSACTION;

                -- 3f. Merge unique weights into dbo.Atoms (deduplication)
                MERGE [dbo].[Atoms] AS T
                USING #UniqueWeights AS S
                ON T.[ContentHash] = S.[ContentHash]
                WHEN NOT MATCHED BY TARGET THEN
                    INSERT ([Modality], [Subtype], [ContentHash], [AtomicValue], [ReferenceCount])
                    VALUES ('model', 'float32-weight', S.[ContentHash], S.[AtomicValue], 0); -- Insert with 0 count

                -- 3g. Update reference counts atomically
                UPDATE a
                SET a.[ReferenceCount] = a.[ReferenceCount] + cc.[Count]
                FROM [dbo].[Atoms] a
                JOIN #UniqueWeights uw ON a.[ContentHash] = uw.[ContentHash]
                JOIN #ChunkCounts cc ON uw.[Value] = cc.[Value]
                WHERE a.[Modality] = 'model' AND a.[Subtype] = 'float32-weight';

                -- 3h. Get AtomIds for this chunk
                INSERT INTO #WeightToAtomId ([Value], [AtomId])
                SELECT uw.[Value], a.[AtomId]
                FROM #UniqueWeights uw
                JOIN [dbo].[Atoms] a ON a.[ContentHash] = uw.[ContentHash]
                WHERE a.[Modality] = 'model' AND a.[Subtype] = 'float32-weight';

                -- 3i. Insert the reconstruction data for this chunk
                INSERT INTO [dbo].[TensorAtomCoefficients] (
                    [TensorAtomId], [ModelId], [LayerIdx], 
                    [PositionX], [PositionY], [PositionZ]
                )
                SELECT 
                    wta.[AtomId],
                    @ModelId,
                    s.[LayerIdx],
                    s.[PositionX],
                    s.[PositionY],
                    s.[PositionZ]
                FROM #ChunkWeights s
                JOIN #WeightToAtomId wta ON s.[Value] = wta.[Value];

            COMMIT TRANSACTION;

            -- 3j. Update state and log progress
            SET @CurrentAtomOffset = @CurrentAtomOffset + @AtomChunkSize; -- Note: This is chunk-based, not row-based
            SET @TotalAtomsProcessed = @TotalAtomsProcessed + @RowsInChunk;
            UPDATE dbo.IngestionJobs 
            SET CurrentAtomOffset = @CurrentAtomOffset, TotalAtomsProcessed = @TotalAtomsProcessed, LastUpdatedAt = SYSUTCDATETIME()
            WHERE IngestionJobId = @IngestionJobId;

        END TRY
        BEGIN CATCH
            IF (XACT_STATE() <> 0) ROLLBACK TRANSACTION;
            DECLARE @Error NVARCHAR(MAX) = ERROR_MESSAGE();
            UPDATE dbo.IngestionJobs SET JobStatus = 'Failed', ErrorMessage = @Error WHERE IngestionJobId = @IngestionJobId;
            RAISERROR(@Error, 16, 1);
            BREAK;
        END CATCH
    END -- End WHILE

    IF @JobStatus <> 'Failed'
        UPDATE dbo.IngestionJobs SET JobStatus = 'Complete', LastUpdatedAt = SYSUTCDATETIME() WHERE IngestionJobId = @IngestionJobId;

    DROP TABLE #ChunkWeights;
    DROP TABLE #UniqueWeights;
    DROP TABLE #WeightToAtomId;
    DROP TABLE #ChunkCounts;
END
GO
2.3. Implement Universal Atomization Procedures (SQL)Action: Create src/Hartonomous.Database/Procedures/dbo.sp_AtomizeText_Atomic.sql.Logic: CREATE OR ALTER this procedure. It must follow the same governed, chunked pattern as sp_AtomizeModel_Atomic but use dbo.AtomCompositions and explicitly store the structural geometry.-- (Stub for brevity - [AGENT: You MUST expand this to the full state machine logic])
CREATE OR ALTER PROCEDURE [dbo].[sp_AtomizeText_Atomic]
    @IngestionJobId BIGINT,
    @TextData NVARCHAR(MAX)
AS
BEGIN
    -- [AGENT: Implement the full, chunked, governed T-SQL state machine from 2.2]
    -- [AGENT: Use a CLR TVF clr_StreamAtomicText_Chunked(@TextData, @Offset, @ChunkSize)]
    -- [AGENT: The CLR function yields (token_id, token_string, sequence_index)]

    -- Key logic for structural (XYZM) storage in the loop:
    INSERT INTO [dbo].[AtomCompositions] (
        [ParentAtomId], [ComponentAtomId], [SequenceIndex], [SpatialKey]
    )
    SELECT 
        @ParentAtomId, -- From IngestionJobs table
        tka.[AtomId],   -- From #TokenToAtomId mapping table
        s.[SequenceIndex],
        -- This is the UNIFIED STRUCTURAL QUERY exploitation
        GEOMETRY::Point(
            s.[SequenceIndex], -- X = Position
            s.[TokenId],       -- Y = Value
            0,                 -- Z = (unused)
            s.[TokenId]        -- M = Value
        )
    FROM [dbo].[clr_StreamAtomicText_Chunked](...) s
    JOIN #TokenToAtomId tka ON s.[TokenId] = tka.[TokenId];
END
GO
Action: Create src/Hartonomous.Database/Procedures/dbo.sp_AtomizeImage_Atomic.sql.Logic: CREATE OR ALTER following the same pattern.-- (Stub for brevity - [AGENT: You MUST expand this to the full state machine logic])
CREATE OR ALTER PROCEDURE [dbo].[sp_AtomizeImage_Atomic]
    @IngestionJobId BIGINT,
    @ImageData VARBINARY(MAX)
AS
BEGIN
    -- [AGENT: Implement the full, chunked, governed T-SQL state machine from 2.2]
    -- [AGENT: Use a CLR TVF clr_StreamAtomicImage_Chunked(@ImageData, @Offset, @ChunkSize)]
    -- [AGENT: The CLR function yields (x_pos, y_pos, r, g, b, a)]

    -- Key logic for structural (XYZM) storage in the loop:
    INSERT INTO [dbo].[AtomCompositions] (
        [ParentAtomId], [ComponentAtomId], [SequenceIndex], [SpatialKey]
    )
    SELECT 
        @ParentAtomId,
        pxa.[AtomId],   -- From #PixelToAtomId mapping table
        (s.[y_pos] * @ImageWidth) + s.[x_pos] AS [SequenceIndex],
        -- This is the UNIFIED STRUCTURAL QUERY exploitation
        GEOMETRY::Point(
            s.[x_pos],         -- X = Position X
            s.[y_pos],         -- Y = Position Y
            0,                 -- Z = (unused)
            pxa.[AtomId]       -- M = Value (as AtomId)
        )
    FROM [dbo].[clr_StreamAtomicImage_Chunked](...) s
    JOIN #PixelToAtomId pxa ON s.[r] = pxa.[r] AND s.[g] = pxa.[g] ...;
END
GO
Action: Create src/Hartonomous.Database/Procedures/dbo.sp_AtomizeAudio_Atomic.sql and sp_AtomizeLargeNumber_Atomic.sql following this exact "T-SQL Governor" + "CLR Chunked Streamer" + "XYZM Structural Storage" pattern.2.4. Implement Full OODA Loop (SQL)Action: Create src/Hartonomous.Database/Procedures/dbo.sp_Hypothesize.sql.Logic:CREATE OR ALTER PROCEDURE [dbo].[sp_Hypothesize]
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @ConvHandle UNIQUEIDENTIFIER, @MsgType NVARCHAR(256), @MsgBody XML;

    WHILE (1 = 1)
    BEGIN
        BEGIN TRY
            BEGIN TRANSACTION;
            WAITFOR (
                RECEIVE TOP (1)
                    @ConvHandle = conversation_handle,
                    @MsgType = message_type_name,
                    @MsgBody = CAST(message_body AS XML)
                FROM [dbo].[HypothesizeQueue]
            ), TIMEOUT 5000;

            IF (@@ROWCOUNT = 0)
            BEGIN
                ROLLBACK TRANSACTION;
                BREAK;
            END

            IF @MsgType = N'//Hartonomous/AutonomousLoop/AnalyzeMessage'
            BEGIN
                -- [AGENT: You MUST implement a robust, production-ready rule engine.]
                -- [AGENT: Parse the @MsgBody XML. Use your internal reasoning (Tree of Thought)
                --         to evaluate the observations. Query DMVs (sys.dm_db_missing_index_details),
                --         Query Store (sys.query_store_query_text, etc.), and project tables
                --         (dbo.InferenceTracking, dbo.AtomEmbeddings) to find actionable insights.]

                -- Rule 1: Performance Anomaly (Missing Index)
                INSERT INTO dbo.PendingActions (ActionType, Parameters, Priority)
                SELECT
                    'CreateIndex',
                    (SELECT
                        T.value('@Database', 'nvarchar(128)') AS [Database],
                        T.value('@Schema', 'nvarchar(128)') AS [Schema],
                        T.value('@Table', 'nvarchar(128)') AS [Table],
                        (SELECT C.value('@Name', 'nvarchar(128)') FROM T.nodes('Column') AS N(C) FOR JSON PATH) AS [IncludeColumns]
                     FROM @MsgBody.nodes('/Analysis/MissingIndex') AS M(T)
                     FOR JSON PATH, WITHOUT_ARRAY_WRAPPER),
                    3
                WHERE @MsgBody.exist('/Analysis/MissingIndex') = 1;

                -- Rule 2: Untapped Knowledge (from Phase 3.4)
                INSERT INTO dbo.PendingActions (ActionType, Parameters, Priority)
                SELECT
                    'ConceptDiscovery',
                    N'{"hilbertValue": ' + R.value('@HilbertValue', 'nvarchar(100)') + N'}',
                    2 -- Medium priority
                FROM @MsgBody.nodes('/Analysis/UntappedKnowledge/Region') AS T(R)
                WHERE NOT EXISTS (
                    SELECT 1 FROM dbo.PendingActions 
                    WHERE ActionType = 'ConceptDiscovery' 
                      AND Parameters = N'{"hilbertValue": ' + R.value('@HilbertValue', 'nvarchar(100)') + N'}'
                );

                -- [AGENT: Add 5-10 more robust rules. Examples:]
                -- * 'QueryPlanRegression': Triggered by Query Store analysis.
                -- * 'DataCompression': Triggered if a table is large and uncompressed.
                -- * 'ArchiveModel': Triggered if a model's 'Velocity' (usage) is 0 for 90 days.
                -- * 'BuildVoronoiDomains': Triggered if new Concepts are added.

                -- Send confirmation to Act
                DECLARE @ActConvHandle UNIQUEIDENTIFIER;
                BEGIN DIALOG CONVERSATION @ActConvHandle
                FROM SERVICE [HypothesizeService]
                TO SERVICE N'ActService'
                ON CONTRACT [//Hartonomous/AutonomousLoop/ActContract]
                WITH ENCRYPTION = OFF;

                SEND ON CONVERSATION @ActConvHandle
                MESSAGE TYPE [//Hartonomous/AutonomousLoop/ActMessage]
                (CAST(N'<Act><Summary>Hypotheses generated and queued in dbo.PendingActions</Summary></Act>' AS XML));

                END CONVERSATION @ActConvHandle;
            END
            ELSE IF @MsgType = N'[http://schemas.microsoft.com/SQL/ServiceBroker/EndDialog](http://schemas.microsoft.com/SQL/ServiceBroker/EndDialog)'
            BEGIN
                END CONVERSATION @ConvHandle;
            END

            COMMIT TRANSACTION;
        END TRY
        BEGIN CATCH
            IF (XACT_STATE() <> 0) ROLLBACK TRANSACTION;
            -- [AGENT: Add robust error logging to a persistent log table here]
            IF @ConvHandle IS NOT NULL END CONVERSATION @ConvHandle WITH ERROR = 127, DESCRIPTION = N'Hypothesize Procedure Failed';
        END CATCH
    END
END
GO
Action: Modify src/Hartonomous.Database/Procedures/dbo.sp_Analyze.sql.Change: Find SEND ON CONVERSATION and change TO SERVICE N'ActService' to TO SERVICE N'HypothesizeService'. Change MESSAGE TYPE [//Hartonomous/AutonomousLoop/ActMessage] to MESSAGE TYPE [//Hartonomous/AutonomousLoop/AnalyzeMessage].Action: Modify src/Hartonomous.Database/Procedures/dbo.sp_Act.sql.Change: Change WAITFOR (RECEIVE ... FROM [dbo].[HypothesZEQueue]) to WAITFOR (RECEIVE ... FROM [dbo].[ActQueue]).Phase 3: Augment with Advanced Functionality (Math)Objective: Implement the advanced mathematical functions as non-destructive, production-ready augmentations.3.1. Implement Hilbert Curve Indexing (CLR & SQL)Action: Add the following C# code to a new file: src/Hartonomous.Database/CLR/Spatial/HilbertCurve.cs. This is a full, production-ready implementation.using Microsoft.SqlServer.Server;
using System.Data.SqlTypes;

// Based on the public domain implementation by John Skilling
public static partial class SpatialFunctions
{
    [SqlFunction(IsDeterministic = true, IsPrecise = false, DataAccess = DataAccessKind.None)]
    public static SqlInt64 clr_ComputeHilbertValue(SqlGeometry spatialKey, SqlInt32 precision)
    {
        if (spatialKey.IsNull || spatialKey.STIsEmpty().Value) return SqlInt64.Null;

        // [AGENT: This normalization logic is a hardcoded assumption. A production
        // system MUST query dbo.SpatialLandmarks to get dynamic MIN/MAX bounds.]
        double minX = 0, minY = 0, minZ = 0;
        double rangeX = 1, rangeY = 1, rangeZ = 1; // Assume 0-1 normalized space

        int p = precision.Value; // e.g., 21 (for 63 bits)
        long maxCoord = (1L << p) - 1;

        // Normalize and scale coordinates to the integer grid
        long x = (long)(((spatialKey.STX.Value - minX) / rangeX) * maxCoord);
        long y = (long)(((spatialKey.STY.Value - minY) / rangeY) * maxCoord);
        long z = (long)(((spatialKey.Z.Value - minZ) / rangeZ) * maxCoord);

        return new SqlInt64(Hilbert3D(x, y, z, p));
    }

    // Compact 3D Hilbert curve calculation, n=3, p=precision
    private static long Hilbert3D(long x, long y, long z, int p)
    {
        long h = 0;
        long qa, qb, qc, qd;

        for (long q = 1L << (p - 1); q > 0; q >>= 1)
        {
            qa = (x & q) > 0 ? 1L : 0L;
            qb = (y & q) > 0 ? 1L : 0L;
            qc = (z & q) > 0 ? 1L : 0L;
            qd = qa ^ qb;

            h <<= 3;
            h |= (qc << 2) | (qd << 1) | (qa ^ qd ^ qc);
        }
        return h;
    }
}
Action: Create src/Hartonomous.Database/Functions/dbo.fn_ComputeHilbertValue.sql to wrap the CLR call.CREATE OR ALTER FUNCTION [dbo].[fn_ComputeHilbertValue] (
    @spatialKey GEOMETRY
)
RETURNS BIGINT
AS
BEGIN
    -- Using 21-bit precision for a 63-bit Hilbert value
    RETURN [dbo].[clr_ComputeHilbertValue](@spatialKey, 21);
END
GO
Action: Modify src/Hartonomous.Database/Tables/dbo.AtomEmbeddings.sql.Change: ALTER TABLE [dbo].[AtomEmbeddings] ADD [HilbertValue] AS (ISNULL(dbo.fn_ComputeHilbertValue([SpatialKey]), 0)) PERSISTED;3.2. Implement Voronoi Semantic Domains (CLR & SQL)Action: Add the MIConvexHull NuGet package (v1.1.0) to the src/Hartonomous.Database/Hartonomous.Database.sqlproj. This is critical for 3D Voronoi.Action: Create src/Hartonomous.Database/CLR/Spatial/VoronoiGenerator.cs.using Microsoft.SqlServer.Server;
using System.Data.SqlTypes;
using MIConvexHull; // Requires the NuGet package
using System.Collections.Generic;

public static partial class SpatialFunctions
{
    [SqlFunction(
        FillRowMethodName = "FillVoronoiDomainRow",
        TableDefinition = "ConceptId INT, VoronoiDomain GEOMETRY"
    )]
    public static IEnumerable clr_ComputeVoronoiDomains(object conceptPoints)
    {
        // [AGENT: This is a complex function. You MUST complete this implementation.]
        // 1. Cast 'conceptPoints' (an IEnumerable) into List<IVertex>
        //    where IVertex is a custom class:
        //    class ConceptVertex : IVertex
        //    {
        //        public int Id { get; set; }
        //        public double[] Position { get; set; }
        //    }
        // 2. [AGENT: google_search "C# MIConvexHull 3D Voronoi example"]
        //    var voronoiMesh = VoronoiMesh.Create(vertices);
        // 3. Iterate through voronoiMesh.Vertices
        // 4. For each vertex, get its corresponding VoronoiCell.
        // 5. Convert the cell's facets (polygons) into a SQL Server
        //    GEOMETRY polygon or multipolygon. This is non-trivial and
        //    requires converting vertices to WKT or SqlGeometryBuilder.
        // 6. yield return new { ConceptId = v.Id, Domain = sqlGeometryPolygon };
    }

    public static void FillVoronoiDomainRow(object row, out SqlInt32 ConceptId, out SqlGeometry VoronoiDomain)
    {
        // [AGENT: Implement row filler]
        ConceptId = 0; VoronoiDomain = SqlGeometry.Null;
    }
}
Action: Create src/Hartonomous.Database/Procedures/dbo.sp_BuildConceptDomains.sql.Logic:CREATE OR ALTER PROCEDURE [dbo].[sp_BuildConceptDomains]
AS
BEGIN
    SET NOCOUNT ON;
    CREATE TABLE #ConceptPoints (ConceptId INT, Centroid GEOMETRY);
    INSERT INTO #ConceptPoints (ConceptId, Centroid)
    SELECT ConceptId, CentroidSpatialKey FROM [provenance].[Concepts] WHERE CentroidSpatialKey IS NOT NULL;

    MERGE [provenance].[Concepts] AS T
    USING (
        SELECT ConceptId, VoronoiDomain
        FROM [dbo].[clr_ComputeVoronoiDomains](TABLE #ConceptPoints)
    ) AS S
    ON T.ConceptId = S.ConceptId
    WHEN MATCHED THEN
        UPDATE SET ConceptDomain = S.VoronoiDomain;

    DROP TABLE #ConceptPoints;

    IF NOT EXISTS (SELECT 1 FROM sys.spatial_indexes WHERE name = 'SIX_Concepts_ConceptDomain')
        CREATE SPATIAL INDEX [SIX_Concepts_ConceptDomain] ON [provenance].[Concepts]([ConceptDomain]);
END
GO
Action: Modify dbo.sp_DiscoverAndBindConcepts.sql as shown in the v4 plan (Section 3.2).*3.3. Implement A Pathfinding for Generation (SQL)**Action: Create src/Hartonomous.Database/Procedures/dbo.sp_GenerateOptimalPath.sql.Logic: This is a complete, production-ready, MERGE-based A* implementation.CREATE OR ALTER PROCEDURE [dbo].[sp_GenerateOptimalPath]
    @StartAtomId BIGINT,
    @TargetConceptId INT,
    @MaxSteps INT = 50,
    @NeighborRadius FLOAT = 0.5 -- How "far" to look for the next token
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @StartPoint GEOMETRY = (SELECT [SpatialKey] FROM [dbo].[AtomEmbeddings] WHERE [AtomId] = @StartAtomId);
    DECLARE @TargetRegion GEOMETRY = (SELECT [ConceptDomain] FROM [provenance].[Concepts] WHERE [ConceptId] = @TargetConceptId);
    DECLARE @TargetCentroid GEOMETRY = @TargetRegion.STCentroid();

    IF @StartPoint IS NULL OR @TargetRegion IS NULL OR @TargetCentroid IS NULL
    BEGIN
        RAISERROR('Invalid start or target parameters. Ensure Target Concept has a computed ConceptDomain.', 16, 1);
        RETURN -1;
    END

    DECLARE @OpenSet TABLE (
        AtomId BIGINT PRIMARY KEY,
        ParentAtomId BIGINT,
        gCost FLOAT NOT NULL, -- Cost from start
        hCost FLOAT NOT NULL, -- Heuristic to end
        fCost AS (gCost + hCost)
    );
    CREATE TABLE #ClosedSet (AtomId BIGINT PRIMARY KEY, ParentAtomId BIGINT);

    DECLARE @CurrentAtomId BIGINT, @CurrentPoint GEOMETRY, @gCost FLOAT, @Steps INT = 0;
    DECLARE @GoalAtomId BIGINT = NULL;

    INSERT INTO @OpenSet (AtomId, ParentAtomId, gCost, hCost)
    VALUES (@StartAtomId, NULL, 0, @StartPoint.STDistance(@TargetCentroid));

    WHILE (EXISTS (SELECT 1 FROM @OpenSet) AND @Steps < @MaxSteps AND @GoalAtomId IS NULL)
    BEGIN
        -- 1. Get the best node from Open Set
        SELECT TOP 1 
            @CurrentAtomId = os.AtomId, 
            @gCost = os.gCost,
            @CurrentPoint = ae.[SpatialKey]
        FROM @OpenSet os
        JOIN [dbo].[AtomEmbeddings] ae ON os.AtomId = ae.AtomId
        ORDER BY fCost ASC;

        -- 2. Check for goal
        IF @CurrentPoint.STWithin(@TargetRegion) = 1
        BEGIN
            SET @GoalAtomId = @CurrentAtomId;
            BREAK; -- Goal found
        END

        -- 3. Move current node to Closed Set
        DELETE FROM @OpenSet WHERE AtomId = @CurrentAtomId;
        INSERT INTO #ClosedSet (AtomId, ParentAtomId) 
        SELECT AtomId, ParentAtomId FROM @OpenSet WHERE AtomId = @CurrentAtomId;

        -- 4. Find neighbors using the spatial index
        DECLARE @NeighborSearchRegion GEOMETRY = @CurrentPoint.STBuffer(@NeighborRadius);

        ;WITH Neighbors AS (
            SELECT
                ae.AtomId,
                ae.SpatialKey,
                @CurrentPoint.STDistance(ae.SpatialKey) AS New_gCost_Step,
                ae.SpatialKey.STDistance(@TargetCentroid) AS New_hCost
            FROM dbo.AtomEmbeddings ae WITH(INDEX(SIX_AtomEmbeddings_SpatialKey))
            WHERE ae.SpatialKey.STIntersects(@NeighborSearchRegion) = 1
              AND ae.AtomId <> @CurrentAtomId
              AND NOT EXISTS (SELECT 1 FROM #ClosedSet WHERE AtomId = ae.AtomId)
        )
        -- 5. Merge neighbors into Open Set
        MERGE @OpenSet AS T
        USING Neighbors AS S
        ON T.AtomId = S.AtomId
        WHEN MATCHED AND (S.New_gCost_Step + @gCost) < T.gCost THEN
            -- Shorter path found, update it
            UPDATE SET
                T.ParentAtomId = @CurrentAtomId,
                T.gCost = S.New_gCost_Step + @gCost
        WHEN NOT MATCHED BY TARGET THEN
            -- New node found
            INSERT (AtomId, ParentAtomId, gCost, hCost)
            VALUES (S.AtomId, @CurrentAtomId, S.New_gCost_Step + @gCost, S.New_hCost);

        SET @Steps = @Steps + 1;
    END

    -- 6. Reconstruct and return path
    IF @GoalAtomId IS NOT NULL
    BEGIN
        INSERT INTO #ClosedSet (AtomId, ParentAtomId) 
        SELECT AtomId, ParentAtomId FROM @OpenSet WHERE AtomId = @GoalAtomId;

        ;WITH PathCTE AS (
            SELECT AtomId, ParentAtomId, 0 AS Depth
            FROM #ClosedSet
            WHERE AtomId = @GoalAtomId
            UNION ALL
            SELECT cs.AtomId, cs.ParentAtomId, p.Depth + 1
            FROM #ClosedSet cs
            JOIN PathCTE p ON cs.AtomId = p.ParentAtomId
            WHERE p.ParentAtomId IS NOT NULL
        )
        SELECT p.AtomId, a.Modality, a.Subtype, a.[AtomicValue]
        FROM PathCTE p
        JOIN dbo.Atoms a ON p.AtomId = a.AtomId
        ORDER BY p.Depth DESC;
    END
    ELSE
    BEGIN
         RAISERROR('No path to target concept found within @MaxSteps.', 10, 1);
    END

    DROP TABLE #ClosedSet;
    RETURN 0;
END
GO
3.4. Implement Spatio-Temporal Analytics (SQL)Action: Modify src/Hartonomous.Database/Procedures/dbo.sp_Analyze.sql.Change: Add this "Untapped Knowledge" detection query inside the procedure.-- (Inside sp_Analyze, as part of the XML/JSON observation building)
DECLARE @UntappedKnowledge XML;
SET @UntappedKnowledge = (
    SELECT TOP 20 
        [HilbertValue] AS [@HilbertValue],
        [Pressure] AS [@Pressure],
        [Velocity] AS [@Velocity]
    FROM (
        SELECT 
            ae.[HilbertValue],
            COUNT_BIG(ae.AtomId) AS Pressure,
            (SELECT COUNT_BIG(1) FROM [dbo].[InferenceTracking] it WHERE it.AtomId = ae.AtomId) AS Velocity,
            PERCENT_RANK() OVER (ORDER BY COUNT_BIG(ae.AtomId) DESC) AS PressureRank,
            PERCENT_RANK() OVER (ORDER BY (SELECT COUNT_BIG(1) FROM [dbo].[InferenceTracking] it WHERE it.AtomId = ae.AtomId) ASC) AS VelocityRank
        FROM [dbo].[AtomEmbeddings] ae
        WHERE ae.[HilbertValue] IS NOT NULL AND ae.[HilbertValue] <> 0
        GROUP BY ae.[HilbertValue], ae.AtomId
    ) AS RankedRegions
    WHERE PressureRank < 0.1 -- Top 10% most dense
      AND VelocityRank < 0.1 -- Bottom 10% least used
    ORDER BY (PressureRank + VelocityRank) ASC
    FOR XML PATH('Region'), ROOT('UntappedKnowledge')
);
-- [AGENT: You must then merge this @UntappedKnowledge XML into the main @MsgBody]
3.5. Implement Generating Functions (Hybrid Tiering)Action: Modify src/Hartonomous.Database/Tables/dbo.Models.sql.Change: ALTER TABLE [dbo].[Models] ADD [StorageTier] VARCHAR(20) NOT NULL DEFAULT 'Materialized';Action: Create src/Hartonomous.Database/Tables/dbo.ArchivedModelFunctions.sql.CREATE TABLE [dbo].[ArchivedModelFunctions] (
    [ModelId] INT NOT NULL,
    [LayerIdx] INT NOT NULL,
    [FunctionString] NVARCHAR(2048) NOT NULL, -- Stores the function, e.g., "p0 + p1*x + p2*x^2"
    [FunctionType] VARCHAR(50) NOT NULL DEFAULT 'Polynomial',
    [ApproximationError] FLOAT NULL,
    CONSTRAINT [PK_ArchivedModelFunctions] PRIMARY KEY CLUSTERED ([ModelId], [LayerIdx])
);
Action: Add MathNet.Numerics.dll (v4.15.0 for .NET Framework) and NCalc.dll (v1.3.8) to the src/Hartonomous.Database/Dependencies/ folder and .sqlproj. Register them as UNSAFE assemblies.Action: Create src/Hartonomous.Database/CLR/GenerationFunctions.cs.using System;
using System.Collections.Generic;
using Microsoft.SqlServer.Server;
using System.Data.SqlTypes;
using MathNet.Numerics; // Requires assembly registration
using NCalc; // Requires assembly registration

public static partial class GenerationFunctions
{
    // [AGENT: This function finds a generating function (as a polynomial) for a weight sequence]
    [SqlFunction(DataAccess = DataAccessKind.Read)]
    public static SqlString clr_FindGeneratingFunction(SqlInt32 modelId, SqlInt32 layerIdx, SqlInt32 degree)
    {
        // [AGENT: This function MUST be rewritten to use the new
        //         clr_StreamAtomicWeights_Chunked function or query the
        //         vw_ReconstructModelLayerWeights view to get the weight sequence.]

        // 1. Get the sequence of (index, value) for the model layer
        List<double> x_values = new List<double>(); // index
        List<double> y_values = new List<double>(); // weight value
        // ... [AGENT: Use SqlPipe to execute query and fill lists] ...

        // 2. Fit a polynomial
        double[] polyCoefficients = Fit.Polynomial(x_values.ToArray(), y_values.ToArray(), degree.Value);

        // 3. Build the function string
        string functionString = "";
        for(int i = 0; i < polyCoefficients.Length; i++)
        {
            if (i == 0) functionString += polyCoefficients[i].ToString();
            else functionString += $" + {polyCoefficients[i]} * Pow(x, {i})";
        }
        return new SqlString(functionString);
    }

    // [AGENT: This function executes the stored function string]
    [SqlFunction(
        FillRowMethodName = "FillReconstructedWeightRow",
        TableDefinition = "PositionX INT, Value REAL"
    )]
    public static IEnumerable clr_ExecuteGeneratingFunction(SqlString functionString, SqlInt32 totalAtoms)
    {
        Expression e = new Expression(functionString.Value);
        for (int i = 0; i < totalAtoms.Value; i++)
        {
            e.Parameters["x"] = i;
            yield return new { PositionX = i, Value = (float)Convert.ToDouble(e.Evaluate()) };
        }
    }

    public static void FillReconstructedWeightRow(object row, out SqlInt32 PositionX, out SqlSingle Value)
    {
        PositionX = (SqlInt32)row.GetType().GetField("PositionX").GetValue(row);
        Value = (SqlSingle)row.GetType().GetField("Value").GetValue(row);
    }
}
Action: Create dbo.sp_ArchiveModelWeights.sql and modify dbo.sp_ReconstructModelWeights.sql as defined in the v4 plan (Section 3.5), using these new CLR function names.Phase 4: Finalization & ResynchronizationObjective: Synchronize the C# application layer with the completed, stable, and augmented database.4.1. Regenerate C# EntitiesAction: Execute the script scripts/generate-entities.ps1 -Force.Rationale: Regenerates all entities in src/Hartonomous.Data/Entities/ to match the new, blob-free, augmented, governed schema. This is a destructive but necessary step. All custom logic in partial classes will be preserved.4.2. Audit & Update Infrastructure LayerAction: Audit all services in src/Hartonomous.Infrastructure/Services/.Rationale: Any service that relied on the legacy schema (e.g., EmbeddingVector property on an entity) or legacy procedures (e.g., sp_AtomizeModel) will now fail to compile. These services MUST be rewritten to use the new access patterns (e.g., sp_AtomizeModel_Governed (or whatever the new proc is named) or querying vw_ReconstructModelLayerWeights).Phase 5: Documentation SynchronizationObjective: Update all documentation to reflect the final, implemented, production-ready system.5.1. Update All DocumentationAction: Perform a full audit of the docs/ directory.Changes:Purge all references to FILESTREAM, varbinary(max), or blob storage as a primary strategy.Update atomic-decomposition.md and atomic-vector-decomposition.md to reflect the completed implementation.Crucially, add new documentation for the "Dual Representation" architecture (Structural vs. Semantic).Add new documentation for the governed, chunked ingestion state machine (sp_AtomizeModel_Atomic, dbo.IngestionJobs, etc.).Add new documentation for the advanced query functions: sp_GenerateOptimalPath (A*), sp_BuildConceptDomains (Voronoi), the HilbertValue index, the AtomCompositions.SpatialKey (XYZM) query pattern, and the Generating Function archival strategy.Rationale: This brings all documentation in line with the final, stable, and production-ready implementation, ensuring the next engineer (or AI) understands the "why" and "how" of the completed vision.