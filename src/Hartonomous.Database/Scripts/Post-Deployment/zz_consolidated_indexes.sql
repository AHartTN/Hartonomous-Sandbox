PRINT 'Applying consolidated indexes...';
GO

CREATE UNIQUE INDEX [UX_AtomEmbeddingComponents_Embedding_Index] ON [dbo].[AtomEmbeddingComponents] ([AtomEmbeddingId], [ComponentIndex]);
GO
CREATE INDEX [IX_AtomEmbeddings_Atom_Model_Type] ON [dbo].[AtomEmbeddings] ([AtomId], [EmbeddingType], [ModelId]);
GO
CREATE INDEX [IX_AtomEmbeddings_ModelId] ON [dbo].[AtomEmbeddings] ([ModelId]);
GO
CREATE INDEX [IX_AtomEmbeddings_SpatialBucket] ON [dbo].[AtomEmbeddings] ([SpatialBucketX], [SpatialBucketY], [SpatialBucketZ]);
GO
CREATE INDEX [IX_AtomGraphEdges_CreatedAt] ON [graph].[AtomGraphEdges] ([CreatedAt]);  -- Changed CreatedUtc to CreatedAt
GO
CREATE INDEX [IX_AtomGraphEdges_RelationType] ON [graph].[AtomGraphEdges] ([RelationType]);  -- Changed EdgeType to RelationType
GO
CREATE INDEX [IX_AtomGraphEdges_Weight] ON [graph].[AtomGraphEdges] ([Weight]);
GO
CREATE INDEX [IX_AtomGraphNodes_AtomId] ON [graph].[AtomGraphNodes] ([AtomId]);
GO
CREATE INDEX [IX_AtomGraphNodes_CreatedAt] ON [graph].[AtomGraphNodes] ([CreatedAt]);  -- Changed CreatedUtc to CreatedAt
GO
CREATE INDEX [IX_AtomGraphNodes_Modality_Subtype] ON [graph].[AtomGraphNodes] ([Modality], [Subtype]);  -- Changed NodeType to Modality+Subtype
GO
CREATE INDEX [IX_AtomicAudioSamples_AmplitudeNormalized] ON [dbo].[AtomicAudioSamples] ([AmplitudeNormalized]);
GO
CREATE UNIQUE INDEX [IX_AtomicTextTokens_TokenHash] ON [dbo].[AtomicTextTokens] ([TokenHash]);
GO
CREATE UNIQUE INDEX [IX_AtomicTextTokens_TokenText] ON [dbo].[AtomicTextTokens] ([TokenText]);
GO
CREATE INDEX [IX_AtomPayloadStore_AtomId] ON [dbo].[AtomPayloadStore] ([AtomId]);
GO
CREATE INDEX [IX_AtomPayloadStore_RowGuid] ON [dbo].[AtomPayloadStore] ([RowGuid]);
GO
CREATE UNIQUE INDEX [UX_AtomPayloadStore_ContentHash] ON [dbo].[AtomPayloadStore] ([ContentHash]);
GO
CREATE INDEX [IX_AtomRelations_Source_Target_Type] ON [dbo].[AtomRelations] ([SourceAtomId], [TargetAtomId], [RelationType]);
GO
CREATE INDEX [IX_AtomRelations_TargetAtomId] ON [dbo].[AtomRelations] ([TargetAtomId]);
GO
CREATE UNIQUE INDEX [UX_Atoms_ContentHash] ON [dbo].[Atoms] ([ContentHash]);
GO
CREATE INDEX [IX_AudioData_DurationMs] ON [dbo].[AudioData] ([DurationMs]);
GO
CREATE INDEX [IX_AudioData_IngestionDate] ON [dbo].[AudioData] ([IngestionDate] DESC);
GO
CREATE INDEX [IX_AutonomousImprovement_ChangeType_RiskLevel] ON [dbo].[AutonomousImprovementHistory] ([ChangeType], [RiskLevel]) INCLUDE ([ErrorMessage], [SuccessScore]);
GO
CREATE INDEX [IX_AutonomousImprovement_StartedAt] ON [dbo].[AutonomousImprovementHistory] ([StartedAt] DESC);
GO
CREATE INDEX [IX_AutonomousImprovement_SuccessScore] ON [dbo].[AutonomousImprovementHistory] ([SuccessScore] DESC) WHERE [WasDeployed] = 1 AND [WasRolledBack] = 0;
GO
CREATE UNIQUE INDEX [UX_BillingMultipliers_Active] ON [dbo].[BillingMultipliers] ([RatePlanId], [Dimension], [Key]) WHERE [IsActive] = 1;
GO
CREATE UNIQUE INDEX [UX_BillingOperationRates_Active] ON [dbo].[BillingOperationRates] ([RatePlanId], [Operation]) WHERE [IsActive] = 1;
GO
CREATE INDEX [IX_BillingRatePlans_Tenant_IsActive] ON [dbo].[BillingRatePlans] ([TenantId], [IsActive]) INCLUDE ([UpdatedUtc]);
GO
CREATE UNIQUE INDEX [UX_BillingRatePlans_Tenant_PlanCode] ON [dbo].[BillingRatePlans] ([TenantId], [PlanCode]) WHERE [PlanCode] <> '';
GO
CREATE INDEX [IX_BillingUsageLedger_Operation_Timestamp] ON [dbo].[BillingUsageLedger] ([Operation], [TimestampUtc]) INCLUDE ([TenantId], [Units], [TotalCost]);
GO
CREATE INDEX [IX_BillingUsageLedger_TenantId_Timestamp] ON [dbo].[BillingUsageLedger] ([TenantId], [TimestampUtc]) INCLUDE ([Operation], [TotalCost]);
GO
CREATE INDEX [IX_CachedActivations_LastAccessed_HitCount] ON [dbo].[CachedActivations] ([LastAccessed] DESC, [HitCount] DESC);
GO
CREATE INDEX [IX_CachedActivations_LayerId] ON [dbo].[CachedActivations] ([LayerId]);
GO
CREATE UNIQUE INDEX [IX_CachedActivations_Model_Layer_InputHash] ON [dbo].[CachedActivations] ([ModelId], [LayerId], [InputHash]);
GO
CREATE UNIQUE INDEX [IX_CodeAtoms_CodeHash] ON [dbo].[CodeAtoms] ([CodeHash]) WHERE [CodeHash] IS NOT NULL;
GO
CREATE INDEX [IX_CodeAtoms_CodeType] ON [dbo].[CodeAtoms] ([CodeType]);
GO
CREATE INDEX [IX_CodeAtoms_CreatedAt] ON [dbo].[CodeAtoms] ([CreatedAt]);
GO
CREATE INDEX [IX_CodeAtoms_Language] ON [dbo].[CodeAtoms] ([Language]);
GO
CREATE INDEX [IX_CodeAtoms_QualityScore] ON [dbo].[CodeAtoms] ([QualityScore]);
GO
CREATE INDEX [IX_Concepts_CoherenceScore] ON [provenance].[Concepts] ([CoherenceScore] DESC);
GO
CREATE INDEX [IX_Concepts_ConceptName] ON [provenance].[Concepts] ([ConceptName]);
GO
CREATE INDEX [IX_Concepts_DiscoveryMethod] ON [provenance].[Concepts] ([DiscoveryMethod]);
GO
CREATE INDEX [IX_Concepts_ModelId_IsActive] ON [provenance].[Concepts] ([ModelId], [IsActive]);
GO
CREATE UNIQUE INDEX [UX_DeduplicationPolicies_PolicyName] ON [dbo].[DeduplicationPolicies] ([PolicyName]);
GO
CREATE INDEX [IX_GenerationStreams_CreatedUtc] ON [provenance].[GenerationStreams] ([CreatedUtc]);
GO
CREATE INDEX [IX_GenerationStreams_GenerationStreamId] ON [provenance].[GenerationStreams] ([GenerationStreamId]);
GO
CREATE INDEX [IX_GenerationStreams_Model] ON [provenance].[GenerationStreams] ([Model]);
GO
CREATE INDEX [IX_GenerationStreams_ModelId] ON [provenance].[GenerationStreams] ([ModelId]);
GO
CREATE INDEX [IX_GenerationStreams_Scope] ON [provenance].[GenerationStreams] ([Scope]);
GO
CREATE INDEX [IX_GenerationStreams_TenantId] ON [provenance].[GenerationStreams] ([TenantId]);
GO
CREATE INDEX [IX_ImagePatches_ImageId_PatchX_PatchY] ON [dbo].[ImagePatches] ([ImageId], [PatchX], [PatchY]);
GO
CREATE INDEX [IX_Images_IngestionDate] ON [dbo].[Images] ([IngestionDate] DESC);
GO
CREATE INDEX [IX_Images_Width_Height] ON [dbo].[Images] ([Width], [Height]);
GO
CREATE INDEX [IX_InferenceCache_CacheKey] ON [dbo].[InferenceCache] ([CacheKey]);
GO
CREATE INDEX [IX_InferenceCache_LastAccessedUtc] ON [dbo].[InferenceCache] ([LastAccessedUtc] DESC);
GO
CREATE INDEX [IX_InferenceCache_ModelId_InferenceType] ON [dbo].[InferenceCache] ([ModelId], [InferenceType]);
GO
CREATE INDEX [IX_InferenceRequests_CacheHit] ON [dbo].[InferenceRequests] ([CacheHit]);
GO
CREATE INDEX [IX_InferenceRequests_InputHash] ON [dbo].[InferenceRequests] ([InputHash]);
GO
CREATE INDEX [IX_InferenceRequests_ModelId] ON [dbo].[InferenceRequests] ([ModelId]);
GO
CREATE INDEX [IX_InferenceRequests_RequestTimestamp] ON [dbo].[InferenceRequests] ([RequestTimestamp] DESC);
GO
CREATE INDEX [IX_InferenceRequests_TaskType] ON [dbo].[InferenceRequests] ([TaskType]);
GO
CREATE INDEX [IX_InferenceSteps_InferenceId_StepNumber] ON [dbo].[InferenceSteps] ([InferenceId], [StepNumber]);
GO
CREATE INDEX [IX_InferenceSteps_ModelId] ON [dbo].[InferenceSteps] ([ModelId]);
GO
CREATE INDEX [IX_IngestionJobAtoms_AtomId] ON [dbo].[IngestionJobAtoms] ([AtomId]);
GO
CREATE INDEX [IX_IngestionJobAtoms_Job_Atom] ON [dbo].[IngestionJobAtoms] ([IngestionJobId], [AtomId]);
GO
CREATE INDEX [IX_LayerTensorSegments_M_Range] ON [dbo].[LayerTensorSegments] ([LayerId], [MMin], [MMax]);
GO
CREATE INDEX [IX_LayerTensorSegments_Morton] ON [dbo].[LayerTensorSegments] ([MortonCode]);
GO
CREATE INDEX [IX_LayerTensorSegments_Z_Range] ON [dbo].[LayerTensorSegments] ([LayerId], [ZMin], [ZMax]);
GO
CREATE UNIQUE INDEX [UX_LayerTensorSegments_LayerId_SegmentOrdinal] ON [dbo].[LayerTensorSegments] ([LayerId], [SegmentOrdinal]);
GO
CREATE INDEX [IX_ModelLayers_LayerAtomId] ON [dbo].[ModelLayers] ([LayerAtomId]);
GO
CREATE INDEX [IX_ModelLayers_LayerType] ON [dbo].[ModelLayers] ([LayerType]);
GO
CREATE INDEX [IX_ModelLayers_M_Range] ON [dbo].[ModelLayers] ([ModelId], [MMin], [MMax]);
GO
CREATE INDEX [IX_ModelLayers_ModelId_LayerIdx] ON [dbo].[ModelLayers] ([ModelId], [LayerIdx]);
GO
CREATE INDEX [IX_ModelLayers_Morton] ON [dbo].[ModelLayers] ([MortonCode]);
GO
CREATE INDEX [IX_ModelLayers_Z_Range] ON [dbo].[ModelLayers] ([ModelId], [ZMin], [ZMax]);
GO
CREATE UNIQUE INDEX [IX_ModelMetadata_ModelId] ON [dbo].[ModelMetadata] ([ModelId]);
GO
CREATE INDEX [IX_TenantSecurityPolicy_EffectiveDates] ON [dbo].[TenantSecurityPolicy] ([EffectiveFrom], [EffectiveTo]);
GO
CREATE INDEX [IX_TenantSecurityPolicy_IsActive] ON [dbo].[TenantSecurityPolicy] ([IsActive]);
GO
CREATE INDEX [IX_TenantSecurityPolicy_TenantId_PolicyType] ON [dbo].[TenantSecurityPolicy] ([TenantId], [PolicyType]);
GO
CREATE INDEX [IX_TensorAtomCoefficients_Lookup] ON [dbo].[TensorAtomCoefficients] ([TensorAtomId], [ParentLayerId], [TensorRole]);
GO
CREATE INDEX [IX_TensorAtomCoefficients_ParentLayerId] ON [dbo].[TensorAtomCoefficients] ([ParentLayerId]);
GO
CREATE INDEX [IX_TensorAtoms_AtomId] ON [dbo].[TensorAtoms] ([AtomId]);
GO
CREATE INDEX [IX_TensorAtoms_LayerId] ON [dbo].[TensorAtoms] ([LayerId]);
GO
CREATE INDEX [IX_TensorAtoms_Model_Layer_Type] ON [dbo].[TensorAtoms] ([ModelId], [LayerId], [AtomType]);
GO
CREATE INDEX [IX_TestResults_ExecutionTimeMs] ON [dbo].[TestResults] ([ExecutionTimeMs] DESC);
GO
CREATE INDEX [IX_TestResults_TestCategory_ExecutedAt] ON [dbo].[TestResults] ([TestCategory] DESC, [ExecutedAt] DESC);
GO
CREATE INDEX [IX_TestResults_TestStatus] ON [dbo].[TestResults] ([TestStatus]);
GO
CREATE INDEX [IX_TestResults_TestSuite_ExecutedAt] ON [dbo].[TestResults] ([TestSuite] DESC, [ExecutedAt] DESC);
GO
CREATE INDEX [IX_TokenVocabulary_ModelId_Token] ON [dbo].[TokenVocabulary] ([ModelId], [Token]);
GO
CREATE UNIQUE INDEX [IX_TokenVocabulary_ModelId_TokenId] ON [dbo].[TokenVocabulary] ([ModelId], [TokenId]);
GO
CREATE UNIQUE INDEX [IX_VideoFrames_VideoId_FrameNumber] ON [dbo].[VideoFrames] ([VideoId], [FrameNumber]);
GO
CREATE INDEX [IX_VideoFrames_VideoId_TimestampMs] ON [dbo].[VideoFrames] ([VideoId], [TimestampMs]);
GO
CREATE INDEX [IX_Videos_IngestionDate] ON [dbo].[Videos] ([IngestionDate] DESC);
GO
CREATE INDEX [IX_Videos_ResolutionWidth_ResolutionHeight] ON [dbo].[Videos] ([ResolutionWidth], [ResolutionHeight]);
GO

