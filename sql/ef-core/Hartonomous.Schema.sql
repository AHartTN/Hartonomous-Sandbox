IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107210027_InitialCreate'
)
BEGIN
    IF SCHEMA_ID(N'graph') IS NULL EXEC(N'CREATE SCHEMA [graph];');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107210027_InitialCreate'
)
BEGIN
    IF SCHEMA_ID(N'provenance') IS NULL EXEC(N'CREATE SCHEMA [provenance];');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107210027_InitialCreate'
)
BEGIN
    CREATE TABLE [graph].[AtomGraphEdges] (
        [EdgeId] bigint NOT NULL IDENTITY,
        [EdgeType] nvarchar(50) NOT NULL,
        [Weight] float NOT NULL DEFAULT 1.0E0,
        [Metadata] nvarchar(max) NULL,
        [ValidFrom] datetime2 NULL,
        [ValidTo] datetime2 NULL,
        [CreatedUtc] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_AtomGraphEdges] PRIMARY KEY ([EdgeId]),
        CONSTRAINT [CK_AtomGraphEdges_EdgeType] CHECK ([EdgeType] IN ('DerivedFrom', 'ComponentOf', 'SimilarTo', 'Uses', 'InputTo', 'OutputFrom', 'BindsToConcept')),
        CONSTRAINT [CK_AtomGraphEdges_Weight] CHECK ([Weight] >= 0.0 AND [Weight] <= 1.0)
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107210027_InitialCreate'
)
BEGIN
    CREATE TABLE [AtomicAudioSamples] (
        [SampleHash] BINARY(32) NOT NULL,
        [AmplitudeNormalized] REAL NOT NULL,
        [AmplitudeInt16] SMALLINT NOT NULL,
        [ReferenceCount] BIGINT NOT NULL DEFAULT CAST(0 AS BIGINT),
        [FirstSeen] DATETIME2 NOT NULL DEFAULT (SYSUTCDATETIME()),
        [LastReferenced] DATETIME2 NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_AtomicAudioSamples] PRIMARY KEY ([SampleHash])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107210027_InitialCreate'
)
BEGIN
    CREATE TABLE [AtomicPixels] (
        [PixelHash] BINARY(32) NOT NULL,
        [R] TINYINT NOT NULL,
        [G] TINYINT NOT NULL,
        [B] TINYINT NOT NULL,
        [A] TINYINT NOT NULL DEFAULT CAST(255 AS TINYINT),
        [ColorPoint] GEOMETRY NULL,
        [ReferenceCount] BIGINT NOT NULL DEFAULT CAST(0 AS BIGINT),
        [FirstSeen] DATETIME2 NOT NULL DEFAULT (SYSUTCDATETIME()),
        [LastReferenced] DATETIME2 NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_AtomicPixels] PRIMARY KEY ([PixelHash])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107210027_InitialCreate'
)
BEGIN
    CREATE TABLE [AtomicTextTokens] (
        [TokenId] bigint NOT NULL IDENTITY,
        [TokenHash] BINARY(32) NOT NULL,
        [TokenText] NVARCHAR(200) NOT NULL,
        [TokenLength] int NOT NULL,
        [TokenEmbedding] VECTOR(768) NULL,
        [EmbeddingModel] NVARCHAR(100) NULL,
        [VocabId] int NULL,
        [ReferenceCount] BIGINT NOT NULL DEFAULT CAST(0 AS BIGINT),
        [FirstSeen] DATETIME2 NOT NULL DEFAULT (SYSUTCDATETIME()),
        [LastReferenced] DATETIME2 NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_AtomicTextTokens] PRIMARY KEY ([TokenId])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107210027_InitialCreate'
)
BEGIN
    CREATE TABLE [Atoms] (
        [AtomId] bigint NOT NULL IDENTITY,
        [ContentHash] binary(32) NOT NULL,
        [Modality] nvarchar(64) NOT NULL,
        [Subtype] nvarchar(128) NULL,
        [SourceUri] nvarchar(1024) NULL,
        [SourceType] nvarchar(128) NULL,
        [CanonicalText] nvarchar(max) NULL,
        [PayloadLocator] nvarchar(1024) NULL,
        [Metadata] JSON NULL,
        [CreatedAt] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
        [UpdatedAt] datetime2 NULL,
        [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
        [ReferenceCount] bigint NOT NULL DEFAULT CAST(0 AS bigint),
        [SpatialKey] geometry NULL,
        [ComponentStream] varbinary(max) NULL,
        CONSTRAINT [PK_Atoms] PRIMARY KEY ([AtomId])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107210027_InitialCreate'
)
BEGIN
    CREATE TABLE [dbo].[AudioData] (
        [AudioId] bigint NOT NULL IDENTITY,
        [SourcePath] nvarchar(500) NULL,
        [SampleRate] int NOT NULL,
        [DurationMs] bigint NOT NULL,
        [NumChannels] tinyint NOT NULL,
        [Format] nvarchar(20) NULL,
        [Spectrogram] GEOMETRY NULL,
        [MelSpectrogram] GEOMETRY NULL,
        [WaveformLeft] GEOMETRY NULL,
        [WaveformRight] GEOMETRY NULL,
        [GlobalEmbedding] VECTOR(768) NULL,
        [GlobalEmbeddingDim] int NULL,
        [Metadata] JSON NULL,
        [IngestionDate] datetime2 NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_AudioData] PRIMARY KEY ([AudioId])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107210027_InitialCreate'
)
BEGIN
    CREATE TABLE [AutonomousImprovementHistory] (
        [ImprovementId] uniqueidentifier NOT NULL DEFAULT (NEWID()),
        [AnalysisResults] nvarchar(max) NOT NULL,
        [GeneratedCode] nvarchar(max) NOT NULL,
        [TargetFile] nvarchar(512) NOT NULL,
        [ChangeType] nvarchar(50) NOT NULL,
        [RiskLevel] nvarchar(20) NOT NULL,
        [EstimatedImpact] nvarchar(20) NULL,
        [GitCommitHash] nvarchar(64) NULL,
        [SuccessScore] decimal(5,4) NULL,
        [TestsPassed] int NULL,
        [TestsFailed] int NULL,
        [PerformanceDelta] decimal(10,4) NULL,
        [ErrorMessage] nvarchar(max) NULL,
        [WasDeployed] bit NOT NULL,
        [WasRolledBack] bit NOT NULL,
        [StartedAt] datetime2 NOT NULL,
        [CompletedAt] datetime2 NULL,
        [RolledBackAt] datetime2 NULL,
        CONSTRAINT [PK_AutonomousImprovementHistory] PRIMARY KEY ([ImprovementId]),
        CONSTRAINT [CK_AutonomousImprovement_SuccessScore] CHECK ([SuccessScore] >= 0 AND [SuccessScore] <= 1)
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107210027_InitialCreate'
)
BEGIN
    CREATE TABLE [BillingRatePlans] (
        [RatePlanId] uniqueidentifier NOT NULL DEFAULT (NEWSEQUENTIALID()),
        [TenantId] nvarchar(64) NULL,
        [PlanCode] nvarchar(64) NOT NULL DEFAULT N'',
        [Name] nvarchar(128) NOT NULL DEFAULT N'',
        [Description] nvarchar(256) NULL,
        [DefaultRate] decimal(18,6) NOT NULL DEFAULT 0.01,
        [MonthlyFee] decimal(18,2) NOT NULL DEFAULT 0.0,
        [UnitPricePerDcu] decimal(18,6) NOT NULL DEFAULT 0.00008,
        [IncludedPublicStorageGb] decimal(18,2) NOT NULL DEFAULT 0.0,
        [IncludedPrivateStorageGb] decimal(18,2) NOT NULL DEFAULT 0.0,
        [IncludedSeatCount] int NOT NULL DEFAULT 1,
        [AllowsPrivateData] bit NOT NULL DEFAULT CAST(0 AS bit),
        [CanQueryPublicCorpus] bit NOT NULL DEFAULT CAST(0 AS bit),
        [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
        [CreatedUtc] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
        [UpdatedUtc] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_BillingRatePlans] PRIMARY KEY ([RatePlanId])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107210027_InitialCreate'
)
BEGIN
    CREATE TABLE [BillingUsageLedger] (
        [LedgerId] bigint NOT NULL IDENTITY,
        [TenantId] nvarchar(128) NOT NULL,
        [PrincipalId] nvarchar(256) NOT NULL,
        [Operation] nvarchar(128) NOT NULL,
        [MessageType] nvarchar(128) NULL,
        [Handler] nvarchar(256) NULL,
        [Units] decimal(18,6) NOT NULL,
        [BaseRate] decimal(18,6) NOT NULL,
        [Multiplier] decimal(18,6) NOT NULL DEFAULT 1.0,
        [TotalCost] decimal(18,6) NOT NULL,
        [MetadataJson] nvarchar(max) NULL,
        [TimestampUtc] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_BillingUsageLedger] PRIMARY KEY ([LedgerId])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107210027_InitialCreate'
)
BEGIN
    CREATE TABLE [dbo].[CodeAtoms] (
        [CodeAtomId] bigint NOT NULL IDENTITY,
        [Language] nvarchar(50) NOT NULL,
        [Code] TEXT NOT NULL,
        [Framework] nvarchar(200) NULL,
        [Description] nvarchar(2000) NULL,
        [CodeType] nvarchar(100) NULL,
        [Embedding] geometry NULL,
        [EmbeddingDimension] int NULL,
        [TestResults] JSON NULL,
        [QualityScore] real NULL,
        [UsageCount] int NOT NULL DEFAULT 0,
        [CodeHash] varbinary(32) NULL,
        [SourceUri] nvarchar(2048) NULL,
        [Tags] JSON NULL,
        [CreatedAt] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
        [UpdatedAt] datetime2 NULL,
        [CreatedBy] nvarchar(200) NULL,
        CONSTRAINT [PK_CodeAtoms] PRIMARY KEY ([CodeAtomId])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107210027_InitialCreate'
)
BEGIN
    CREATE TABLE [DeduplicationPolicies] (
        [DeduplicationPolicyId] int NOT NULL IDENTITY,
        [PolicyName] nvarchar(128) NOT NULL,
        [SemanticThreshold] float NULL,
        [SpatialThreshold] float NULL,
        [Metadata] JSON NULL,
        [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
        [CreatedAt] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_DeduplicationPolicies] PRIMARY KEY ([DeduplicationPolicyId])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107210027_InitialCreate'
)
BEGIN
    CREATE TABLE [dbo].[Images] (
        [ImageId] bigint NOT NULL IDENTITY,
        [SourcePath] nvarchar(500) NULL,
        [SourceUrl] nvarchar(1000) NULL,
        [Width] int NOT NULL,
        [Height] int NOT NULL,
        [Channels] int NOT NULL,
        [Format] nvarchar(20) NULL,
        [PixelCloud] GEOMETRY NULL,
        [EdgeMap] GEOMETRY NULL,
        [ObjectRegions] GEOMETRY NULL,
        [SaliencyRegions] GEOMETRY NULL,
        [GlobalEmbedding] VECTOR(1536) NULL,
        [GlobalEmbeddingDim] int NULL,
        [Metadata] JSON NULL,
        [IngestionDate] datetime2 NULL DEFAULT (SYSUTCDATETIME()),
        [LastAccessed] datetime2 NULL,
        [AccessCount] bigint NOT NULL DEFAULT CAST(0 AS bigint),
        CONSTRAINT [PK_Images] PRIMARY KEY ([ImageId])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107210027_InitialCreate'
)
BEGIN
    CREATE TABLE [IngestionJobs] (
        [IngestionJobId] bigint NOT NULL IDENTITY,
        [PipelineName] nvarchar(256) NOT NULL,
        [StartedAt] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CompletedAt] datetime2 NULL,
        [Status] nvarchar(64) NULL,
        [SourceUri] nvarchar(1024) NULL,
        [Metadata] JSON NULL,
        CONSTRAINT [PK_IngestionJobs] PRIMARY KEY ([IngestionJobId])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107210027_InitialCreate'
)
BEGIN
    CREATE TABLE [Models] (
        [ModelId] int NOT NULL IDENTITY,
        [ModelName] nvarchar(200) NOT NULL,
        [ModelType] nvarchar(100) NOT NULL,
        [Architecture] nvarchar(100) NULL,
        [Config] JSON NULL,
        [ParameterCount] bigint NULL,
        [IngestionDate] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
        [LastUsed] datetime2 NULL,
        [UsageCount] bigint NOT NULL DEFAULT CAST(0 AS bigint),
        [AverageInferenceMs] float NULL,
        CONSTRAINT [PK_Models] PRIMARY KEY ([ModelId])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107210027_InitialCreate'
)
BEGIN
    CREATE TABLE [TenantSecurityPolicy] (
        [PolicyId] int NOT NULL IDENTITY,
        [TenantId] nvarchar(128) NOT NULL,
        [PolicyName] nvarchar(100) NOT NULL,
        [PolicyType] nvarchar(50) NOT NULL,
        [PolicyRules] nvarchar(max) NOT NULL,
        [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
        [EffectiveFrom] datetime2 NULL,
        [EffectiveTo] datetime2 NULL,
        [CreatedUtc] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
        [UpdatedUtc] datetime2 NULL,
        [CreatedBy] nvarchar(256) NULL,
        [UpdatedBy] nvarchar(256) NULL,
        CONSTRAINT [PK_TenantSecurityPolicy] PRIMARY KEY ([PolicyId])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107210027_InitialCreate'
)
BEGIN
    CREATE TABLE [TestResults] (
        [TestResultId] bigint NOT NULL IDENTITY,
        [TestName] nvarchar(200) NOT NULL,
        [TestSuite] nvarchar(100) NOT NULL,
        [TestStatus] nvarchar(50) NOT NULL,
        [ExecutionTimeMs] float NULL,
        [ErrorMessage] nvarchar(max) NULL,
        [StackTrace] nvarchar(max) NULL,
        [TestOutput] nvarchar(max) NULL,
        [ExecutedAt] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
        [Environment] nvarchar(100) NULL,
        [TestCategory] nvarchar(50) NULL,
        [MemoryUsageMB] float NULL,
        [CpuUsagePercent] float NULL,
        CONSTRAINT [PK_TestResults] PRIMARY KEY ([TestResultId])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107210027_InitialCreate'
)
BEGIN
    CREATE TABLE [dbo].[TextDocuments] (
        [DocId] bigint NOT NULL IDENTITY,
        [SourcePath] nvarchar(500) NULL,
        [SourceUrl] nvarchar(1000) NULL,
        [RawText] nvarchar(max) NOT NULL,
        [Language] nvarchar(10) NULL,
        [CharCount] int NULL,
        [WordCount] int NULL,
        [GlobalEmbedding] VECTOR(768) NULL,
        [GlobalEmbeddingDim] int NULL,
        [TopicVector] VECTOR(100) NULL,
        [SentimentScore] real NULL,
        [Toxicity] real NULL,
        [Metadata] JSON NULL,
        [IngestionDate] datetime2 NULL DEFAULT (SYSUTCDATETIME()),
        [LastAccessed] datetime2 NULL,
        [AccessCount] bigint NOT NULL DEFAULT CAST(0 AS bigint),
        CONSTRAINT [PK_TextDocuments] PRIMARY KEY ([DocId])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107210027_InitialCreate'
)
BEGIN
    CREATE TABLE [dbo].[Videos] (
        [VideoId] bigint NOT NULL IDENTITY,
        [SourcePath] nvarchar(500) NULL,
        [Fps] int NOT NULL,
        [DurationMs] bigint NOT NULL,
        [ResolutionWidth] int NOT NULL,
        [ResolutionHeight] int NOT NULL,
        [NumFrames] bigint NOT NULL,
        [Format] nvarchar(20) NULL,
        [GlobalEmbedding] VECTOR(768) NULL,
        [GlobalEmbeddingDim] int NULL,
        [Metadata] JSON NULL,
        [IngestionDate] datetime2 NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_Videos] PRIMARY KEY ([VideoId])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107210027_InitialCreate'
)
BEGIN
    CREATE TABLE [graph].[AtomGraphNodes] (
        [NodeId] bigint NOT NULL IDENTITY,
        [AtomId] bigint NOT NULL,
        [NodeType] nvarchar(100) NOT NULL,
        [NodeLabel] nvarchar(500) NULL,
        [Properties] nvarchar(max) NULL,
        [CreatedUtc] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
        [UpdatedUtc] datetime2 NULL,
        CONSTRAINT [PK_AtomGraphNodes] PRIMARY KEY ([NodeId]),
        CONSTRAINT [FK_AtomGraphNodes_Atoms_AtomId] FOREIGN KEY ([AtomId]) REFERENCES [Atoms] ([AtomId]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107210027_InitialCreate'
)
BEGIN
    CREATE TABLE [AtomPayloadStore] (
        [PayloadId] bigint NOT NULL IDENTITY,
        [RowGuid] uniqueidentifier NOT NULL DEFAULT (NEWSEQUENTIALID()),
        [AtomId] bigint NOT NULL,
        [ContentType] nvarchar(256) NOT NULL,
        [ContentHash] binary(32) NOT NULL,
        [SizeBytes] bigint NOT NULL,
        [PayloadData] VARBINARY(MAX) FILESTREAM NOT NULL,
        [CreatedBy] nvarchar(256) NULL,
        [CreatedUtc] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_AtomPayloadStore] PRIMARY KEY ([PayloadId]),
        CONSTRAINT [CK_AtomPayloadStore_ContentType] CHECK ([ContentType] LIKE '%/%'),
        CONSTRAINT [CK_AtomPayloadStore_SizeBytes] CHECK ([SizeBytes] > 0),
        CONSTRAINT [FK_AtomPayloadStore_Atoms_AtomId] FOREIGN KEY ([AtomId]) REFERENCES [Atoms] ([AtomId]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107210027_InitialCreate'
)
BEGIN
    CREATE TABLE [AtomRelations] (
        [AtomRelationId] bigint NOT NULL IDENTITY,
        [SourceAtomId] bigint NOT NULL,
        [TargetAtomId] bigint NOT NULL,
        [RelationType] nvarchar(128) NOT NULL,
        [Weight] real NULL,
        [SpatialExpression] geometry NULL,
        [Metadata] JSON NULL,
        [CreatedAt] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_AtomRelations] PRIMARY KEY ([AtomRelationId]),
        CONSTRAINT [FK_AtomRelations_Atoms_SourceAtomId] FOREIGN KEY ([SourceAtomId]) REFERENCES [Atoms] ([AtomId]),
        CONSTRAINT [FK_AtomRelations_Atoms_TargetAtomId] FOREIGN KEY ([TargetAtomId]) REFERENCES [Atoms] ([AtomId])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107210027_InitialCreate'
)
BEGIN
    CREATE TABLE [dbo].[AudioFrames] (
        [AudioId] bigint NOT NULL,
        [FrameNumber] bigint NOT NULL,
        [TimestampMs] bigint NOT NULL,
        [AmplitudeL] real NULL,
        [AmplitudeR] real NULL,
        [SpectralCentroid] real NULL,
        [SpectralRolloff] real NULL,
        [ZeroCrossingRate] real NULL,
        [RmsEnergy] real NULL,
        [Mfcc] VECTOR(13) NULL,
        [FrameEmbedding] VECTOR(768) NULL,
        CONSTRAINT [PK_AudioFrames] PRIMARY KEY ([AudioId], [FrameNumber]),
        CONSTRAINT [FK_AudioFrames_AudioData_AudioId] FOREIGN KEY ([AudioId]) REFERENCES [dbo].[AudioData] ([AudioId]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107210027_InitialCreate'
)
BEGIN
    CREATE TABLE [BillingMultipliers] (
        [MultiplierId] uniqueidentifier NOT NULL DEFAULT (NEWSEQUENTIALID()),
        [RatePlanId] uniqueidentifier NOT NULL,
        [Dimension] nvarchar(32) NOT NULL DEFAULT N'',
        [Key] nvarchar(128) NOT NULL DEFAULT N'',
        [Multiplier] decimal(18,6) NOT NULL,
        [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
        [CreatedUtc] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
        [UpdatedUtc] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_BillingMultipliers] PRIMARY KEY ([MultiplierId]),
        CONSTRAINT [FK_BillingMultipliers_BillingRatePlans_RatePlanId] FOREIGN KEY ([RatePlanId]) REFERENCES [BillingRatePlans] ([RatePlanId]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107210027_InitialCreate'
)
BEGIN
    CREATE TABLE [BillingOperationRates] (
        [OperationRateId] uniqueidentifier NOT NULL DEFAULT (NEWSEQUENTIALID()),
        [RatePlanId] uniqueidentifier NOT NULL,
        [Operation] nvarchar(128) NOT NULL DEFAULT N'',
        [UnitOfMeasure] nvarchar(64) NOT NULL DEFAULT N'',
        [Category] nvarchar(64) NULL,
        [Description] nvarchar(256) NULL,
        [Rate] decimal(18,6) NOT NULL,
        [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
        [CreatedUtc] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
        [UpdatedUtc] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_BillingOperationRates] PRIMARY KEY ([OperationRateId]),
        CONSTRAINT [FK_BillingOperationRates_BillingRatePlans_RatePlanId] FOREIGN KEY ([RatePlanId]) REFERENCES [BillingRatePlans] ([RatePlanId]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107210027_InitialCreate'
)
BEGIN
    CREATE TABLE [dbo].[ImagePatches] (
        [PatchId] bigint NOT NULL IDENTITY,
        [ImageId] bigint NOT NULL,
        [PatchX] int NOT NULL,
        [PatchY] int NOT NULL,
        [PatchWidth] int NOT NULL,
        [PatchHeight] int NOT NULL,
        [PatchRegion] GEOMETRY NOT NULL,
        [PatchEmbedding] VECTOR(768) NULL,
        [DominantColor] GEOMETRY NULL,
        [MeanIntensity] real NULL,
        [StdIntensity] real NULL,
        CONSTRAINT [PK_ImagePatches] PRIMARY KEY ([PatchId]),
        CONSTRAINT [FK_ImagePatches_Images_ImageId] FOREIGN KEY ([ImageId]) REFERENCES [dbo].[Images] ([ImageId]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107210027_InitialCreate'
)
BEGIN
    CREATE TABLE [IngestionJobAtoms] (
        [IngestionJobAtomId] bigint NOT NULL IDENTITY,
        [IngestionJobId] bigint NOT NULL,
        [AtomId] bigint NOT NULL,
        [WasDuplicate] bit NOT NULL,
        [Notes] nvarchar(1024) NULL,
        CONSTRAINT [PK_IngestionJobAtoms] PRIMARY KEY ([IngestionJobAtomId]),
        CONSTRAINT [FK_IngestionJobAtoms_Atoms_AtomId] FOREIGN KEY ([AtomId]) REFERENCES [Atoms] ([AtomId]),
        CONSTRAINT [FK_IngestionJobAtoms_IngestionJobs_IngestionJobId] FOREIGN KEY ([IngestionJobId]) REFERENCES [IngestionJobs] ([IngestionJobId]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107210027_InitialCreate'
)
BEGIN
    CREATE TABLE [AtomEmbeddings] (
        [AtomEmbeddingId] bigint NOT NULL IDENTITY,
        [AtomId] bigint NOT NULL,
        [ModelId] int NULL,
        [EmbeddingType] nvarchar(128) NOT NULL,
        [Dimension] int NOT NULL DEFAULT 0,
        [EmbeddingVector] VECTOR(1998) NULL,
        [UsesMaxDimensionPadding] bit NOT NULL DEFAULT CAST(0 AS bit),
        [SpatialProjX] float NULL,
        [SpatialProjY] float NULL,
        [SpatialProjZ] float NULL,
        [SpatialGeometry] geometry NULL,
        [SpatialCoarse] geometry NULL,
        [SpatialBucketX] int NULL,
        [SpatialBucketY] int NULL,
        [SpatialBucketZ] int NOT NULL DEFAULT -2147483648,
        [Metadata] JSON NULL,
        [CreatedAt] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_AtomEmbeddings] PRIMARY KEY ([AtomEmbeddingId]),
        CONSTRAINT [FK_AtomEmbeddings_Atoms_AtomId] FOREIGN KEY ([AtomId]) REFERENCES [Atoms] ([AtomId]) ON DELETE CASCADE,
        CONSTRAINT [FK_AtomEmbeddings_Models_ModelId] FOREIGN KEY ([ModelId]) REFERENCES [Models] ([ModelId])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107210027_InitialCreate'
)
BEGIN
    CREATE TABLE [provenance].[Concepts] (
        [ConceptId] bigint NOT NULL IDENTITY,
        [ConceptName] nvarchar(200) NOT NULL,
        [Description] nvarchar(max) NULL,
        [CentroidVector] varbinary(max) NOT NULL,
        [VectorDimension] int NOT NULL,
        [MemberCount] int NOT NULL DEFAULT 0,
        [CoherenceScore] float NULL,
        [SeparationScore] float NULL,
        [DiscoveryMethod] nvarchar(100) NOT NULL,
        [ModelId] int NOT NULL,
        [DiscoveredAt] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
        [LastUpdatedAt] datetime2 NULL,
        [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
        CONSTRAINT [PK_Concepts] PRIMARY KEY ([ConceptId]),
        CONSTRAINT [FK_Concepts_Models_ModelId] FOREIGN KEY ([ModelId]) REFERENCES [Models] ([ModelId]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107210027_InitialCreate'
)
BEGIN
    CREATE TABLE [provenance].[GenerationStreams] (
        [StreamId] uniqueidentifier NOT NULL,
        [GenerationStreamId] bigint NOT NULL IDENTITY,
        [ModelId] int NULL,
        [Scope] nvarchar(128) NULL,
        [Model] nvarchar(128) NULL,
        [GeneratedAtomIds] nvarchar(max) NULL,
        [ProvenanceStream] varbinary(max) NULL,
        [ContextMetadata] nvarchar(max) NULL,
        [TenantId] int NOT NULL DEFAULT 0,
        [CreatedUtc] datetime2(3) NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_GenerationStreams] PRIMARY KEY ([StreamId]),
        CONSTRAINT [FK_GenerationStreams_Models] FOREIGN KEY ([ModelId]) REFERENCES [Models] ([ModelId])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107210027_InitialCreate'
)
BEGIN
    CREATE TABLE [InferenceCache] (
        [CacheId] bigint NOT NULL IDENTITY,
        [CacheKey] nvarchar(64) NOT NULL,
        [ModelId] int NOT NULL,
        [InferenceType] nvarchar(100) NOT NULL,
        [InputHash] varbinary(max) NOT NULL,
        [OutputData] varbinary(max) NOT NULL,
        [IntermediateStates] varbinary(max) NULL,
        [CreatedUtc] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
        [LastAccessedUtc] datetime2 NULL,
        [AccessCount] bigint NOT NULL DEFAULT CAST(0 AS bigint),
        [SizeBytes] bigint NULL,
        [ComputeTimeMs] float NULL,
        CONSTRAINT [PK_InferenceCache] PRIMARY KEY ([CacheId]),
        CONSTRAINT [FK_InferenceCache_Models_ModelId] FOREIGN KEY ([ModelId]) REFERENCES [Models] ([ModelId]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107210027_InitialCreate'
)
BEGIN
    CREATE TABLE [InferenceRequests] (
        [InferenceId] bigint NOT NULL IDENTITY,
        [RequestTimestamp] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
        [TaskType] nvarchar(50) NULL,
        [InputData] JSON NULL,
        [InputHash] binary(32) NULL,
        [CorrelationId] nvarchar(max) NULL,
        [Status] nvarchar(max) NULL,
        [Confidence] float NULL,
        [ModelsUsed] JSON NULL,
        [EnsembleStrategy] nvarchar(50) NULL,
        [OutputData] JSON NULL,
        [OutputMetadata] JSON NULL,
        [TotalDurationMs] int NULL,
        [CacheHit] bit NOT NULL DEFAULT CAST(0 AS bit),
        [UserRating] tinyint NULL,
        [UserFeedback] nvarchar(max) NULL,
        [Complexity] int NULL,
        [SlaTier] nvarchar(50) NULL,
        [EstimatedResponseTimeMs] int NULL,
        [ModelId] int NULL,
        CONSTRAINT [PK_InferenceRequests] PRIMARY KEY ([InferenceId]),
        CONSTRAINT [FK_InferenceRequests_Models_ModelId] FOREIGN KEY ([ModelId]) REFERENCES [Models] ([ModelId])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107210027_InitialCreate'
)
BEGIN
    CREATE TABLE [ModelLayers] (
        [LayerId] bigint NOT NULL IDENTITY,
        [ModelId] int NOT NULL,
        [LayerIdx] int NOT NULL,
        [LayerName] nvarchar(100) NULL,
        [LayerType] nvarchar(50) NULL,
        [WeightsGeometry] geometry NULL,
        [TensorShape] NVARCHAR(200) NULL,
        [TensorDtype] nvarchar(20) NULL DEFAULT N'float32',
        [QuantizationType] nvarchar(20) NULL,
        [QuantizationScale] float NULL,
        [QuantizationZeroPoint] float NULL,
        [Parameters] JSON NULL,
        [ParameterCount] bigint NULL,
        [ZMin] float NULL,
        [ZMax] float NULL,
        [MMin] float NULL,
        [MMax] float NULL,
        [MortonCode] bigint NULL,
        [PreviewPointCount] int NULL,
        [CacheHitRate] float NULL DEFAULT 0.0E0,
        [AvgComputeTimeMs] float NULL,
        [LayerAtomId] bigint NULL,
        CONSTRAINT [PK_ModelLayers] PRIMARY KEY ([LayerId]),
        CONSTRAINT [FK_ModelLayers_Atoms_LayerAtomId] FOREIGN KEY ([LayerAtomId]) REFERENCES [Atoms] ([AtomId]) ON DELETE SET NULL,
        CONSTRAINT [FK_ModelLayers_Models_ModelId] FOREIGN KEY ([ModelId]) REFERENCES [Models] ([ModelId]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107210027_InitialCreate'
)
BEGIN
    CREATE TABLE [ModelMetadata] (
        [MetadataId] int NOT NULL IDENTITY,
        [ModelId] int NOT NULL,
        [SupportedTasks] JSON NULL,
        [SupportedModalities] JSON NULL,
        [MaxInputLength] int NULL,
        [MaxOutputLength] int NULL,
        [EmbeddingDimension] int NULL,
        [PerformanceMetrics] JSON NULL,
        [TrainingDataset] nvarchar(500) NULL,
        [TrainingDate] date NULL,
        [License] nvarchar(100) NULL,
        [SourceUrl] nvarchar(500) NULL,
        CONSTRAINT [PK_ModelMetadata] PRIMARY KEY ([MetadataId]),
        CONSTRAINT [FK_ModelMetadata_Models_ModelId] FOREIGN KEY ([ModelId]) REFERENCES [Models] ([ModelId]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107210027_InitialCreate'
)
BEGIN
    CREATE TABLE [TokenVocabulary] (
        [VocabId] bigint NOT NULL IDENTITY,
        [ModelId] int NOT NULL,
        [Token] nvarchar(100) NOT NULL,
        [TokenId] int NOT NULL,
        [TokenType] nvarchar(20) NULL,
        [Embedding] VECTOR(768) NULL,
        [EmbeddingDim] int NULL,
        [Frequency] bigint NOT NULL DEFAULT CAST(0 AS bigint),
        [LastUsed] datetime2 NULL,
        CONSTRAINT [PK_TokenVocabulary] PRIMARY KEY ([VocabId]),
        CONSTRAINT [FK_TokenVocabulary_Models_ModelId] FOREIGN KEY ([ModelId]) REFERENCES [Models] ([ModelId]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107210027_InitialCreate'
)
BEGIN
    CREATE TABLE [dbo].[VideoFrames] (
        [FrameId] bigint NOT NULL IDENTITY,
        [VideoId] bigint NOT NULL,
        [FrameNumber] bigint NOT NULL,
        [TimestampMs] bigint NOT NULL,
        [PixelCloud] GEOMETRY NULL,
        [ObjectRegions] GEOMETRY NULL,
        [MotionVectors] GEOMETRY NULL,
        [OpticalFlow] GEOMETRY NULL,
        [FrameEmbedding] VECTOR(768) NULL,
        [PerceptualHash] varbinary(8) NULL,
        CONSTRAINT [PK_VideoFrames] PRIMARY KEY ([FrameId]),
        CONSTRAINT [FK_VideoFrames_Videos_VideoId] FOREIGN KEY ([VideoId]) REFERENCES [dbo].[Videos] ([VideoId]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107210027_InitialCreate'
)
BEGIN
    CREATE TABLE [AtomEmbeddingComponents] (
        [AtomEmbeddingComponentId] bigint NOT NULL IDENTITY,
        [AtomEmbeddingId] bigint NOT NULL,
        [ComponentIndex] int NOT NULL,
        [ComponentValue] real NOT NULL,
        CONSTRAINT [PK_AtomEmbeddingComponents] PRIMARY KEY ([AtomEmbeddingComponentId]),
        CONSTRAINT [FK_AtomEmbeddingComponents_AtomEmbeddings_AtomEmbeddingId] FOREIGN KEY ([AtomEmbeddingId]) REFERENCES [AtomEmbeddings] ([AtomEmbeddingId]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107210027_InitialCreate'
)
BEGIN
    CREATE TABLE [InferenceSteps] (
        [StepId] bigint NOT NULL IDENTITY,
        [InferenceId] bigint NOT NULL,
        [StepNumber] int NOT NULL,
        [ModelId] int NULL,
        [LayerId] bigint NULL,
        [OperationType] nvarchar(50) NULL,
        [QueryText] nvarchar(max) NULL,
        [IndexUsed] nvarchar(200) NULL,
        [RowsExamined] bigint NULL,
        [RowsReturned] bigint NULL,
        [DurationMs] int NULL,
        [CacheUsed] bit NOT NULL DEFAULT CAST(0 AS bit),
        CONSTRAINT [PK_InferenceSteps] PRIMARY KEY ([StepId]),
        CONSTRAINT [FK_InferenceSteps_InferenceRequests_InferenceId] FOREIGN KEY ([InferenceId]) REFERENCES [InferenceRequests] ([InferenceId]) ON DELETE CASCADE,
        CONSTRAINT [FK_InferenceSteps_Models_ModelId] FOREIGN KEY ([ModelId]) REFERENCES [Models] ([ModelId]) ON DELETE SET NULL
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107210027_InitialCreate'
)
BEGIN
    CREATE TABLE [CachedActivations] (
        [CacheId] bigint NOT NULL IDENTITY,
        [ModelId] int NOT NULL,
        [LayerId] bigint NOT NULL,
        [InputHash] binary(32) NOT NULL,
        [ActivationOutput] VECTOR(1998) NULL,
        [OutputShape] nvarchar(100) NULL,
        [HitCount] bigint NOT NULL DEFAULT CAST(0 AS bigint),
        [CreatedDate] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
        [LastAccessed] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
        [ComputeTimeSavedMs] bigint NOT NULL DEFAULT CAST(0 AS bigint),
        CONSTRAINT [PK_CachedActivations] PRIMARY KEY ([CacheId]),
        CONSTRAINT [FK_CachedActivations_ModelLayers_LayerId] FOREIGN KEY ([LayerId]) REFERENCES [ModelLayers] ([LayerId]) ON DELETE CASCADE,
        CONSTRAINT [FK_CachedActivations_Models_ModelId] FOREIGN KEY ([ModelId]) REFERENCES [Models] ([ModelId])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107210027_InitialCreate'
)
BEGIN
    CREATE TABLE [LayerTensorSegments] (
        [LayerTensorSegmentId] bigint NOT NULL IDENTITY,
        [LayerId] bigint NOT NULL,
        [SegmentOrdinal] int NOT NULL,
        [PointOffset] bigint NOT NULL,
        [PointCount] int NOT NULL,
        [QuantizationType] nvarchar(20) NOT NULL,
        [QuantizationScale] float NULL,
        [QuantizationZeroPoint] float NULL,
        [ZMin] float NULL,
        [ZMax] float NULL,
        [MMin] float NULL,
        [MMax] float NULL,
        [MortonCode] bigint NULL,
        [GeometryFootprint] geometry NULL,
        [RawPayload] VARBINARY(MAX) FILESTREAM NOT NULL,
        [PayloadRowGuid] uniqueidentifier ROWGUIDCOL NOT NULL DEFAULT (NEWSEQUENTIALID()),
        [CreatedAt] DATETIME2 NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_LayerTensorSegments] PRIMARY KEY ([LayerTensorSegmentId]),
        CONSTRAINT [UX_LayerTensorSegments_PayloadRowGuid] UNIQUE ([PayloadRowGuid]),
        CONSTRAINT [FK_LayerTensorSegments_ModelLayers_LayerId] FOREIGN KEY ([LayerId]) REFERENCES [ModelLayers] ([LayerId]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107210027_InitialCreate'
)
BEGIN
    CREATE TABLE [TensorAtoms] (
        [TensorAtomId] bigint NOT NULL IDENTITY,
        [AtomId] bigint NOT NULL,
        [ModelId] int NULL,
        [LayerId] bigint NULL,
        [AtomType] nvarchar(128) NOT NULL,
        [SpatialSignature] geometry NULL,
        [GeometryFootprint] geometry NULL,
        [Metadata] JSON NULL,
        [ImportanceScore] real NULL,
        [CreatedAt] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_TensorAtoms] PRIMARY KEY ([TensorAtomId]),
        CONSTRAINT [FK_TensorAtoms_Atoms_AtomId] FOREIGN KEY ([AtomId]) REFERENCES [Atoms] ([AtomId]) ON DELETE CASCADE,
        CONSTRAINT [FK_TensorAtoms_ModelLayers_LayerId] FOREIGN KEY ([LayerId]) REFERENCES [ModelLayers] ([LayerId]),
        CONSTRAINT [FK_TensorAtoms_Models_ModelId] FOREIGN KEY ([ModelId]) REFERENCES [Models] ([ModelId])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107210027_InitialCreate'
)
BEGIN
    CREATE TABLE [TensorAtomCoefficients] (
        [TensorAtomCoefficientId] bigint NOT NULL IDENTITY,
        [TensorAtomId] bigint NOT NULL,
        [ParentLayerId] bigint NOT NULL,
        [TensorRole] nvarchar(128) NULL,
        [Coefficient] real NOT NULL,
        CONSTRAINT [PK_TensorAtomCoefficients] PRIMARY KEY ([TensorAtomCoefficientId]),
        CONSTRAINT [FK_TensorAtomCoefficients_ModelLayers_ParentLayerId] FOREIGN KEY ([ParentLayerId]) REFERENCES [ModelLayers] ([LayerId]) ON DELETE CASCADE,
        CONSTRAINT [FK_TensorAtomCoefficients_TensorAtoms_TensorAtomId] FOREIGN KEY ([TensorAtomId]) REFERENCES [TensorAtoms] ([TensorAtomId]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107210027_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [UX_AtomEmbeddingComponents_Embedding_Index] ON [AtomEmbeddingComponents] ([AtomEmbeddingId], [ComponentIndex]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107210027_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AtomEmbeddings_Atom_Model_Type] ON [AtomEmbeddings] ([AtomId], [EmbeddingType], [ModelId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107210027_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AtomEmbeddings_ModelId] ON [AtomEmbeddings] ([ModelId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107210027_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AtomEmbeddings_SpatialBucket] ON [AtomEmbeddings] ([SpatialBucketX], [SpatialBucketY], [SpatialBucketZ]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107210027_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AtomGraphEdges_CreatedUtc] ON [graph].[AtomGraphEdges] ([CreatedUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107210027_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AtomGraphEdges_EdgeType] ON [graph].[AtomGraphEdges] ([EdgeType]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107210027_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AtomGraphEdges_Weight] ON [graph].[AtomGraphEdges] ([Weight]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107210027_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AtomGraphNodes_AtomId] ON [graph].[AtomGraphNodes] ([AtomId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107210027_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AtomGraphNodes_CreatedUtc] ON [graph].[AtomGraphNodes] ([CreatedUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107210027_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AtomGraphNodes_NodeType] ON [graph].[AtomGraphNodes] ([NodeType]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107210027_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AtomicAudioSamples_AmplitudeNormalized] ON [AtomicAudioSamples] ([AmplitudeNormalized]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107210027_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_AtomicTextTokens_TokenHash] ON [AtomicTextTokens] ([TokenHash]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107210027_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_AtomicTextTokens_TokenText] ON [AtomicTextTokens] ([TokenText]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107210027_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AtomPayloadStore_AtomId] ON [AtomPayloadStore] ([AtomId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107210027_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AtomPayloadStore_RowGuid] ON [AtomPayloadStore] ([RowGuid]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107210027_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [UX_AtomPayloadStore_ContentHash] ON [AtomPayloadStore] ([ContentHash]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107210027_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AtomRelations_Source_Target_Type] ON [AtomRelations] ([SourceAtomId], [TargetAtomId], [RelationType]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107210027_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AtomRelations_TargetAtomId] ON [AtomRelations] ([TargetAtomId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107210027_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [UX_Atoms_ContentHash] ON [Atoms] ([ContentHash]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107210027_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AudioData_DurationMs] ON [dbo].[AudioData] ([DurationMs]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107210027_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AudioData_IngestionDate] ON [dbo].[AudioData] ([IngestionDate] DESC);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107210027_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AutonomousImprovement_ChangeType_RiskLevel] ON [AutonomousImprovementHistory] ([ChangeType], [RiskLevel]) INCLUDE ([ErrorMessage], [SuccessScore]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107210027_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AutonomousImprovement_StartedAt] ON [AutonomousImprovementHistory] ([StartedAt] DESC);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107210027_InitialCreate'
)
BEGIN
    EXEC(N'CREATE INDEX [IX_AutonomousImprovement_SuccessScore] ON [AutonomousImprovementHistory] ([SuccessScore] DESC) WHERE [WasDeployed] = 1 AND [WasRolledBack] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107210027_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_BillingMultipliers_Active] ON [BillingMultipliers] ([RatePlanId], [Dimension], [Key]) WHERE [IsActive] = 1');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107210027_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_BillingOperationRates_Active] ON [BillingOperationRates] ([RatePlanId], [Operation]) WHERE [IsActive] = 1');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107210027_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_BillingRatePlans_Tenant_IsActive] ON [BillingRatePlans] ([TenantId], [IsActive]) INCLUDE ([UpdatedUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107210027_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_BillingRatePlans_Tenant_PlanCode] ON [BillingRatePlans] ([TenantId], [PlanCode]) WHERE [PlanCode] <> ''''');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107210027_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_BillingUsageLedger_Operation_Timestamp] ON [BillingUsageLedger] ([Operation], [TimestampUtc]) INCLUDE ([TenantId], [Units], [TotalCost]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107210027_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_BillingUsageLedger_TenantId_Timestamp] ON [BillingUsageLedger] ([TenantId], [TimestampUtc]) INCLUDE ([Operation], [TotalCost]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107210027_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_CachedActivations_LastAccessed_HitCount] ON [CachedActivations] ([LastAccessed] DESC, [HitCount] DESC);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107210027_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_CachedActivations_LayerId] ON [CachedActivations] ([LayerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107210027_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_CachedActivations_Model_Layer_InputHash] ON [CachedActivations] ([ModelId], [LayerId], [InputHash]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107210027_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_CodeAtoms_CodeHash] ON [dbo].[CodeAtoms] ([CodeHash]) WHERE [CodeHash] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107210027_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_CodeAtoms_CodeType] ON [dbo].[CodeAtoms] ([CodeType]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107210027_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_CodeAtoms_CreatedAt] ON [dbo].[CodeAtoms] ([CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107210027_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_CodeAtoms_Language] ON [dbo].[CodeAtoms] ([Language]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107210027_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_CodeAtoms_QualityScore] ON [dbo].[CodeAtoms] ([QualityScore]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107210027_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Concepts_CoherenceScore] ON [provenance].[Concepts] ([CoherenceScore] DESC);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107210027_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Concepts_ConceptName] ON [provenance].[Concepts] ([ConceptName]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107210027_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Concepts_DiscoveryMethod] ON [provenance].[Concepts] ([DiscoveryMethod]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107210027_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Concepts_ModelId_IsActive] ON [provenance].[Concepts] ([ModelId], [IsActive]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107210027_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [UX_DeduplicationPolicies_PolicyName] ON [DeduplicationPolicies] ([PolicyName]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107210027_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_GenerationStreams_CreatedUtc] ON [provenance].[GenerationStreams] ([CreatedUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107210027_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_GenerationStreams_GenerationStreamId] ON [provenance].[GenerationStreams] ([GenerationStreamId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107210027_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_GenerationStreams_Model] ON [provenance].[GenerationStreams] ([Model]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107210027_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_GenerationStreams_ModelId] ON [provenance].[GenerationStreams] ([ModelId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107210027_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_GenerationStreams_Scope] ON [provenance].[GenerationStreams] ([Scope]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107210027_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_GenerationStreams_TenantId] ON [provenance].[GenerationStreams] ([TenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107210027_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ImagePatches_ImageId_PatchX_PatchY] ON [dbo].[ImagePatches] ([ImageId], [PatchX], [PatchY]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107210027_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Images_IngestionDate] ON [dbo].[Images] ([IngestionDate] DESC);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107210027_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Images_Width_Height] ON [dbo].[Images] ([Width], [Height]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107210027_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_InferenceCache_CacheKey] ON [InferenceCache] ([CacheKey]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107210027_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_InferenceCache_LastAccessedUtc] ON [InferenceCache] ([LastAccessedUtc] DESC);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107210027_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_InferenceCache_ModelId_InferenceType] ON [InferenceCache] ([ModelId], [InferenceType]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107210027_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_InferenceRequests_CacheHit] ON [InferenceRequests] ([CacheHit]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107210027_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_InferenceRequests_InputHash] ON [InferenceRequests] ([InputHash]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107210027_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_InferenceRequests_ModelId] ON [InferenceRequests] ([ModelId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107210027_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_InferenceRequests_RequestTimestamp] ON [InferenceRequests] ([RequestTimestamp] DESC);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107210027_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_InferenceRequests_TaskType] ON [InferenceRequests] ([TaskType]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107210027_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_InferenceSteps_InferenceId_StepNumber] ON [InferenceSteps] ([InferenceId], [StepNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107210027_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_InferenceSteps_ModelId] ON [InferenceSteps] ([ModelId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107210027_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_IngestionJobAtoms_AtomId] ON [IngestionJobAtoms] ([AtomId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107210027_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_IngestionJobAtoms_Job_Atom] ON [IngestionJobAtoms] ([IngestionJobId], [AtomId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107210027_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_LayerTensorSegments_M_Range] ON [LayerTensorSegments] ([LayerId], [MMin], [MMax]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107210027_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_LayerTensorSegments_Morton] ON [LayerTensorSegments] ([MortonCode]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107210027_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_LayerTensorSegments_Z_Range] ON [LayerTensorSegments] ([LayerId], [ZMin], [ZMax]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107210027_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [UX_LayerTensorSegments_LayerId_SegmentOrdinal] ON [LayerTensorSegments] ([LayerId], [SegmentOrdinal]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107210027_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ModelLayers_LayerAtomId] ON [ModelLayers] ([LayerAtomId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107210027_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ModelLayers_LayerType] ON [ModelLayers] ([LayerType]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107210027_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ModelLayers_M_Range] ON [ModelLayers] ([ModelId], [MMin], [MMax]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107210027_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ModelLayers_ModelId_LayerIdx] ON [ModelLayers] ([ModelId], [LayerIdx]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107210027_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ModelLayers_Morton] ON [ModelLayers] ([MortonCode]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107210027_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ModelLayers_Z_Range] ON [ModelLayers] ([ModelId], [ZMin], [ZMax]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107210027_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ModelMetadata_ModelId] ON [ModelMetadata] ([ModelId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107210027_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Models_ModelName] ON [Models] ([ModelName]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107210027_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Models_ModelType] ON [Models] ([ModelType]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107210027_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_TenantSecurityPolicy_EffectiveDates] ON [TenantSecurityPolicy] ([EffectiveFrom], [EffectiveTo]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107210027_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_TenantSecurityPolicy_IsActive] ON [TenantSecurityPolicy] ([IsActive]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107210027_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_TenantSecurityPolicy_TenantId_PolicyType] ON [TenantSecurityPolicy] ([TenantId], [PolicyType]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107210027_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_TensorAtomCoefficients_Lookup] ON [TensorAtomCoefficients] ([TensorAtomId], [ParentLayerId], [TensorRole]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107210027_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_TensorAtomCoefficients_ParentLayerId] ON [TensorAtomCoefficients] ([ParentLayerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107210027_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_TensorAtoms_AtomId] ON [TensorAtoms] ([AtomId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107210027_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_TensorAtoms_LayerId] ON [TensorAtoms] ([LayerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107210027_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_TensorAtoms_Model_Layer_Type] ON [TensorAtoms] ([ModelId], [LayerId], [AtomType]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107210027_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_TestResults_ExecutionTimeMs] ON [TestResults] ([ExecutionTimeMs] DESC);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107210027_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_TestResults_TestCategory_ExecutedAt] ON [TestResults] ([TestCategory] DESC, [ExecutedAt] DESC);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107210027_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_TestResults_TestStatus] ON [TestResults] ([TestStatus]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107210027_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_TestResults_TestSuite_ExecutedAt] ON [TestResults] ([TestSuite] DESC, [ExecutedAt] DESC);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107210027_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_TokenVocabulary_ModelId_Token] ON [TokenVocabulary] ([ModelId], [Token]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107210027_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_TokenVocabulary_ModelId_TokenId] ON [TokenVocabulary] ([ModelId], [TokenId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107210027_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_VideoFrames_VideoId_FrameNumber] ON [dbo].[VideoFrames] ([VideoId], [FrameNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107210027_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_VideoFrames_VideoId_TimestampMs] ON [dbo].[VideoFrames] ([VideoId], [TimestampMs]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107210027_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Videos_IngestionDate] ON [dbo].[Videos] ([IngestionDate] DESC);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107210027_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Videos_ResolutionWidth_ResolutionHeight] ON [dbo].[Videos] ([ResolutionWidth], [ResolutionHeight]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251107210027_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251107210027_InitialCreate', N'10.0.0-rc.2.25502.107');
END;

COMMIT;
GO