PRINT 'Adding indexes for graph.AtomGraphNodes and graph.AtomGraphEdges...';
GO
CREATE INDEX IX_AtomGraphNodes_Modality ON graph.AtomGraphNodes (Modality, Subtype);
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'SIX_AtomGraphNodes_SpatialKey'
    AND object_id = OBJECT_ID('graph.AtomGraphNodes')
)
BEGIN
    CREATE SPATIAL INDEX SIX_AtomGraphNodes_SpatialKey 
    ON graph.AtomGraphNodes (SpatialKey) 
    WITH (
        BOUNDING_BOX = (-10000000, -10000000, 10000000, 10000000),
        GRIDS = (
            LEVEL_1 = MEDIUM,
            LEVEL_2 = MEDIUM,
            LEVEL_3 = MEDIUM,
            LEVEL_4 = LOW
        ),
        CELLS_PER_OBJECT = 16,
        PAD_INDEX = ON,
        SORT_IN_TEMPDB = ON
    );
    PRINT '✓ SIX_AtomGraphNodes_SpatialKey created';
END
ELSE
BEGIN
    PRINT '✓ SIX_AtomGraphNodes_SpatialKey already exists';
END;
GO

CREATE INDEX IX_AtomGraphEdges_Type ON graph.AtomGraphEdges (RelationType);
GO

-- Graph edge indexes on $from_id and $to_id are created in separate script (graph.AtomGraphEdges_Add_PseudoColumn_Indexes.sql)

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'SIX_AtomGraphEdges_SpatialExpression'
    AND object_id = OBJECT_ID('graph.AtomGraphEdges')
)
BEGIN
    CREATE SPATIAL INDEX SIX_AtomGraphEdges_SpatialExpression 
    ON graph.AtomGraphEdges (SpatialExpression) 
    WITH (
        BOUNDING_BOX = (-10000000, -10000000, 10000000, 10000000),
        GRIDS = (
            LEVEL_1 = HIGH,
            LEVEL_2 = MEDIUM,
            LEVEL_3 = MEDIUM,
            LEVEL_4 = LOW
        ),
        CELLS_PER_OBJECT = 16,
        PAD_INDEX = ON,
        SORT_IN_TEMPDB = ON
    );
    PRINT '✓ SIX_AtomGraphEdges_SpatialExpression created';
END
ELSE
BEGIN
    PRINT '✓ SIX_AtomGraphEdges_SpatialExpression already exists';
END;
GO

-- Creates and repairs spatial indexes required by the platform. Runnable multiple times safely.

SET NOCOUNT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET ARITHABORT ON;
SET NUMERIC_ROUNDABORT OFF;
GO

PRINT '============================================================';
PRINT 'CREATING SPATIAL INDEXES';
PRINT '============================================================';
GO

-- ==========================================
-- AtomEmbeddings.SpatialGeometry (Fine-grained)
-- ==========================================
IF EXISTS (
    SELECT 1 FROM sys.indexes 
    WHERE name = 'idx_spatial_fine'
      AND object_id = OBJECT_ID('dbo.AtomEmbeddings')
)
BEGIN
    PRINT 'Dropping legacy idx_spatial_fine on AtomEmbeddings.SpatialGeometry...';
    DROP INDEX idx_spatial_fine ON dbo.AtomEmbeddings;
END;

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes 
    WHERE name = 'IX_AtomEmbeddings_SpatialGeometry' 
    AND object_id = OBJECT_ID('dbo.AtomEmbeddings')
)
BEGIN
    PRINT 'Creating IX_AtomEmbeddings_SpatialGeometry on AtomEmbeddings.SpatialGeometry...';
    
    CREATE SPATIAL INDEX IX_AtomEmbeddings_SpatialGeometry
    ON dbo.AtomEmbeddings (SpatialGeometry)
    WITH (
        BOUNDING_BOX = (-1000, -1000, 1000, 1000),
        GRIDS = (
            LEVEL_1 = MEDIUM,
            LEVEL_2 = MEDIUM,
            LEVEL_3 = MEDIUM,
            LEVEL_4 = MEDIUM
        ),
        CELLS_PER_OBJECT = 16,
        PAD_INDEX = ON,
        SORT_IN_TEMPDB = ON
    );
    
    PRINT '  ✓ IX_AtomEmbeddings_SpatialGeometry created';
END
ELSE
BEGIN
    PRINT '  ✓ IX_AtomEmbeddings_SpatialGeometry already exists';
END;
GO

-- ==========================================
-- AtomEmbeddings.SpatialCoarse (Fast filter)
-- ==========================================
IF EXISTS (
    SELECT 1 FROM sys.indexes 
    WHERE name = 'idx_spatial_coarse'
      AND object_id = OBJECT_ID('dbo.AtomEmbeddings')
)
BEGIN
    PRINT 'Dropping legacy idx_spatial_coarse on AtomEmbeddings.SpatialCoarse...';
    DROP INDEX idx_spatial_coarse ON dbo.AtomEmbeddings;
END;

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes 
    WHERE name = 'IX_AtomEmbeddings_SpatialCoarse' 
    AND object_id = OBJECT_ID('dbo.AtomEmbeddings')
)
BEGIN
    PRINT 'Creating IX_AtomEmbeddings_SpatialCoarse on AtomEmbeddings.SpatialCoarse...';
    
    CREATE SPATIAL INDEX IX_AtomEmbeddings_SpatialCoarse
    ON dbo.AtomEmbeddings (SpatialCoarse)
    WITH (
        BOUNDING_BOX = (-100, -100, 100, 100),
        GRIDS = (
            LEVEL_1 = LOW,
            LEVEL_2 = LOW,
            LEVEL_3 = LOW,
            LEVEL_4 = LOW
        ),
        CELLS_PER_OBJECT = 8,
        PAD_INDEX = ON,
        SORT_IN_TEMPDB = ON
    );
    
    PRINT '  ✓ IX_AtomEmbeddings_SpatialCoarse created';
END
ELSE
BEGIN
    PRINT '  ✓ IX_AtomEmbeddings_SpatialCoarse already exists';
END;
GO

-- ==========================================
-- TensorAtoms.SpatialSignature (Weight signatures)
-- ==========================================
IF EXISTS (
    SELECT 1 FROM sys.indexes 
    WHERE name = 'idx_spatial_signature'
      AND object_id = OBJECT_ID('dbo.TensorAtoms')
)
BEGIN
    PRINT 'Dropping legacy idx_spatial_signature on TensorAtoms.SpatialSignature...';
    DROP INDEX idx_spatial_signature ON dbo.TensorAtoms;
END;

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes 
    WHERE name = 'IX_TensorAtoms_SpatialSignature' 
    AND object_id = OBJECT_ID('dbo.TensorAtoms')
)
BEGIN
    PRINT 'Creating IX_TensorAtoms_SpatialSignature on TensorAtoms.SpatialSignature...';
    
    CREATE SPATIAL INDEX IX_TensorAtoms_SpatialSignature
    ON dbo.TensorAtoms (SpatialSignature)
    WITH (
        BOUNDING_BOX = (-500, -500, 500, 500),
        GRIDS = (
            LEVEL_1 = MEDIUM,
            LEVEL_2 = MEDIUM,
            LEVEL_3 = LOW,
            LEVEL_4 = LOW
        ),
        CELLS_PER_OBJECT = 12,
        PAD_INDEX = ON,
        SORT_IN_TEMPDB = ON
    );
    
    PRINT '  ✓ IX_TensorAtoms_SpatialSignature created';
END
ELSE
BEGIN
    PRINT '  ✓ IX_TensorAtoms_SpatialSignature already exists';
END;
GO

-- ==========================================
-- TensorAtoms.GeometryFootprint (Weight topology)
-- ==========================================
IF EXISTS (
    SELECT 1 FROM sys.indexes 
    WHERE name = 'idx_geometry_footprint'
      AND object_id = OBJECT_ID('dbo.TensorAtoms')
)
BEGIN
    PRINT 'Dropping legacy idx_geometry_footprint on TensorAtoms.GeometryFootprint...';
    DROP INDEX idx_geometry_footprint ON dbo.TensorAtoms;
END;

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes 
    WHERE name = 'IX_TensorAtoms_GeometryFootprint' 
    AND object_id = OBJECT_ID('dbo.TensorAtoms')
)
BEGIN
    PRINT 'Creating IX_TensorAtoms_GeometryFootprint on TensorAtoms.GeometryFootprint...';
    
    CREATE SPATIAL INDEX IX_TensorAtoms_GeometryFootprint
    ON dbo.TensorAtoms (GeometryFootprint)
    WITH (
        BOUNDING_BOX = (-1000, -1000, 1000, 1000),
        GRIDS = (
            LEVEL_1 = HIGH,
            LEVEL_2 = MEDIUM,
            LEVEL_3 = MEDIUM,
            LEVEL_4 = LOW
        ),
        CELLS_PER_OBJECT = 16,
        PAD_INDEX = ON,
        SORT_IN_TEMPDB = ON
    );
    
    PRINT '  ✓ IX_TensorAtoms_GeometryFootprint created';
END
ELSE
BEGIN
    PRINT '  ✓ IX_TensorAtoms_GeometryFootprint already exists';
END;
GO

-- ==========================================
-- Atoms.SpatialKey (Optional - if used)
-- ==========================================
IF EXISTS (
    SELECT 1 FROM sys.indexes 
    WHERE name = 'idx_atom_spatial_key'
      AND object_id = OBJECT_ID('dbo.Atoms')
)
BEGIN
    PRINT 'Dropping legacy idx_atom_spatial_key on Atoms.SpatialKey...';
    DROP INDEX idx_atom_spatial_key ON dbo.Atoms;
END;

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes 
    WHERE name = 'IX_Atoms_SpatialKey' 
    AND object_id = OBJECT_ID('dbo.Atoms')
)
BEGIN
    PRINT 'Creating IX_Atoms_SpatialKey on Atoms.SpatialKey...';
    
    CREATE SPATIAL INDEX IX_Atoms_SpatialKey
    ON dbo.Atoms (SpatialKey)
    WITH (
        BOUNDING_BOX = (-10000, -10000, 10000, 10000),
        GRIDS = (
            LEVEL_1 = LOW,
            LEVEL_2 = LOW,
            LEVEL_3 = LOW,
            LEVEL_4 = LOW
        ),
        CELLS_PER_OBJECT = 8,
        PAD_INDEX = ON,
        SORT_IN_TEMPDB = ON
    );
    
    PRINT '  ✓ IX_Atoms_SpatialKey created';
END
ELSE
BEGIN
    PRINT '  ✓ IX_Atoms_SpatialKey already exists';
END;
GO

IF OBJECT_ID('dbo.TokenEmbeddingsGeo', 'U') IS NOT NULL
BEGIN
    IF EXISTS (
        SELECT 1 FROM sys.indexes 
        WHERE name = 'idx_spatial_embedding'
          AND object_id = OBJECT_ID('dbo.TokenEmbeddingsGeo')
    )
    BEGIN
        PRINT 'Dropping legacy idx_spatial_embedding on TokenEmbeddingsGeo.SpatialProjection...';
        DROP INDEX idx_spatial_embedding ON dbo.TokenEmbeddingsGeo;
    END;

    IF NOT EXISTS (
        SELECT 1 FROM sys.indexes 
        WHERE name = 'IX_TokenEmbeddingsGeo_SpatialProjection' 
        AND object_id = OBJECT_ID('dbo.TokenEmbeddingsGeo')
    )
    BEGIN
        PRINT 'Creating IX_TokenEmbeddingsGeo_SpatialProjection on TokenEmbeddingsGeo.SpatialProjection...';
        
        CREATE SPATIAL INDEX IX_TokenEmbeddingsGeo_SpatialProjection
        ON dbo.TokenEmbeddingsGeo(SpatialProjection)
        USING GEOMETRY_GRID
        WITH (
            BOUNDING_BOX = (-100, -100, 100, 100),
            GRIDS = (
                LEVEL_1 = HIGH,
                LEVEL_2 = HIGH,
                LEVEL_3 = MEDIUM,
                LEVEL_4 = LOW
            ),
            CELLS_PER_OBJECT = 16
        );
        
        PRINT '  ✓ IX_TokenEmbeddingsGeo_SpatialProjection created';
    END
    ELSE
    BEGIN
        PRINT '  ✓ IX_TokenEmbeddingsGeo_SpatialProjection already exists';
    END;
END
ELSE
BEGIN
    PRINT '  WARNING: TokenEmbeddingsGeo table not found; skipping spatial projection index.';
END;
GO

-- ==========================================
-- CodeAtoms.Embedding (AST structural search)
-- ==========================================
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_CodeAtoms_Embedding'
    AND object_id = OBJECT_ID('dbo.CodeAtoms')
)
BEGIN
    PRINT 'Creating IX_CodeAtoms_Embedding on CodeAtoms.Embedding...';

    CREATE SPATIAL INDEX IX_CodeAtoms_Embedding
    ON dbo.CodeAtoms (Embedding)
    WITH (
        BOUNDING_BOX = (-1000, -1000, 1000, 1000),
        GRIDS = (
            LEVEL_1 = MEDIUM,
            LEVEL_2 = MEDIUM,
            LEVEL_3 = MEDIUM,
            LEVEL_4 = LOW
        ),
        CELLS_PER_OBJECT = 16,
        PAD_INDEX = ON,
        SORT_IN_TEMPDB = ON
    );

    PRINT '  ✓ IX_CodeAtoms_Embedding created';
END
ELSE
BEGIN
    PRINT '  ✓ IX_CodeAtoms_Embedding already exists';
END;
GO

-- ==========================================
-- AudioData.Spectrogram (Audio waveform search)
-- ==========================================
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_AudioData_Spectrogram'
    AND object_id = OBJECT_ID('dbo.AudioData')
)
BEGIN
    PRINT 'Creating IX_AudioData_Spectrogram on AudioData.Spectrogram...';

    CREATE SPATIAL INDEX IX_AudioData_Spectrogram
    ON dbo.AudioData (Spectrogram)
    WITH (
        BOUNDING_BOX = (-500, -500, 500, 500),
        GRIDS = (
            LEVEL_1 = MEDIUM,
            LEVEL_2 = MEDIUM,
            LEVEL_3 = LOW,
            LEVEL_4 = LOW
        ),
        CELLS_PER_OBJECT = 12,
        PAD_INDEX = ON,
        SORT_IN_TEMPDB = ON
    );

    PRINT '  ✓ IX_AudioData_Spectrogram created';
END
ELSE
BEGIN
    PRINT '  ✓ IX_AudioData_Spectrogram already exists';
END;
GO

-- ==========================================
-- VideoFrames.MotionVectors (Video motion analysis)
-- ==========================================
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_VideoFrames_MotionVectors'
    AND object_id = OBJECT_ID('dbo.VideoFrames')
)
BEGIN
    PRINT 'Creating IX_VideoFrames_MotionVectors on VideoFrames.MotionVectors...';

    CREATE SPATIAL INDEX IX_VideoFrames_MotionVectors
    ON dbo.VideoFrames (MotionVectors)
    WITH (
        BOUNDING_BOX = (-1000, -1000, 1000, 1000),
        GRIDS = (
            LEVEL_1 = MEDIUM,
            LEVEL_2 = MEDIUM,
            LEVEL_3 = MEDIUM,
            LEVEL_4 = LOW
        ),
        CELLS_PER_OBJECT = 16,
        PAD_INDEX = ON,
        SORT_IN_TEMPDB = ON
    );

    PRINT '  ✓ IX_VideoFrames_MotionVectors created';
END
ELSE
BEGIN
    PRINT '  ✓ IX_VideoFrames_MotionVectors already exists';
END;
GO

-- ==========================================
-- Images.ObjectRegions (Image region search)
-- ==========================================
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_Images_ObjectRegions'
    AND object_id = OBJECT_ID('dbo.Images')
)
BEGIN
    PRINT 'Creating IX_Images_ObjectRegions on Images.ObjectRegions...';

    CREATE SPATIAL INDEX IX_Images_ObjectRegions
    ON dbo.Images (ObjectRegions)
    WITH (
        BOUNDING_BOX = (-1000, -1000, 1000, 1000),
        GRIDS = (
            LEVEL_1 = HIGH,
            LEVEL_2 = MEDIUM,
            LEVEL_3 = MEDIUM,
            LEVEL_4 = LOW
        ),
        CELLS_PER_OBJECT = 16,
        PAD_INDEX = ON,
        SORT_IN_TEMPDB = ON
    );

    PRINT '  ✓ IX_Images_ObjectRegions created';
END
ELSE
BEGIN
    PRINT '  ✓ IX_Images_ObjectRegions already exists';
END;
GO

-- ==========================================
-- VERIFY INDEX CREATION
-- ==========================================
PRINT '';
PRINT 'Spatial Index Summary:';
GO

SELECT
    OBJECT_NAME(i.object_id) AS TableName,
    i.name AS IndexName,
    i.type_desc AS IndexType,
    st.bounding_box_xmin,
    st.bounding_box_ymin,
    st.bounding_box_xmax,
    st.bounding_box_ymax,
    st.level_1_grid_desc,
    st.cells_per_object
FROM sys.indexes i
INNER JOIN sys.spatial_index_tessellations st ON i.object_id = st.object_id AND i.index_id = st.index_id
WHERE OBJECT_NAME(i.object_id) IN ('AtomEmbeddings', 'TensorAtoms', 'Atoms', 'TokenEmbeddingsGeo', 'CodeAtoms', 'AudioData', 'VideoFrames', 'Images')
ORDER BY OBJECT_NAME(i.object_id), i.name;
GO

PRINT '';
PRINT '============================================================';
PRINT 'SPATIAL INDEXES CREATED';
PRINT '============================================================';
GO

