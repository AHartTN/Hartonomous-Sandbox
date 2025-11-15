using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Hartonomous.Data.Entities;

public partial class HartonomousDbContext : DbContext
{
    public HartonomousDbContext(DbContextOptions<HartonomousDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AgentTool> AgentTools { get; set; }

    public virtual DbSet<Atom> Atoms { get; set; }

    public virtual DbSet<AtomComposition> AtomCompositions { get; set; }

    public virtual DbSet<AtomConcept> AtomConcepts { get; set; }

    public virtual DbSet<AtomEmbedding> AtomEmbeddings { get; set; }

    public virtual DbSet<AtomEmbeddingComponent> AtomEmbeddingComponents { get; set; }

    public virtual DbSet<AtomEmbeddingSpatialMetadatum> AtomEmbeddingSpatialMetadata { get; set; }

    public virtual DbSet<AtomGraphEdge> AtomGraphEdges { get; set; }

    public virtual DbSet<AtomGraphEdge1> AtomGraphEdges1 { get; set; }

    public virtual DbSet<AtomGraphNode> AtomGraphNodes { get; set; }

    public virtual DbSet<AtomPayloadStore> AtomPayloadStores { get; set; }

    public virtual DbSet<AtomRelation> AtomRelations { get; set; }

    public virtual DbSet<AtomicAudioSample> AtomicAudioSamples { get; set; }

    public virtual DbSet<AtomicPixel> AtomicPixels { get; set; }

    public virtual DbSet<AtomicTextToken> AtomicTextTokens { get; set; }

    public virtual DbSet<AtomicWeight> AtomicWeights { get; set; }

    public virtual DbSet<AtomsHistory> AtomsHistories { get; set; }

    public virtual DbSet<AtomsLob> AtomsLobs { get; set; }

    public virtual DbSet<AttentionGenerationLog> AttentionGenerationLogs { get; set; }

    public virtual DbSet<AttentionInferenceResult> AttentionInferenceResults { get; set; }

    public virtual DbSet<AudioDatum> AudioData { get; set; }

    public virtual DbSet<AudioFrame> AudioFrames { get; set; }

    public virtual DbSet<AutonomousComputeJob> AutonomousComputeJobs { get; set; }

    public virtual DbSet<AutonomousImprovementHistory> AutonomousImprovementHistories { get; set; }

    public virtual DbSet<BillingInvoice> BillingInvoices { get; set; }

    public virtual DbSet<BillingMultiplier> BillingMultipliers { get; set; }

    public virtual DbSet<BillingOperationRate> BillingOperationRates { get; set; }

    public virtual DbSet<BillingPricingTier> BillingPricingTiers { get; set; }

    public virtual DbSet<BillingQuotaViolation> BillingQuotaViolations { get; set; }

    public virtual DbSet<BillingRatePlan> BillingRatePlans { get; set; }

    public virtual DbSet<BillingTenantQuota> BillingTenantQuotas { get; set; }

    public virtual DbSet<BillingUsageLedger> BillingUsageLedgers { get; set; }

    public virtual DbSet<BillingUsageLedgerInMemory> BillingUsageLedgerInMemories { get; set; }

    public virtual DbSet<CachedActivation> CachedActivations { get; set; }

    public virtual DbSet<CachedActivationsInMemory> CachedActivationsInMemories { get; set; }

    public virtual DbSet<CdcCheckpoint> CdcCheckpoints { get; set; }

    public virtual DbSet<Cicdbuild> Cicdbuilds { get; set; }

    public virtual DbSet<CodeAtom> CodeAtoms { get; set; }

    public virtual DbSet<Concept> Concepts { get; set; }

    public virtual DbSet<ConceptEvolution> ConceptEvolutions { get; set; }

    public virtual DbSet<DeduplicationPolicy> DeduplicationPolicies { get; set; }

    public virtual DbSet<EmbeddingMigrationProgress> EmbeddingMigrationProgresses { get; set; }

    public virtual DbSet<EventAtom> EventAtoms { get; set; }

    public virtual DbSet<EventGenerationResult> EventGenerationResults { get; set; }

    public virtual DbSet<EventHubCheckpoint> EventHubCheckpoints { get; set; }

    public virtual DbSet<GenerationStream> GenerationStreams { get; set; }

    public virtual DbSet<GenerationStreamSegment> GenerationStreamSegments { get; set; }

    public virtual DbSet<Image> Images { get; set; }

    public virtual DbSet<ImagePatch> ImagePatches { get; set; }

    public virtual DbSet<InferenceCache> InferenceCaches { get; set; }

    public virtual DbSet<InferenceCacheInMemory> InferenceCacheInMemories { get; set; }

    public virtual DbSet<InferenceRequest> InferenceRequests { get; set; }

    public virtual DbSet<InferenceStep> InferenceSteps { get; set; }

    public virtual DbSet<IngestionJob> IngestionJobs { get; set; }

    public virtual DbSet<IngestionJobAtom> IngestionJobAtoms { get; set; }

    public virtual DbSet<LayerTensorSegment> LayerTensorSegments { get; set; }

    public virtual DbSet<Model> Models { get; set; }

    public virtual DbSet<ModelLayer> ModelLayers { get; set; }

    public virtual DbSet<ModelMetadatum> ModelMetadata { get; set; }

    public virtual DbSet<ModelVersionHistory> ModelVersionHistories { get; set; }

    public virtual DbSet<MultiPathReasoning> MultiPathReasonings { get; set; }

    public virtual DbSet<Neo4jSyncLog> Neo4jSyncLogs { get; set; }

    public virtual DbSet<OperationProvenance> OperationProvenances { get; set; }

    public virtual DbSet<PendingAction> PendingActions { get; set; }

    public virtual DbSet<ProvenanceAuditResult> ProvenanceAuditResults { get; set; }

    public virtual DbSet<ProvenanceValidationResult> ProvenanceValidationResults { get; set; }

    public virtual DbSet<ReasoningChain> ReasoningChains { get; set; }

    public virtual DbSet<SelfConsistencyResult> SelfConsistencyResults { get; set; }

    public virtual DbSet<SemanticFeature> SemanticFeatures { get; set; }

    public virtual DbSet<SessionPath> SessionPaths { get; set; }

    public virtual DbSet<SessionPathsInMemory> SessionPathsInMemories { get; set; }

    public virtual DbSet<SpatialLandmark> SpatialLandmarks { get; set; }

    public virtual DbSet<Status> Statuses { get; set; }

    public virtual DbSet<StreamFusionResult> StreamFusionResults { get; set; }

    public virtual DbSet<StreamOrchestrationResult> StreamOrchestrationResults { get; set; }

    public virtual DbSet<TenantAtom> TenantAtoms { get; set; }

    public virtual DbSet<TenantSecurityPolicy> TenantSecurityPolicies { get; set; }

    public virtual DbSet<TensorAtom> TensorAtoms { get; set; }

    public virtual DbSet<TensorAtomCoefficient> TensorAtomCoefficients { get; set; }

    public virtual DbSet<TensorAtomPayload> TensorAtomPayloads { get; set; }

    public virtual DbSet<TestResult> TestResults { get; set; }

    public virtual DbSet<TextDocument> TextDocuments { get; set; }

    public virtual DbSet<TokenVocabulary> TokenVocabularies { get; set; }

    public virtual DbSet<TopicKeyword> TopicKeywords { get; set; }

    public virtual DbSet<TransformerInferenceResult> TransformerInferenceResults { get; set; }

    public virtual DbSet<Video> Videos { get; set; }

    public virtual DbSet<VideoFrame> VideoFrames { get; set; }

    public virtual DbSet<VwAtomsWithLob> VwAtomsWithLobs { get; set; }

    public virtual DbSet<VwCurrentWeight> VwCurrentWeights { get; set; }

    public virtual DbSet<VwEmbeddingVector> VwEmbeddingVectors { get; set; }

    public virtual DbSet<VwWeightChangeHistory> VwWeightChangeHistories { get; set; }

    public virtual DbSet<Weight> Weights { get; set; }

    public virtual DbSet<WeightSnapshot> WeightSnapshots { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AgentTool>(entity =>
        {
            entity.HasKey(e => e.ToolId).HasName("PK__tmp_ms_x__CC0CEB9104E2C62E");

            entity.HasIndex(e => e.ToolName, "UQ__tmp_ms_x__006DA271D25C4318").IsUnique();

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Description).HasMaxLength(2000);
            entity.Property(e => e.IsEnabled).HasDefaultValue(true);
            entity.Property(e => e.ObjectName).HasMaxLength(256);
            entity.Property(e => e.ObjectType).HasMaxLength(128);
            entity.Property(e => e.ParametersJson).HasColumnType("json");
            entity.Property(e => e.ToolCategory).HasMaxLength(100);
            entity.Property(e => e.ToolName).HasMaxLength(200);
        });

        modelBuilder.Entity<Atom>(entity =>
        {
            entity.HasIndex(e => new { e.Modality, e.Subtype }, "IX_Atoms_Modality_Subtype");

            entity.HasIndex(e => e.ReferenceCount, "IX_Atoms_References").IsDescending();

            entity.HasIndex(e => e.SpatialKey, "IX_Atoms_SpatialKey");

            entity.HasIndex(e => new { e.TenantId, e.IsActive, e.IsDeleted }, "IX_Atoms_TenantActive");

            entity.HasIndex(e => e.ContentHash, "UQ_Atoms_ContentHash").IsUnique();

            entity.HasIndex(e => e.ContentHash, "UX_Atoms_ContentHash").IsUnique();

            entity.Property(e => e.AtomicValue).HasMaxLength(64);
            entity.Property(e => e.CanonicalText).HasMaxLength(256);
            entity.Property(e => e.ContentHash)
                .HasMaxLength(32)
                .IsFixedLength();
            entity.Property(e => e.ContentType).HasMaxLength(128);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.CreatedUtc).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Metadata).HasColumnType("json");
            entity.Property(e => e.Modality).HasMaxLength(64);
            entity.Property(e => e.PayloadLocator).HasMaxLength(1024);
            entity.Property(e => e.SourceType).HasMaxLength(128);
            entity.Property(e => e.SourceUri).HasMaxLength(1024);
            entity.Property(e => e.SpatialKey).HasColumnType("geometry");
            entity.Property(e => e.Subtype).HasMaxLength(128);
        });

        modelBuilder.Entity<AtomComposition>(entity =>
        {
            entity.HasKey(e => e.CompositionId);

            entity.HasIndex(e => e.ComponentAtomId, "IX_AtomCompositions_Component");

            entity.HasIndex(e => e.SourceAtomId, "IX_AtomCompositions_Source");

            entity.HasIndex(e => new { e.ComponentType, e.SourceAtomId }, "IX_AtomCompositions_Type");

            entity.HasIndex(e => e.PositionKey, "SIDX_AtomCompositions_Position");

            entity.Property(e => e.ComponentType).HasMaxLength(64);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Metadata).HasColumnType("json");
            entity.Property(e => e.PositionKey).HasColumnType("geometry");

            entity.HasOne(d => d.ComponentAtom).WithMany(p => p.AtomCompositionComponentAtoms)
                .HasForeignKey(d => d.ComponentAtomId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AtomCompositions_Component");

            entity.HasOne(d => d.SourceAtom).WithMany(p => p.AtomCompositionSourceAtoms)
                .HasForeignKey(d => d.SourceAtomId)
                .HasConstraintName("FK_AtomCompositions_Source");
        });

        modelBuilder.Entity<AtomConcept>(entity =>
        {
            entity.ToTable("AtomConcepts", "provenance");

            entity.HasIndex(e => e.ConceptId, "IX_AtomConcepts_ConceptId");

            entity.HasIndex(e => new { e.AtomId, e.ConceptId }, "UX_AtomConcepts_AtomId_ConceptId").IsUnique();

            entity.Property(e => e.AssignedAt).HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.Atom).WithMany(p => p.AtomConcepts)
                .HasForeignKey(d => d.AtomId)
                .HasConstraintName("FK_AtomConcepts_Atoms");

            entity.HasOne(d => d.Concept).WithMany(p => p.AtomConcepts)
                .HasForeignKey(d => d.ConceptId)
                .HasConstraintName("FK_AtomConcepts_Concepts");
        });

        modelBuilder.Entity<AtomEmbedding>(entity =>
        {
            entity.HasIndex(e => e.AtomId, "IX_AtomEmbeddings_Atom");

            entity.HasIndex(e => new { e.AtomId, e.EmbeddingType, e.ModelId }, "IX_AtomEmbeddings_Atom_Model_Type");

            entity.HasIndex(e => e.SpatialBucket, "IX_AtomEmbeddings_Bucket");

            entity.HasIndex(e => new { e.SpatialBucketX, e.SpatialBucketY, e.SpatialBucketZ }, "IX_AtomEmbeddings_BucketXYZ").HasFilter("([SpatialBucketX] IS NOT NULL)");

            entity.HasIndex(e => e.SpatialCoarse, "IX_AtomEmbeddings_Coarse");

            entity.HasIndex(e => e.ModelId, "IX_AtomEmbeddings_ModelId");

            entity.HasIndex(e => e.SpatialGeometry, "IX_AtomEmbeddings_Spatial");

            entity.HasIndex(e => new { e.SpatialBucketX, e.SpatialBucketY, e.SpatialBucketZ }, "IX_AtomEmbeddings_SpatialBucket");

            entity.HasIndex(e => e.SpatialCoarse, "IX_AtomEmbeddings_SpatialCoarse");

            entity.HasIndex(e => e.SpatialGeometry, "IX_AtomEmbeddings_SpatialGeometry");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Dimension).HasDefaultValue(1998);
            entity.Property(e => e.EmbeddingType)
                .HasMaxLength(50)
                .HasDefaultValue("semantic");
            entity.Property(e => e.EmbeddingVector).HasMaxLength(1998);
            entity.Property(e => e.LastUpdated).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Metadata).HasColumnType("json");
            entity.Property(e => e.SpatialCoarse).HasColumnType("geometry");
            entity.Property(e => e.SpatialGeometry).HasColumnType("geometry");
            entity.Property(e => e.SpatialProjection3D).HasColumnType("geometry");

            entity.HasOne(d => d.Atom).WithMany(p => p.AtomEmbeddings)
                .HasForeignKey(d => d.AtomId)
                .HasConstraintName("FK_AtomEmbeddings_Atoms");

            entity.HasOne(d => d.Model).WithMany(p => p.AtomEmbeddings)
                .HasForeignKey(d => d.ModelId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_AtomEmbeddings_Models");
        });

        modelBuilder.Entity<AtomEmbeddingComponent>(entity =>
        {
            entity.HasIndex(e => new { e.AtomEmbeddingId, e.ComponentIndex }, "UX_AtomEmbeddingComponents_Embedding_Index").IsUnique();

            entity.HasOne(d => d.AtomEmbedding).WithMany(p => p.AtomEmbeddingComponents).HasForeignKey(d => d.AtomEmbeddingId);
        });

        modelBuilder.Entity<AtomEmbeddingSpatialMetadatum>(entity =>
        {
            entity.HasKey(e => e.MetadataId);

            entity.HasIndex(e => new { e.SpatialBucketX, e.SpatialBucketY, e.SpatialBucketZ, e.HasZ }, "UX_AtomEmbeddingSpatialMetadata_BucketXYZ").IsUnique();

            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(sysutcdatetime())");
        });

        modelBuilder.Entity<AtomGraphEdge>(entity =>
        {
            entity.HasKey(e => e.AtomRelationId);

            entity.ToTable("AtomGraphEdges", "graph");

            entity.HasIndex(e => e.CreatedAt, "IX_AtomGraphEdges_CreatedAt");

            entity.HasIndex(e => e.RelationType, "IX_AtomGraphEdges_RelationType");

            entity.HasIndex(e => e.RelationType, "IX_AtomGraphEdges_Type");

            entity.HasIndex(e => e.Weight, "IX_AtomGraphEdges_Weight");

            entity.HasIndex(e => e.SpatialExpression, "SIX_AtomGraphEdges_SpatialExpression");

            entity.HasIndex(e => e.AtomRelationId, "UX_AtomGraphEdges_AtomRelationId").IsUnique();

            entity.Property(e => e.AtomRelationId).ValueGeneratedNever();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.EdgeIdEe9be59b11634b148dba809bd1d99150)
                .HasMaxLength(1000)
                .HasColumnName("$edge_id_EE9BE59B11634B148DBA809BD1D99150");
            entity.Property(e => e.FromId1e22d35020c54c1da39133f04210fc27).HasColumnName("from_id_1E22D35020C54C1DA39133F04210FC27");
            entity.Property(e => e.FromId607e9fcfb54c4a409ab7bdc29b63f086)
                .HasMaxLength(1000)
                .HasColumnName("$from_id_607E9FCFB54C4A409AB7BDC29B63F086");
            entity.Property(e => e.FromObjIdDb1d84b22b074350a65c32aba4c92a17).HasColumnName("from_obj_id_DB1D84B22B074350A65C32ABA4C92A17");
            entity.Property(e => e.GraphIdC0374105abf94d8690a28e3930c45799).HasColumnName("graph_id_C0374105ABF94D8690A28E3930C45799");
            entity.Property(e => e.Metadata).HasColumnType("json");
            entity.Property(e => e.RelationType).HasMaxLength(128);
            entity.Property(e => e.SpatialExpression).HasColumnType("geometry");
            entity.Property(e => e.ToIdD48121dd004c41b0b88470dfc6226c34)
                .HasMaxLength(1000)
                .HasColumnName("$to_id_D48121DD004C41B0B88470DFC6226C34");
            entity.Property(e => e.ToIdFfbd0b6425204fd2a03bd072f908138d).HasColumnName("to_id_FFBD0B6425204FD2A03BD072F908138D");
            entity.Property(e => e.ToObjId4f26395fa31342c986b9312a47cc5f26).HasColumnName("to_obj_id_4F26395FA31342C986B9312A47CC5F26");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(sysutcdatetime())");
        });

        modelBuilder.Entity<AtomGraphEdge1>(entity =>
        {
            entity.HasKey(e => e.EdgeId).HasName("PK__AtomGrap__DD62104679B8D843");

            entity.ToTable("AtomGraphEdges", "provenance");

            entity.HasIndex(e => e.DependencyType, "IX_AtomGraphEdges_DependencyType");

            entity.HasIndex(e => e.FromAtomId, "IX_AtomGraphEdges_FromId");

            entity.HasIndex(e => e.ToAtomId, "IX_AtomGraphEdges_ToId");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.DependencyType).HasMaxLength(50);
            entity.Property(e => e.EdgeType).HasMaxLength(50);
        });

        modelBuilder.Entity<AtomGraphNode>(entity =>
        {
            entity.HasKey(e => e.AtomId);

            entity.ToTable("AtomGraphNodes", "graph");

            entity.HasIndex(e => e.AtomId, "IX_AtomGraphNodes_AtomId");

            entity.HasIndex(e => e.CreatedAt, "IX_AtomGraphNodes_CreatedAt");

            entity.HasIndex(e => new { e.Modality, e.Subtype }, "IX_AtomGraphNodes_Modality");

            entity.HasIndex(e => new { e.Modality, e.Subtype }, "IX_AtomGraphNodes_Modality_Subtype");

            entity.HasIndex(e => e.SpatialKey, "SIX_AtomGraphNodes_SpatialKey");

            entity.HasIndex(e => e.AtomId, "UX_AtomGraphNodes_AtomId").IsUnique();

            entity.Property(e => e.AtomId).ValueGeneratedNever();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.GraphId1ed5587659f24875930e33ed4194a3a6).HasColumnName("graph_id_1ED5587659F24875930E33ED4194A3A6");
            entity.Property(e => e.Metadata).HasColumnType("json");
            entity.Property(e => e.Modality).HasMaxLength(64);
            entity.Property(e => e.NodeIdA4600067acf04785ae52bb0b40c7d43b)
                .HasMaxLength(1000)
                .HasColumnName("$node_id_A4600067ACF04785AE52BB0B40C7D43B");
            entity.Property(e => e.PayloadLocator).HasMaxLength(512);
            entity.Property(e => e.Semantics).HasColumnType("json");
            entity.Property(e => e.SourceType).HasMaxLength(128);
            entity.Property(e => e.SourceUri).HasMaxLength(2048);
            entity.Property(e => e.SpatialKey).HasColumnType("geometry");
            entity.Property(e => e.Subtype).HasMaxLength(64);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(sysutcdatetime())");
        });

        modelBuilder.Entity<AtomPayloadStore>(entity =>
        {
            entity.HasKey(e => e.PayloadId);

            entity.ToTable("AtomPayloadStore");

            entity.HasIndex(e => e.AtomId, "IX_AtomPayloadStore_AtomId");

            entity.HasIndex(e => e.RowGuid, "IX_AtomPayloadStore_RowGuid");

            entity.HasIndex(e => e.RowGuid, "UQ_AtomPayloadStore_RowGuid").IsUnique();

            entity.HasIndex(e => e.ContentHash, "UX_AtomPayloadStore_ContentHash").IsUnique();

            entity.Property(e => e.ContentHash)
                .HasMaxLength(32)
                .IsFixedLength();
            entity.Property(e => e.ContentType).HasMaxLength(256);
            entity.Property(e => e.CreatedBy).HasMaxLength(256);
            entity.Property(e => e.CreatedUtc).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.RowGuid).HasDefaultValueSql("(newsequentialid())");

            entity.HasOne(d => d.Atom).WithMany(p => p.AtomPayloadStores).HasForeignKey(d => d.AtomId);
        });

        modelBuilder.Entity<AtomRelation>(entity =>
        {
            entity.ToTable(tb => tb.IsTemporal(ttb =>
                    {
                        ttb.UseHistoryTable("AtomRelations_History", "dbo");
                        ttb
                            .HasPeriodStart("ValidFrom")
                            .HasColumnName("ValidFrom");
                        ttb
                            .HasPeriodEnd("ValidTo")
                            .HasColumnName("ValidTo");
                    }));

            entity.HasIndex(e => e.RelationType, "IX_AtomRelations_RelationType");

            entity.HasIndex(e => new { e.SourceAtomId, e.SequenceIndex }, "IX_AtomRelations_SequenceIndex");

            entity.HasIndex(e => new { e.SourceAtomId, e.TargetAtomId }, "IX_AtomRelations_SourceTarget");

            entity.HasIndex(e => new { e.SourceAtomId, e.TargetAtomId, e.RelationType }, "IX_AtomRelations_Source_Target_Type");

            entity.HasIndex(e => e.SpatialBucket, "IX_AtomRelations_SpatialBucket");

            entity.HasIndex(e => e.TargetAtomId, "IX_AtomRelations_TargetAtomId");

            entity.HasIndex(e => new { e.TargetAtomId, e.SourceAtomId }, "IX_AtomRelations_TargetSource");

            entity.HasIndex(e => new { e.TenantId, e.RelationType }, "IX_AtomRelations_Tenant");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Metadata).HasColumnType("json");
            entity.Property(e => e.RelationType).HasMaxLength(128);
            entity.Property(e => e.SpatialExpression).HasColumnType("geometry");

            entity.HasOne(d => d.SourceAtom).WithMany(p => p.AtomRelationSourceAtoms)
                .HasForeignKey(d => d.SourceAtomId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.TargetAtom).WithMany(p => p.AtomRelationTargetAtoms)
                .HasForeignKey(d => d.TargetAtomId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<AtomicAudioSample>(entity =>
        {
            entity.HasKey(e => e.SampleHash);

            entity.HasIndex(e => e.AmplitudeInt16, "IX_AtomicAudioSamples_Amplitude");

            entity.HasIndex(e => e.AmplitudeNormalized, "IX_AtomicAudioSamples_AmplitudeNormalized");

            entity.HasIndex(e => e.ReferenceCount, "IX_AtomicAudioSamples_References").IsDescending();

            entity.HasIndex(e => e.AmplitudePoint, "SIDX_AtomicAudioSamples_Amplitude");

            entity.Property(e => e.SampleHash)
                .HasMaxLength(32)
                .IsFixedLength();
            entity.Property(e => e.AmplitudePoint).HasColumnType("geometry");
            entity.Property(e => e.FirstSeen).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.LastReferenced).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.SampleBytes).HasMaxLength(2);
        });

        modelBuilder.Entity<AtomicPixel>(entity =>
        {
            entity.HasKey(e => e.PixelHash);

            entity.HasIndex(e => new { e.R, e.G, e.B }, "IX_AtomicPixels_RGB");

            entity.HasIndex(e => e.ReferenceCount, "IX_AtomicPixels_References").IsDescending();

            entity.HasIndex(e => e.ColorPoint, "SIDX_AtomicPixels_ColorSpace");

            entity.Property(e => e.PixelHash)
                .HasMaxLength(32)
                .IsFixedLength();
            entity.Property(e => e.A).HasDefaultValue((byte)255);
            entity.Property(e => e.ColorPoint).HasColumnType("geometry");
            entity.Property(e => e.FirstSeen).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.LastReferenced).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.RgbaBytes).HasMaxLength(4);
        });

        modelBuilder.Entity<AtomicTextToken>(entity =>
        {
            entity.HasKey(e => e.TokenId);

            entity.HasIndex(e => e.TokenHash, "IX_AtomicTextTokens_TokenHash").IsUnique();

            entity.HasIndex(e => e.TokenText, "IX_AtomicTextTokens_TokenText").IsUnique();

            entity.Property(e => e.EmbeddingModel).HasMaxLength(100);
            entity.Property(e => e.FirstSeen).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.LastReferenced).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.TokenEmbedding).HasMaxLength(1998);
            entity.Property(e => e.TokenHash)
                .HasMaxLength(32)
                .IsFixedLength();
            entity.Property(e => e.TokenText).HasMaxLength(200);
        });

        modelBuilder.Entity<AtomicWeight>(entity =>
        {
            entity.HasKey(e => e.WeightHash);

            entity.HasIndex(e => e.ReferenceCount, "IX_AtomicWeights_References").IsDescending();

            entity.HasIndex(e => e.WeightValue, "IX_AtomicWeights_Value");

            entity.HasIndex(e => e.ValuePoint, "SIDX_AtomicWeights_Value");

            entity.Property(e => e.WeightHash)
                .HasMaxLength(32)
                .IsFixedLength();
            entity.Property(e => e.FirstSeen).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.LastReferenced).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.ValuePoint).HasColumnType("geometry");
            entity.Property(e => e.WeightBytes).HasMaxLength(4);
        });

        modelBuilder.Entity<AtomsHistory>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("AtomsHistory", tb => tb.HasComment("Temporal history table for Atoms. Stores all historical versions with nonclustered columnstore for compression and fast analytics. Spatial columns excluded from columnstore due to type restrictions."));

            entity.HasIndex(e => new { e.ContentHash, e.ValidFrom, e.ValidTo }, "IX_AtomsHistory_ContentHash");

            entity.HasIndex(e => new { e.ValidFrom, e.ValidTo }, "IX_AtomsHistory_Period").IsClustered();

            entity.Property(e => e.AtomicValue).HasMaxLength(64);
            entity.Property(e => e.CanonicalText).HasMaxLength(256);
            entity.Property(e => e.ContentHash)
                .HasMaxLength(32)
                .IsFixedLength();
            entity.Property(e => e.ContentType).HasMaxLength(128);
            entity.Property(e => e.Modality).HasMaxLength(64);
            entity.Property(e => e.SourceType).HasMaxLength(128);
            entity.Property(e => e.SourceUri).HasMaxLength(1024);
            entity.Property(e => e.SpatialKey).HasColumnType("geometry");
            entity.Property(e => e.Subtype).HasMaxLength(128);
        });

        modelBuilder.Entity<AtomsLob>(entity =>
        {
            entity.HasKey(e => e.AtomId);

            entity.ToTable("AtomsLOB", tb => tb.HasComment("Large object storage for Atoms table. Separates LOBs to disk to enable Atoms memory-optimization."));

            entity.HasIndex(e => e.PayloadLocator, "IX_AtomsLOB_PayloadLocator").HasFilter("([PayloadLocator] IS NOT NULL)");

            entity.Property(e => e.AtomId)
                .ValueGeneratedNever()
                .HasComment("Foreign key to Atoms.AtomId. CASCADE DELETE ensures no orphaned LOBs.");
            entity.Property(e => e.ComponentStream).HasComment("Binary payload for multimedia content (images, audio, video). Stored as VARBINARY(MAX).");
            entity.Property(e => e.Content).HasComment("Full text content for documents, articles, transcripts. Stored as NVARCHAR(MAX) for full-text indexing.");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Metadata)
                .HasComment("Extended JSON metadata. Native JSON type for SQL Server 2025.")
                .HasColumnType("json");
            entity.Property(e => e.PayloadLocator)
                .HasMaxLength(1024)
                .HasComment("Azure Blob Storage URL for offloaded large content. Enables hybrid storage (hot metadata in SQL, cold payload in blob).");

            entity.HasOne(d => d.Atom).WithOne(p => p.AtomsLob)
                .HasForeignKey<AtomsLob>(d => d.AtomId)
                .HasConstraintName("FK_AtomsLOB_Atoms");
        });

        modelBuilder.Entity<AttentionGenerationLog>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Attentio__3214EC077FC50932");

            entity.ToTable("AttentionGenerationLog");

            entity.HasIndex(e => e.CreatedAt, "IX_AttentionGenerationLog_CreatedAt").IsDescending();

            entity.HasIndex(e => e.GenerationStreamId, "IX_AttentionGenerationLog_GenerationStreamId");

            entity.HasIndex(e => e.ModelId, "IX_AttentionGenerationLog_ModelId");

            entity.Property(e => e.ContextJson).HasColumnType("json");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.GeneratedAtomIds).HasColumnType("json");
            entity.Property(e => e.InputAtomIds).HasColumnType("json");

            entity.HasOne(d => d.Model).WithMany(p => p.AttentionGenerationLogs)
                .HasForeignKey(d => d.ModelId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AttentionGenerationLog_Models");
        });

        modelBuilder.Entity<AttentionInferenceResult>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Attentio__3214EC076FD837B0");

            entity.HasIndex(e => e.CreatedAt, "IX_AttentionInferenceResults_CreatedAt").IsDescending();

            entity.HasIndex(e => e.ProblemId, "IX_AttentionInferenceResults_ProblemId");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.ReasoningSteps).HasColumnType("json");

            entity.HasOne(d => d.Model).WithMany(p => p.AttentionInferenceResults)
                .HasForeignKey(d => d.ModelId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AttentionInferenceResults_Models");
        });

        modelBuilder.Entity<AudioDatum>(entity =>
        {
            entity.HasKey(e => e.AudioId);

            entity.HasIndex(e => e.DurationMs, "IX_AudioData_DurationMs");

            entity.HasIndex(e => e.IngestionDate, "IX_AudioData_IngestionDate").IsDescending();

            entity.HasIndex(e => e.Spectrogram, "IX_AudioData_Spectrogram");

            entity.Property(e => e.Format).HasMaxLength(20);
            entity.Property(e => e.GlobalEmbedding).HasMaxLength(1998);
            entity.Property(e => e.IngestionDate).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.MelSpectrogram).HasColumnType("geometry");
            entity.Property(e => e.Metadata).HasColumnType("json");
            entity.Property(e => e.SourcePath).HasMaxLength(500);
            entity.Property(e => e.Spectrogram).HasColumnType("geometry");
            entity.Property(e => e.WaveformLeft).HasColumnType("geometry");
            entity.Property(e => e.WaveformRight).HasColumnType("geometry");
        });

        modelBuilder.Entity<AudioFrame>(entity =>
        {
            entity.HasKey(e => new { e.AudioId, e.FrameNumber });

            entity.Property(e => e.FrameEmbedding).HasMaxLength(1998);
            entity.Property(e => e.WaveformGeometry).HasColumnType("geometry");

            entity.HasOne(d => d.Audio).WithMany(p => p.AudioFrames).HasForeignKey(d => d.AudioId);

            entity.HasOne(d => d.ParentAtom).WithMany(p => p.AudioFrames)
                .HasForeignKey(d => d.ParentAtomId)
                .HasConstraintName("FK_AudioFrames_Atoms");
        });

        modelBuilder.Entity<AutonomousComputeJob>(entity =>
        {
            entity.HasKey(e => e.JobId);

            entity.HasIndex(e => e.JobType, "IX_AutonomousComputeJobs_JobType");

            entity.HasIndex(e => new { e.Status, e.CreatedAt }, "IX_AutonomousComputeJobs_Status_CreatedAt");

            entity.Property(e => e.JobId).HasDefaultValueSql("(newid())");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.CurrentState).HasColumnType("json");
            entity.Property(e => e.JobParameters).HasColumnType("json");
            entity.Property(e => e.JobType).HasMaxLength(100);
            entity.Property(e => e.Results).HasColumnType("json");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasDefaultValue("Pending");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(sysutcdatetime())");
        });

        modelBuilder.Entity<AutonomousImprovementHistory>(entity =>
        {
            entity.HasKey(e => e.ImprovementId);

            entity.ToTable("AutonomousImprovementHistory");

            entity.HasIndex(e => new { e.ChangeType, e.RiskLevel }, "IX_AutonomousImprovement_ChangeType_RiskLevel");

            entity.HasIndex(e => e.StartedAt, "IX_AutonomousImprovement_StartedAt").IsDescending();

            entity.HasIndex(e => e.SuccessScore, "IX_AutonomousImprovement_SuccessScore")
                .IsDescending()
                .HasFilter("([WasDeployed]=(1) AND [WasRolledBack]=(0))");

            entity.Property(e => e.ImprovementId).HasDefaultValueSql("(newid())");
            entity.Property(e => e.ChangeType).HasMaxLength(50);
            entity.Property(e => e.EstimatedImpact).HasMaxLength(20);
            entity.Property(e => e.GitCommitHash).HasMaxLength(64);
            entity.Property(e => e.PerformanceDelta).HasColumnType("decimal(10, 4)");
            entity.Property(e => e.RiskLevel).HasMaxLength(20);
            entity.Property(e => e.SuccessScore).HasColumnType("decimal(5, 4)");
            entity.Property(e => e.TargetFile).HasMaxLength(512);
        });

        modelBuilder.Entity<BillingInvoice>(entity =>
        {
            entity.HasKey(e => e.InvoiceId);

            entity.HasIndex(e => new { e.Status, e.GeneratedUtc }, "IX_BillingInvoices_Status").IsDescending(false, true);

            entity.HasIndex(e => new { e.TenantId, e.GeneratedUtc }, "IX_BillingInvoices_Tenant").IsDescending(false, true);

            entity.HasIndex(e => e.InvoiceNumber, "UQ_BillingInvoices_Number").IsUnique();

            entity.Property(e => e.Discount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.GeneratedUtc).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.InvoiceNumber).HasMaxLength(100);
            entity.Property(e => e.Status).HasMaxLength(50);
            entity.Property(e => e.Subtotal).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Tax).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Total).HasColumnType("decimal(18, 2)");
        });

        modelBuilder.Entity<BillingMultiplier>(entity =>
        {
            entity.HasKey(e => e.MultiplierId);

            entity.HasIndex(e => new { e.RatePlanId, e.Dimension, e.Key }, "UX_BillingMultipliers_Active")
                .IsUnique()
                .HasFilter("([IsActive]=(1))");

            entity.Property(e => e.MultiplierId).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.CreatedUtc).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Dimension)
                .HasMaxLength(32)
                .HasDefaultValue("");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Key)
                .HasMaxLength(128)
                .HasDefaultValue("");
            entity.Property(e => e.Multiplier).HasColumnType("decimal(18, 6)");
            entity.Property(e => e.UpdatedUtc).HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.RatePlan).WithMany(p => p.BillingMultipliers).HasForeignKey(d => d.RatePlanId);
        });

        modelBuilder.Entity<BillingOperationRate>(entity =>
        {
            entity.HasKey(e => e.OperationRateId);

            entity.HasIndex(e => new { e.RatePlanId, e.Operation }, "UX_BillingOperationRates_Active")
                .IsUnique()
                .HasFilter("([IsActive]=(1))");

            entity.Property(e => e.OperationRateId).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.Category).HasMaxLength(64);
            entity.Property(e => e.CreatedUtc).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Description).HasMaxLength(256);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Operation)
                .HasMaxLength(128)
                .HasDefaultValue("");
            entity.Property(e => e.Rate).HasColumnType("decimal(18, 6)");
            entity.Property(e => e.UnitOfMeasure)
                .HasMaxLength(64)
                .HasDefaultValue("");
            entity.Property(e => e.UpdatedUtc).HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.RatePlan).WithMany(p => p.BillingOperationRates).HasForeignKey(d => d.RatePlanId);
        });

        modelBuilder.Entity<BillingPricingTier>(entity =>
        {
            entity.HasKey(e => e.TierId);

            entity.HasIndex(e => new { e.UsageType, e.UnitType, e.EffectiveFrom }, "IX_BillingPricingTiers_UsageType").IsDescending(false, false, true);

            entity.Property(e => e.CreatedUtc).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(18, 8)");
            entity.Property(e => e.UnitType).HasMaxLength(50);
            entity.Property(e => e.UsageType).HasMaxLength(50);
        });

        modelBuilder.Entity<BillingQuotaViolation>(entity =>
        {
            entity.HasKey(e => e.ViolationId);

            entity.HasIndex(e => new { e.TenantId, e.ViolatedUtc }, "IX_BillingQuotaViolations_Tenant").IsDescending(false, true);

            entity.HasIndex(e => e.Resolved, "IX_BillingQuotaViolations_Unresolved").HasFilter("([Resolved]=(0))");

            entity.Property(e => e.UsageType).HasMaxLength(50);
            entity.Property(e => e.ViolatedUtc).HasDefaultValueSql("(sysutcdatetime())");
        });

        modelBuilder.Entity<BillingRatePlan>(entity =>
        {
            entity.HasKey(e => e.RatePlanId);

            entity.HasIndex(e => new { e.TenantId, e.IsActive }, "IX_BillingRatePlans_Tenant_IsActive");

            entity.HasIndex(e => new { e.TenantId, e.PlanCode }, "UX_BillingRatePlans_Tenant_PlanCode")
                .IsUnique()
                .HasFilter("([PlanCode]<>'')");

            entity.Property(e => e.RatePlanId).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.CreatedUtc).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.DefaultRate)
                .HasDefaultValue(0.01m)
                .HasColumnType("decimal(18, 6)");
            entity.Property(e => e.Description).HasMaxLength(256);
            entity.Property(e => e.IncludedPrivateStorageGb).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.IncludedPublicStorageGb).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.IncludedSeatCount).HasDefaultValue(1);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.MonthlyFee).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Name)
                .HasMaxLength(128)
                .HasDefaultValue("");
            entity.Property(e => e.PlanCode)
                .HasMaxLength(64)
                .HasDefaultValue("");
            entity.Property(e => e.TenantId).HasMaxLength(64);
            entity.Property(e => e.UnitPricePerDcu)
                .HasDefaultValue(0.00008m)
                .HasColumnType("decimal(18, 6)");
            entity.Property(e => e.UpdatedUtc).HasDefaultValueSql("(sysutcdatetime())");
        });

        modelBuilder.Entity<BillingTenantQuota>(entity =>
        {
            entity.HasKey(e => e.QuotaId);

            entity.HasIndex(e => new { e.TenantId, e.UsageType, e.IsActive }, "IX_BillingTenantQuotas_Tenant");

            entity.Property(e => e.CreatedUtc).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.ResetInterval).HasMaxLength(20);
            entity.Property(e => e.UpdatedUtc).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.UsageType).HasMaxLength(50);
        });

        modelBuilder.Entity<BillingUsageLedger>(entity =>
        {
            entity.HasKey(e => e.LedgerId);

            entity.ToTable("BillingUsageLedger");

            entity.HasIndex(e => new { e.Operation, e.TimestampUtc }, "IX_BillingUsageLedger_Operation_Timestamp");

            entity.HasIndex(e => new { e.TenantId, e.TimestampUtc }, "IX_BillingUsageLedger_Tenant").IsDescending(false, true);

            entity.HasIndex(e => new { e.TenantId, e.TimestampUtc }, "IX_BillingUsageLedger_TenantId_Timestamp");

            entity.HasIndex(e => new { e.UsageType, e.RecordedUtc }, "IX_BillingUsageLedger_UsageType").IsDescending(false, true);

            entity.Property(e => e.BaseRate).HasColumnType("decimal(18, 6)");
            entity.Property(e => e.CostPerUnit).HasColumnType("decimal(18, 8)");
            entity.Property(e => e.Handler).HasMaxLength(256);
            entity.Property(e => e.MessageType).HasMaxLength(128);
            entity.Property(e => e.Multiplier)
                .HasDefaultValue(1.0m)
                .HasColumnType("decimal(18, 6)");
            entity.Property(e => e.Operation).HasMaxLength(128);
            entity.Property(e => e.PrincipalId).HasMaxLength(256);
            entity.Property(e => e.TenantId).HasMaxLength(128);
            entity.Property(e => e.TimestampUtc).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.TotalCost).HasColumnType("decimal(18, 6)");
            entity.Property(e => e.UnitType).HasMaxLength(50);
            entity.Property(e => e.Units).HasColumnType("decimal(18, 6)");
            entity.Property(e => e.UsageType).HasMaxLength(50);
        });

        modelBuilder.Entity<BillingUsageLedgerInMemory>(entity =>
        {
            entity.HasKey(e => e.LedgerId).IsClustered(false);

            entity.ToTable("BillingUsageLedger_InMemory", t => t.IsMemoryOptimized());

            entity.HasIndex(e => e.TenantId, "IX_TenantId_Hash");

            entity.HasIndex(e => e.TimestampUtc, "IX_Timestamp_Range").IsDescending();

            entity.Property(e => e.BaseRate).HasColumnType("decimal(18, 6)");
            entity.Property(e => e.Handler)
                .HasMaxLength(256)
                .UseCollation("Latin1_General_100_BIN2");
            entity.Property(e => e.MessageType)
                .HasMaxLength(128)
                .UseCollation("Latin1_General_100_BIN2");
            entity.Property(e => e.MetadataJson).UseCollation("Latin1_General_100_BIN2");
            entity.Property(e => e.Multiplier)
                .HasDefaultValue(1.0m)
                .HasColumnType("decimal(18, 6)");
            entity.Property(e => e.Operation)
                .HasMaxLength(128)
                .UseCollation("Latin1_General_100_BIN2");
            entity.Property(e => e.PrincipalId)
                .HasMaxLength(256)
                .UseCollation("Latin1_General_100_BIN2");
            entity.Property(e => e.TenantId)
                .HasMaxLength(128)
                .UseCollation("Latin1_General_100_BIN2");
            entity.Property(e => e.TimestampUtc).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.TotalCost).HasColumnType("decimal(18, 6)");
            entity.Property(e => e.Units).HasColumnType("decimal(18, 6)");
        });

        modelBuilder.Entity<CachedActivation>(entity =>
        {
            entity.HasKey(e => e.CacheId);

            entity.HasIndex(e => new { e.LastAccessed, e.HitCount }, "IX_CachedActivations_LastAccessed_HitCount").IsDescending();

            entity.HasIndex(e => e.LayerId, "IX_CachedActivations_LayerId");

            entity.HasIndex(e => new { e.ModelId, e.LayerId, e.InputHash }, "IX_CachedActivations_Model_Layer_InputHash").IsUnique();

            entity.Property(e => e.CreatedDate).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.InputHash)
                .HasMaxLength(32)
                .IsFixedLength();
            entity.Property(e => e.LastAccessed).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.OutputShape).HasMaxLength(100);

            entity.HasOne(d => d.Layer).WithMany(p => p.CachedActivations).HasForeignKey(d => d.LayerId);

            entity.HasOne(d => d.Model).WithMany(p => p.CachedActivations)
                .HasForeignKey(d => d.ModelId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<CachedActivationsInMemory>(entity =>
        {
            entity.HasKey(e => e.CacheId).IsClustered(false);

            entity.ToTable("CachedActivations_InMemory", t => t.IsMemoryOptimized());

            entity.HasIndex(e => e.LastAccessed, "IX_LastAccessed_Range");

            entity.HasIndex(e => new { e.LayerId, e.InputHash }, "IX_LayerInput_Hash");

            entity.HasIndex(e => e.ModelId, "IX_ModelId_Hash");

            entity.Property(e => e.CreatedDate).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.InputHash)
                .HasMaxLength(32)
                .IsFixedLength();
            entity.Property(e => e.LastAccessed).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.OutputShape)
                .HasMaxLength(100)
                .UseCollation("Latin1_General_100_BIN2");
        });

        modelBuilder.Entity<CdcCheckpoint>(entity =>
        {
            entity.HasKey(e => new { e.ConsumerGroup, e.PartitionId });

            entity.Property(e => e.ConsumerGroup).HasMaxLength(100);
            entity.Property(e => e.PartitionId).HasMaxLength(50);
            entity.Property(e => e.LastModified).HasDefaultValueSql("(sysutcdatetime())", "DF_CdcCheckpoints_LastModified");
        });

        modelBuilder.Entity<Cicdbuild>(entity =>
        {
            entity.HasKey(e => e.BuildId);

            entity.ToTable("CICDBuilds");

            entity.HasIndex(e => e.CommitHash, "IX_CICDBuilds_CommitHash");

            entity.HasIndex(e => e.CreatedAt, "IX_CICDBuilds_CreatedAt").IsDescending();

            entity.HasIndex(e => new { e.Status, e.StartedAt }, "IX_CICDBuilds_Status_StartedAt").IsDescending(false, true);

            entity.Property(e => e.ArtifactUrl).HasMaxLength(500);
            entity.Property(e => e.BranchName).HasMaxLength(255);
            entity.Property(e => e.BuildAgent).HasMaxLength(255);
            entity.Property(e => e.BuildNumber).HasMaxLength(50);
            entity.Property(e => e.CodeCoverage).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.CommitHash).HasMaxLength(40);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())", "DF_CICDBuilds_CreatedAt");
            entity.Property(e => e.DeploymentStatus).HasMaxLength(50);
            entity.Property(e => e.Status).HasMaxLength(50);
            entity.Property(e => e.TriggerType).HasMaxLength(50);
        });

        modelBuilder.Entity<CodeAtom>(entity =>
        {
            entity.HasIndex(e => e.CodeHash, "IX_CodeAtoms_CodeHash")
                .IsUnique()
                .HasFilter("([CodeHash] IS NOT NULL)");

            entity.HasIndex(e => e.CodeType, "IX_CodeAtoms_CodeType");

            entity.HasIndex(e => e.CreatedAt, "IX_CodeAtoms_CreatedAt");

            entity.HasIndex(e => e.Embedding, "IX_CodeAtoms_Embedding");

            entity.HasIndex(e => e.Language, "IX_CodeAtoms_Language");

            entity.HasIndex(e => e.QualityScore, "IX_CodeAtoms_QualityScore");

            entity.Property(e => e.Code).HasColumnType("text");
            entity.Property(e => e.CodeHash).HasMaxLength(32);
            entity.Property(e => e.CodeType).HasMaxLength(100);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.CreatedBy).HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(2000);
            entity.Property(e => e.Embedding).HasColumnType("geometry");
            entity.Property(e => e.Framework).HasMaxLength(200);
            entity.Property(e => e.Language).HasMaxLength(50);
            entity.Property(e => e.SourceUri).HasMaxLength(2048);
            entity.Property(e => e.Tags).HasColumnType("json");
            entity.Property(e => e.TestResults).HasColumnType("json");
        });

        modelBuilder.Entity<Concept>(entity =>
        {
            entity.ToTable("Concepts", "provenance");

            entity.HasIndex(e => e.CoherenceScore, "IX_Concepts_CoherenceScore").IsDescending();

            entity.HasIndex(e => e.ConceptName, "IX_Concepts_ConceptName");

            entity.HasIndex(e => e.DiscoveryMethod, "IX_Concepts_DiscoveryMethod");

            entity.HasIndex(e => new { e.ModelId, e.IsActive }, "IX_Concepts_ModelId_IsActive");

            entity.Property(e => e.ConceptName).HasMaxLength(200);
            entity.Property(e => e.DiscoveredAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.DiscoveryMethod).HasMaxLength(100);
            entity.Property(e => e.IsActive).HasDefaultValue(true);

            entity.HasOne(d => d.Model).WithMany(p => p.Concepts).HasForeignKey(d => d.ModelId);
        });

        modelBuilder.Entity<ConceptEvolution>(entity =>
        {
            entity.HasKey(e => e.EvolutionId);

            entity.ToTable("ConceptEvolution", "provenance");

            entity.HasIndex(e => new { e.ConceptId, e.RecordedAt }, "IX_ConceptEvolution_ConceptId_RecordedAt").IsDescending(false, true);

            entity.Property(e => e.EvolutionReason).HasMaxLength(200);
            entity.Property(e => e.EvolutionType).HasMaxLength(50);
            entity.Property(e => e.RecordedAt).HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.Concept).WithMany(p => p.ConceptEvolutions)
                .HasForeignKey(d => d.ConceptId)
                .HasConstraintName("FK_ConceptEvolution_Concepts");
        });

        modelBuilder.Entity<DeduplicationPolicy>(entity =>
        {
            entity.HasIndex(e => e.PolicyName, "UX_DeduplicationPolicies_PolicyName").IsUnique();

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Metadata).HasColumnType("json");
            entity.Property(e => e.PolicyName).HasMaxLength(128);
        });

        modelBuilder.Entity<EmbeddingMigrationProgress>(entity =>
        {
            entity.HasKey(e => e.AtomEmbeddingId);

            entity.ToTable("EmbeddingMigrationProgress");

            entity.Property(e => e.AtomEmbeddingId).ValueGeneratedNever();
            entity.Property(e => e.MigratedAt).HasDefaultValueSql("(sysutcdatetime())");
        });

        modelBuilder.Entity<EventAtom>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__EventAto__3214EC075C395ED2");

            entity.HasIndex(e => e.ClusterId, "IX_EventAtoms_ClusterId");

            entity.HasIndex(e => e.CreatedAt, "IX_EventAtoms_CreatedAt").IsDescending();

            entity.HasIndex(e => e.EventType, "IX_EventAtoms_EventType");

            entity.HasIndex(e => e.StreamId, "IX_EventAtoms_StreamId");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.EventType).HasMaxLength(100);

            entity.HasOne(d => d.CentroidAtom).WithMany(p => p.EventAtoms)
                .HasForeignKey(d => d.CentroidAtomId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_EventAtoms_Atoms");

            entity.HasOne(d => d.Stream).WithMany(p => p.EventAtoms)
                .HasForeignKey(d => d.StreamId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_EventAtoms_StreamOrchestration");
        });

        modelBuilder.Entity<EventGenerationResult>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__EventGen__3214EC07FC89EC1E");

            entity.HasIndex(e => e.CreatedAt, "IX_EventGenerationResults_CreatedAt").IsDescending();

            entity.HasIndex(e => e.EventType, "IX_EventGenerationResults_EventType");

            entity.HasIndex(e => e.StreamId, "IX_EventGenerationResults_StreamId");

            entity.Property(e => e.ClusteringMethod).HasMaxLength(50);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.EventType).HasMaxLength(100);

            entity.HasOne(d => d.Stream).WithMany(p => p.EventGenerationResults)
                .HasForeignKey(d => d.StreamId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__EventGene__Strea__4FD1D5C8");
        });

        modelBuilder.Entity<EventHubCheckpoint>(entity =>
        {
            entity.HasKey(e => e.CheckpointId);

            entity.HasIndex(e => e.OwnerIdentifier, "IX_EventHubCheckpoints_Owner").HasFilter("([OwnerIdentifier] IS NOT NULL)");

            entity.HasIndex(e => e.UniqueKeyHash, "UX_EventHubCheckpoints_Composite").IsUnique();

            entity.Property(e => e.CheckpointId).HasDefaultValueSql("(newsequentialid())", "DF_EventHubCheckpoints_CheckpointId");
            entity.Property(e => e.ConsumerGroup).HasMaxLength(256);
            entity.Property(e => e.Etag)
                .HasMaxLength(36)
                .HasDefaultValueSql("(CONVERT([nvarchar](36),newid()))", "DF_EventHubCheckpoints_Etag")
                .HasColumnName("ETag");
            entity.Property(e => e.EventHubName).HasMaxLength(256);
            entity.Property(e => e.FullyQualifiedNamespace).HasMaxLength(256);
            entity.Property(e => e.LastModifiedTimeUtc).HasDefaultValueSql("(sysutcdatetime())", "DF_EventHubCheckpoints_LastModified");
            entity.Property(e => e.OwnerIdentifier).HasMaxLength(256);
            entity.Property(e => e.PartitionId).HasMaxLength(64);
            entity.Property(e => e.UniqueKeyHash)
                .HasMaxLength(32)
                .HasComputedColumnSql("(CONVERT([varbinary](32),hashbytes('SHA2_256',concat([FullyQualifiedNamespace],N'|',[EventHubName],N'|',[ConsumerGroup],N'|',[PartitionId]))))", true);
        });

        modelBuilder.Entity<GenerationStream>(entity =>
        {
            entity.HasKey(e => e.StreamId);

            entity.ToTable("GenerationStreams", "provenance");

            entity.HasIndex(e => e.CreatedUtc, "IX_GenerationStreams_CreatedUtc");

            entity.HasIndex(e => e.GenerationStreamId, "IX_GenerationStreams_GenerationStreamId");

            entity.HasIndex(e => e.Model, "IX_GenerationStreams_Model");

            entity.HasIndex(e => e.ModelId, "IX_GenerationStreams_ModelId");

            entity.HasIndex(e => e.Scope, "IX_GenerationStreams_Scope");

            entity.HasIndex(e => e.TenantId, "IX_GenerationStreams_TenantId");

            entity.HasIndex(e => e.GenerationStreamId, "UQ_GenerationStreams_GenerationStreamId").IsUnique();

            entity.Property(e => e.StreamId).ValueGeneratedNever();
            entity.Property(e => e.CreatedUtc)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.GenerationStreamId).ValueGeneratedOnAdd();
            entity.Property(e => e.Model).HasMaxLength(128);
            entity.Property(e => e.Scope).HasMaxLength(128);

            entity.HasOne(d => d.ModelNavigation).WithMany(p => p.GenerationStreams)
                .HasForeignKey(d => d.ModelId)
                .HasConstraintName("FK_GenerationStreams_Models");
        });

        modelBuilder.Entity<GenerationStreamSegment>(entity =>
        {
            entity.HasKey(e => e.SegmentId);

            entity.HasIndex(e => e.CreatedAt, "IX_GenerationStreamSegments_CreatedAt").IsDescending();

            entity.HasIndex(e => new { e.GenerationStreamId, e.SegmentOrdinal }, "IX_GenerationStreamSegments_GenerationStreamId");

            entity.HasIndex(e => e.SegmentKind, "IX_GenerationStreamSegments_SegmentKind").HasFilter("([EmbeddingVector] IS NOT NULL)");

            entity.Property(e => e.ContentType).HasMaxLength(128);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.EmbeddingVector).HasMaxLength(1998);
            entity.Property(e => e.SegmentKind).HasMaxLength(50);

            entity.HasOne(d => d.GenerationStream).WithMany(p => p.GenerationStreamSegments)
                .HasPrincipalKey(p => p.GenerationStreamId)
                .HasForeignKey(d => d.GenerationStreamId)
                .HasConstraintName("FK_GenerationStreamSegments_GenerationStreams");
        });

        modelBuilder.Entity<Image>(entity =>
        {
            entity.HasIndex(e => e.IngestionDate, "IX_Images_IngestionDate").IsDescending();

            entity.HasIndex(e => e.ObjectRegions, "IX_Images_ObjectRegions");

            entity.HasIndex(e => new { e.Width, e.Height }, "IX_Images_Width_Height");

            entity.Property(e => e.EdgeMap).HasColumnType("geometry");
            entity.Property(e => e.Format).HasMaxLength(20);
            entity.Property(e => e.GlobalEmbedding).HasMaxLength(1998);
            entity.Property(e => e.IngestionDate).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Metadata).HasColumnType("json");
            entity.Property(e => e.ObjectRegions).HasColumnType("geometry");
            entity.Property(e => e.PixelCloud).HasColumnType("geometry");
            entity.Property(e => e.SaliencyRegions).HasColumnType("geometry");
            entity.Property(e => e.SourcePath).HasMaxLength(500);
            entity.Property(e => e.SourceUrl).HasMaxLength(1000);
        });

        modelBuilder.Entity<ImagePatch>(entity =>
        {
            entity.HasKey(e => e.PatchId);

            entity.HasIndex(e => new { e.ImageId, e.PatchX, e.PatchY }, "IX_ImagePatches_ImageId_PatchX_PatchY");

            entity.Property(e => e.DominantColor).HasColumnType("geometry");
            entity.Property(e => e.PatchEmbedding).HasMaxLength(1998);
            entity.Property(e => e.PatchGeometry).HasColumnType("geometry");
            entity.Property(e => e.PatchRegion).HasColumnType("geometry");

            entity.HasOne(d => d.Image).WithMany(p => p.ImagePatches).HasForeignKey(d => d.ImageId);

            entity.HasOne(d => d.ParentAtom).WithMany(p => p.ImagePatches)
                .HasForeignKey(d => d.ParentAtomId)
                .HasConstraintName("FK_ImagePatches_Atoms");
        });

        modelBuilder.Entity<InferenceCache>(entity =>
        {
            entity.HasKey(e => e.CacheId);

            entity.ToTable("InferenceCache");

            entity.HasIndex(e => e.CacheKey, "IX_InferenceCache_CacheKey");

            entity.HasIndex(e => e.LastAccessedUtc, "IX_InferenceCache_LastAccessedUtc").IsDescending();

            entity.HasIndex(e => new { e.ModelId, e.InferenceType }, "IX_InferenceCache_ModelId_InferenceType");

            entity.Property(e => e.CacheKey).HasMaxLength(64);
            entity.Property(e => e.CreatedUtc).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.InferenceType).HasMaxLength(100);

            entity.HasOne(d => d.Model).WithMany(p => p.InferenceCaches).HasForeignKey(d => d.ModelId);
        });

        modelBuilder.Entity<InferenceCacheInMemory>(entity =>
        {
            entity.HasKey(e => e.CacheId).IsClustered(false);

            entity.ToTable("InferenceCache_InMemory", t => t.IsMemoryOptimized());

            entity.HasIndex(e => e.CacheKey, "IX_CacheKey_Hash");

            entity.HasIndex(e => e.LastAccessedUtc, "IX_LastAccessed_Range");

            entity.HasIndex(e => new { e.ModelId, e.InputHash }, "IX_ModelInput_Hash");

            entity.Property(e => e.CacheKey)
                .HasMaxLength(64)
                .UseCollation("Latin1_General_100_BIN2");
            entity.Property(e => e.CreatedUtc).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.InferenceType)
                .HasMaxLength(100)
                .UseCollation("Latin1_General_100_BIN2");
            entity.Property(e => e.InputHash)
                .HasMaxLength(32)
                .IsFixedLength();
            entity.Property(e => e.LastAccessedUtc).HasDefaultValueSql("(sysutcdatetime())");
        });

        modelBuilder.Entity<InferenceRequest>(entity =>
        {
            entity.HasKey(e => e.InferenceId);

            entity.HasIndex(e => e.CacheHit, "IX_InferenceRequests_CacheHit");

            entity.HasIndex(e => e.InputHash, "IX_InferenceRequests_InputHash");

            entity.HasIndex(e => e.ModelId, "IX_InferenceRequests_ModelId");

            entity.HasIndex(e => e.RequestTimestamp, "IX_InferenceRequests_RequestTimestamp").IsDescending();

            entity.HasIndex(e => e.TaskType, "IX_InferenceRequests_TaskType");

            entity.Property(e => e.EnsembleStrategy).HasMaxLength(50);
            entity.Property(e => e.InputData).HasColumnType("json");
            entity.Property(e => e.InputHash)
                .HasMaxLength(32)
                .IsFixedLength();
            entity.Property(e => e.ModelsUsed).HasColumnType("json");
            entity.Property(e => e.OutputData).HasColumnType("json");
            entity.Property(e => e.OutputMetadata).HasColumnType("json");
            entity.Property(e => e.RequestTimestamp).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.SlaTier).HasMaxLength(50);
            entity.Property(e => e.TaskType).HasMaxLength(50);

            entity.HasOne(d => d.Model).WithMany(p => p.InferenceRequests).HasForeignKey(d => d.ModelId);
        });

        modelBuilder.Entity<InferenceStep>(entity =>
        {
            entity.HasKey(e => e.StepId);

            entity.HasIndex(e => new { e.InferenceId, e.StepNumber }, "IX_InferenceSteps_InferenceId_StepNumber");

            entity.HasIndex(e => e.ModelId, "IX_InferenceSteps_ModelId");

            entity.Property(e => e.IndexUsed).HasMaxLength(200);
            entity.Property(e => e.OperationType).HasMaxLength(50);

            entity.HasOne(d => d.Inference).WithMany(p => p.InferenceSteps).HasForeignKey(d => d.InferenceId);

            entity.HasOne(d => d.Model).WithMany(p => p.InferenceSteps)
                .HasForeignKey(d => d.ModelId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<IngestionJob>(entity =>
        {
            entity.Property(e => e.Metadata).HasColumnType("json");
            entity.Property(e => e.PipelineName).HasMaxLength(256);
            entity.Property(e => e.SourceUri).HasMaxLength(1024);
            entity.Property(e => e.StartedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Status).HasMaxLength(64);
        });

        modelBuilder.Entity<IngestionJobAtom>(entity =>
        {
            entity.HasIndex(e => e.AtomId, "IX_IngestionJobAtoms_AtomId");

            entity.HasIndex(e => new { e.IngestionJobId, e.AtomId }, "IX_IngestionJobAtoms_Job_Atom");

            entity.Property(e => e.Notes).HasMaxLength(1024);

            entity.HasOne(d => d.Atom).WithMany(p => p.IngestionJobAtoms)
                .HasForeignKey(d => d.AtomId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.IngestionJob).WithMany(p => p.IngestionJobAtoms).HasForeignKey(d => d.IngestionJobId);
        });

        modelBuilder.Entity<LayerTensorSegment>(entity =>
        {
            entity.HasIndex(e => new { e.LayerId, e.Mmin, e.Mmax }, "IX_LayerTensorSegments_M_Range");

            entity.HasIndex(e => e.MortonCode, "IX_LayerTensorSegments_Morton");

            entity.HasIndex(e => new { e.LayerId, e.Zmin, e.Zmax }, "IX_LayerTensorSegments_Z_Range");

            entity.HasIndex(e => new { e.LayerId, e.SegmentOrdinal }, "UX_LayerTensorSegments_LayerId_SegmentOrdinal").IsUnique();

            entity.HasIndex(e => e.PayloadRowGuid, "UX_LayerTensorSegments_PayloadRowGuid").IsUnique();

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.GeometryFootprint).HasColumnType("geometry");
            entity.Property(e => e.Mmax).HasColumnName("MMax");
            entity.Property(e => e.Mmin).HasColumnName("MMin");
            entity.Property(e => e.PayloadRowGuid).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.QuantizationType).HasMaxLength(20);
            entity.Property(e => e.Zmax).HasColumnName("ZMax");
            entity.Property(e => e.Zmin).HasColumnName("ZMin");

            entity.HasOne(d => d.Layer).WithMany(p => p.LayerTensorSegments).HasForeignKey(d => d.LayerId);
        });

        modelBuilder.Entity<Model>(entity =>
        {
            entity.HasIndex(e => e.ModelName, "IX_Models_ModelName");

            entity.HasIndex(e => e.ModelType, "IX_Models_ModelType");

            entity.Property(e => e.Architecture).HasMaxLength(100);
            entity.Property(e => e.Config).HasColumnType("json");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())", "DF_Models_CreatedAt");
            entity.Property(e => e.IngestionDate).HasDefaultValueSql("(sysutcdatetime())", "DF_Models_IngestionDate");
            entity.Property(e => e.IsActive).HasDefaultValue(true, "DF_Models_IsActive");
            entity.Property(e => e.MetadataJson).HasColumnType("json");
            entity.Property(e => e.ModelName).HasMaxLength(200);
            entity.Property(e => e.ModelType).HasMaxLength(100);
            entity.Property(e => e.ModelVersion).HasMaxLength(50);
        });

        modelBuilder.Entity<ModelLayer>(entity =>
        {
            entity.HasKey(e => e.LayerId);

            entity.HasIndex(e => e.LayerAtomId, "IX_ModelLayers_LayerAtomId");

            entity.HasIndex(e => e.LayerType, "IX_ModelLayers_LayerType");

            entity.HasIndex(e => new { e.ModelId, e.Mmin, e.Mmax }, "IX_ModelLayers_M_Range");

            entity.HasIndex(e => new { e.ModelId, e.LayerIdx }, "IX_ModelLayers_ModelId_LayerIdx");

            entity.HasIndex(e => e.MortonCode, "IX_ModelLayers_Morton");

            entity.HasIndex(e => new { e.ModelId, e.Zmin, e.Zmax }, "IX_ModelLayers_Z_Range");

            entity.Property(e => e.CacheHitRate).HasDefaultValue(0.0);
            entity.Property(e => e.LayerName).HasMaxLength(100);
            entity.Property(e => e.LayerType).HasMaxLength(50);
            entity.Property(e => e.Mmax).HasColumnName("MMax");
            entity.Property(e => e.Mmin).HasColumnName("MMin");
            entity.Property(e => e.Parameters).HasColumnType("json");
            entity.Property(e => e.QuantizationType).HasMaxLength(20);
            entity.Property(e => e.TensorDtype)
                .HasMaxLength(20)
                .HasDefaultValue("float32");
            entity.Property(e => e.TensorShape).HasMaxLength(200);
            entity.Property(e => e.WeightsGeometry).HasColumnType("geometry");
            entity.Property(e => e.Zmax).HasColumnName("ZMax");
            entity.Property(e => e.Zmin).HasColumnName("ZMin");

            entity.HasOne(d => d.LayerAtom).WithMany(p => p.ModelLayers)
                .HasForeignKey(d => d.LayerAtomId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(d => d.Model).WithMany(p => p.ModelLayers).HasForeignKey(d => d.ModelId);
        });

        modelBuilder.Entity<ModelMetadatum>(entity =>
        {
            entity.HasKey(e => e.MetadataId);

            entity.HasIndex(e => e.ModelId, "IX_ModelMetadata_ModelId").IsUnique();

            entity.Property(e => e.License).HasMaxLength(100);
            entity.Property(e => e.PerformanceMetrics).HasColumnType("json");
            entity.Property(e => e.SourceUrl).HasMaxLength(500);
            entity.Property(e => e.SupportedModalities).HasColumnType("json");
            entity.Property(e => e.SupportedTasks).HasColumnType("json");
            entity.Property(e => e.TrainingDataset).HasMaxLength(500);

            entity.HasOne(d => d.Model).WithOne(p => p.ModelMetadatum).HasForeignKey<ModelMetadatum>(d => d.ModelId);
        });

        modelBuilder.Entity<ModelVersionHistory>(entity =>
        {
            entity.HasKey(e => e.VersionHistoryId);

            entity.ToTable("ModelVersionHistory", "provenance");

            entity.HasIndex(e => new { e.ModelId, e.CreatedAt }, "IX_ModelVersionHistory_ModelId_CreatedAt").IsDescending(false, true);

            entity.HasIndex(e => new { e.ModelId, e.VersionTag }, "UX_ModelVersionHistory_ModelId_VersionTag").IsUnique();

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.CreatedBy).HasMaxLength(128);
            entity.Property(e => e.VersionHash).HasMaxLength(64);
            entity.Property(e => e.VersionTag).HasMaxLength(50);

            entity.HasOne(d => d.Model).WithMany(p => p.ModelVersionHistories)
                .HasForeignKey(d => d.ModelId)
                .HasConstraintName("FK_ModelVersionHistory_Models");

            entity.HasOne(d => d.ParentVersion).WithMany(p => p.InverseParentVersion)
                .HasForeignKey(d => d.ParentVersionId)
                .HasConstraintName("FK_ModelVersionHistory_ParentVersion");
        });

        modelBuilder.Entity<MultiPathReasoning>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__MultiPat__3214EC079E9827F1");

            entity.ToTable("MultiPathReasoning");

            entity.HasIndex(e => e.CreatedAt, "IX_MultiPathReasoning_CreatedAt").IsDescending();

            entity.HasIndex(e => e.ProblemId, "IX_MultiPathReasoning_ProblemId");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.ReasoningTree).HasColumnType("json");
        });

        modelBuilder.Entity<Neo4jSyncLog>(entity =>
        {
            entity.HasKey(e => e.LogId);

            entity.ToTable("Neo4jSyncLog");

            entity.HasIndex(e => new { e.EntityType, e.EntityId, e.SyncTimestamp }, "IX_Neo4jSyncLog_Entity").IsDescending(false, false, true);

            entity.HasIndex(e => new { e.SyncStatus, e.SyncTimestamp }, "IX_Neo4jSyncLog_Status").IsDescending(false, true);

            entity.HasIndex(e => e.SyncTimestamp, "IX_Neo4jSyncLog_Timestamp").IsDescending();

            entity.Property(e => e.EntityType).HasMaxLength(50);
            entity.Property(e => e.SyncStatus).HasMaxLength(50);
            entity.Property(e => e.SyncTimestamp).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.SyncType).HasMaxLength(50);
        });

        modelBuilder.Entity<OperationProvenance>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__tmp_ms_x__3214EC07929DA11B");

            entity.ToTable("OperationProvenance");

            entity.HasIndex(e => e.CreatedAt, "IX_OperationProvenance_CreatedAt").IsDescending();

            entity.HasIndex(e => e.OperationId, "IX_OperationProvenance_OperationId");

            entity.HasIndex(e => e.OperationId, "UQ__tmp_ms_x__A4F5FC45624FE83C").IsUnique();

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
        });

        modelBuilder.Entity<PendingAction>(entity =>
        {
            entity.HasKey(e => e.ActionId);

            entity.HasIndex(e => e.CreatedUtc, "IX_PendingActions_Created").IsDescending();

            entity.HasIndex(e => e.Status, "IX_PendingActions_Status");

            entity.Property(e => e.ActionType).HasMaxLength(100);
            entity.Property(e => e.ApprovedBy).HasMaxLength(128);
            entity.Property(e => e.CreatedUtc).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.EstimatedImpact).HasMaxLength(20);
            entity.Property(e => e.ResultJson).HasColumnType("json");
            entity.Property(e => e.RiskLevel)
                .HasMaxLength(20)
                .HasDefaultValue("medium");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasDefaultValue("PendingApproval");
        });

        modelBuilder.Entity<ProvenanceAuditResult>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Provenan__3214EC07028A8C64");

            entity.HasIndex(e => new { e.AuditPeriodStart, e.AuditPeriodEnd }, "IX_ProvenanceAuditResults_AuditPeriod");

            entity.HasIndex(e => e.AuditedAt, "IX_ProvenanceAuditResults_AuditedAt").IsDescending();

            entity.Property(e => e.Anomalies).HasColumnType("json");
            entity.Property(e => e.AuditedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Scope).HasMaxLength(100);
        });

        modelBuilder.Entity<ProvenanceValidationResult>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Provenan__3214EC07AE792BDA");

            entity.HasIndex(e => e.OperationId, "IX_ProvenanceValidationResults_OperationId");

            entity.HasIndex(e => e.OverallStatus, "IX_ProvenanceValidationResults_Status");

            entity.HasIndex(e => e.ValidatedAt, "IX_ProvenanceValidationResults_ValidatedAt").IsDescending();

            entity.Property(e => e.OverallStatus).HasMaxLength(20);
            entity.Property(e => e.ValidatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.ValidationResults).HasColumnType("json");

            entity.HasOne(d => d.Operation).WithMany(p => p.ProvenanceValidationResults)
                .HasPrincipalKey(p => p.OperationId)
                .HasForeignKey(d => d.OperationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ProvenanceValidationResults_OperationProvenance");
        });

        modelBuilder.Entity<ReasoningChain>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Reasonin__3214EC07801AA884");

            entity.HasIndex(e => e.CreatedAt, "IX_ReasoningChains_CreatedAt").IsDescending();

            entity.HasIndex(e => e.ProblemId, "IX_ReasoningChains_ProblemId");

            entity.Property(e => e.ChainData).HasColumnType("json");
            entity.Property(e => e.CoherenceMetrics).HasColumnType("json");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.ReasoningType)
                .HasMaxLength(50)
                .HasDefaultValue("chain_of_thought");
        });

        modelBuilder.Entity<SelfConsistencyResult>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__SelfCons__3214EC0782E045FB");

            entity.HasIndex(e => e.CreatedAt, "IX_SelfConsistencyResults_CreatedAt").IsDescending();

            entity.HasIndex(e => e.ProblemId, "IX_SelfConsistencyResults_ProblemId");

            entity.Property(e => e.ConsensusMetrics).HasColumnType("json");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.SampleData).HasColumnType("json");
        });

        modelBuilder.Entity<SemanticFeature>(entity =>
        {
            entity.HasKey(e => e.AtomEmbeddingId).HasName("PK__Semantic__AB86F630B871FA03");

            entity.HasIndex(e => e.SentimentScore, "ix_semantic_sentiment");

            entity.HasIndex(e => e.TemporalRelevance, "ix_semantic_temporal").HasFilter("([TemporalRelevance]>(0.5))");

            entity.HasIndex(e => e.TopicBusiness, "ix_semantic_topic_business").HasFilter("([TopicBusiness]>(0.5))");

            entity.HasIndex(e => e.TopicCreative, "ix_semantic_topic_creative").HasFilter("([TopicCreative]>(0.5))");

            entity.HasIndex(e => e.TopicScientific, "ix_semantic_topic_scientific").HasFilter("([TopicScientific]>(0.5))");

            entity.HasIndex(e => e.TopicTechnical, "ix_semantic_topic_technical").HasFilter("([TopicTechnical]>(0.5))");

            entity.Property(e => e.AtomEmbeddingId).ValueGeneratedNever();
            entity.Property(e => e.ComplexityScore).HasDefaultValue(0.0);
            entity.Property(e => e.ComputedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.FormalityScore).HasDefaultValue(0.0);
            entity.Property(e => e.SentimentScore).HasDefaultValue(0.0);
            entity.Property(e => e.TemporalRelevance).HasDefaultValue(1.0);
            entity.Property(e => e.TopicBusiness).HasDefaultValue(0.0);
            entity.Property(e => e.TopicCreative).HasDefaultValue(0.0);
            entity.Property(e => e.TopicScientific).HasDefaultValue(0.0);
            entity.Property(e => e.TopicTechnical).HasDefaultValue(0.0);

            entity.HasOne(d => d.AtomEmbedding).WithOne(p => p.SemanticFeature)
                .HasForeignKey<SemanticFeature>(d => d.AtomEmbeddingId)
                .HasConstraintName("FK_SemanticFeatures_AtomEmbeddings");
        });

        modelBuilder.Entity<SessionPath>(entity =>
        {
            entity.HasIndex(e => e.SessionId, "IX_SessionPaths_SessionId").IsUnique();

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.EndTime).HasComputedColumnSql("([Path].[STPointN]([Path].[STNumPoints]()).M)", false);
            entity.Property(e => e.Path).HasColumnType("geometry");
            entity.Property(e => e.PathLength).HasComputedColumnSql("([Path].[STLength]())", false);
            entity.Property(e => e.StartTime).HasComputedColumnSql("([Path].[STPointN]((1)).M)", false);
        });

        modelBuilder.Entity<SessionPathsInMemory>(entity =>
        {
            entity.HasKey(e => e.SessionPathId).IsClustered(false);

            entity.ToTable("SessionPaths_InMemory", t => t.IsMemoryOptimized());

            entity.HasIndex(e => e.SessionId, "IX_SessionId_Hash");

            entity.HasIndex(e => new { e.SessionId, e.PathNumber }, "IX_SessionPath_Hash");

            entity.Property(e => e.CreatedUtc).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.ResponseText).UseCollation("Latin1_General_100_BIN2");
        });

        modelBuilder.Entity<SpatialLandmark>(entity =>
        {
            entity.HasKey(e => e.LandmarkId);

            entity.Property(e => e.CreatedUtc).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.LandmarkPoint).HasColumnType("geometry");
            entity.Property(e => e.LandmarkVector).HasMaxLength(1998);
            entity.Property(e => e.SelectionMethod).HasMaxLength(50);
        });

        modelBuilder.Entity<Status>(entity =>
        {
            entity
                .ToTable("Status", "ref")
                .ToTable(tb => tb.IsTemporal(ttb =>
                    {
                        ttb.UseHistoryTable("Status_History", "ref");
                        ttb
                            .HasPeriodStart("ValidFrom")
                            .HasColumnName("ValidFrom");
                        ttb
                            .HasPeriodEnd("ValidTo")
                            .HasColumnName("ValidTo");
                    }));

            entity.HasIndex(e => new { e.IsActive, e.Code }, "IX_Status_Active_Code").HasFilter("([IsActive]=(1))");

            entity.HasIndex(e => e.Code, "UQ_Status_Code").IsUnique();

            entity.HasIndex(e => e.Name, "UQ_Status_Name").IsUnique();

            entity.Property(e => e.Code)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Name).HasMaxLength(100);
        });

        modelBuilder.Entity<StreamFusionResult>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__StreamFu__3214EC073F5F230A");

            entity.HasIndex(e => e.CreatedAt, "IX_StreamFusionResults_CreatedAt").IsDescending();

            entity.HasIndex(e => e.FusionType, "IX_StreamFusionResults_FusionType");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.FusionType).HasMaxLength(50);
            entity.Property(e => e.StreamIds).HasColumnType("json");
            entity.Property(e => e.Weights).HasColumnType("json");
        });

        modelBuilder.Entity<StreamOrchestrationResult>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__StreamOr__3214EC07D0D661A7");

            entity.HasIndex(e => e.CreatedAt, "IX_StreamOrchestrationResults_CreatedAt").IsDescending();

            entity.HasIndex(e => e.SensorType, "IX_StreamOrchestrationResults_SensorType");

            entity.HasIndex(e => new { e.TimeWindowStart, e.TimeWindowEnd }, "IX_StreamOrchestrationResults_TimeWindow");

            entity.Property(e => e.AggregationLevel).HasMaxLength(50);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.SensorType).HasMaxLength(100);
        });

        modelBuilder.Entity<TenantAtom>(entity =>
        {
            entity.HasKey(e => new { e.TenantId, e.AtomId });

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())", "DF_TenantAtoms_CreatedAt");

            entity.HasOne(d => d.Atom).WithMany(p => p.TenantAtoms)
                .HasForeignKey(d => d.AtomId)
                .HasConstraintName("FK_TenantAtoms_Atoms");
        });

        modelBuilder.Entity<TenantSecurityPolicy>(entity =>
        {
            entity.HasKey(e => e.PolicyId);

            entity.ToTable("TenantSecurityPolicy");

            entity.HasIndex(e => new { e.EffectiveFrom, e.EffectiveTo }, "IX_TenantSecurityPolicy_EffectiveDates");

            entity.HasIndex(e => e.IsActive, "IX_TenantSecurityPolicy_IsActive");

            entity.HasIndex(e => new { e.TenantId, e.PolicyType }, "IX_TenantSecurityPolicy_TenantId_PolicyType");

            entity.Property(e => e.CreatedBy).HasMaxLength(256);
            entity.Property(e => e.CreatedUtc).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.PolicyName).HasMaxLength(100);
            entity.Property(e => e.PolicyType).HasMaxLength(50);
            entity.Property(e => e.TenantId).HasMaxLength(128);
            entity.Property(e => e.UpdatedBy).HasMaxLength(256);
        });

        modelBuilder.Entity<TensorAtom>(entity =>
        {
            entity.HasIndex(e => e.AtomId, "IX_TensorAtoms_AtomId");

            entity.HasIndex(e => e.GeometryFootprint, "IX_TensorAtoms_GeometryFootprint");

            entity.HasIndex(e => e.LayerId, "IX_TensorAtoms_LayerId");

            entity.HasIndex(e => new { e.ModelId, e.LayerId, e.AtomType }, "IX_TensorAtoms_Model_Layer_Type");

            entity.HasIndex(e => e.SpatialSignature, "IX_TensorAtoms_SpatialSignature");

            entity.Property(e => e.AtomType).HasMaxLength(128);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.GeometryFootprint).HasColumnType("geometry");
            entity.Property(e => e.Metadata).HasColumnType("json");
            entity.Property(e => e.SpatialSignature).HasColumnType("geometry");

            entity.HasOne(d => d.Atom).WithMany(p => p.TensorAtoms).HasForeignKey(d => d.AtomId);

            entity.HasOne(d => d.Layer).WithMany(p => p.TensorAtoms).HasForeignKey(d => d.LayerId);

            entity.HasOne(d => d.Model).WithMany(p => p.TensorAtoms).HasForeignKey(d => d.ModelId);
        });

        modelBuilder.Entity<TensorAtomCoefficient>(entity =>
        {
            entity.ToTable(tb => tb.IsTemporal(ttb =>
                    {
                        ttb.UseHistoryTable("TensorAtomCoefficients_History", "dbo");
                        ttb
                            .HasPeriodStart("ValidFrom")
                            .HasColumnName("ValidFrom");
                        ttb
                            .HasPeriodEnd("ValidTo")
                            .HasColumnName("ValidTo");
                    }));

            entity.HasIndex(e => new { e.TensorAtomId, e.ParentLayerId, e.TensorRole }, "IX_TensorAtomCoefficients_Lookup");

            entity.HasIndex(e => e.ParentLayerId, "IX_TensorAtomCoefficients_ParentLayerId");

            entity.Property(e => e.TensorRole).HasMaxLength(128);

            entity.HasOne(d => d.ParentLayer).WithMany(p => p.TensorAtomCoefficients).HasForeignKey(d => d.ParentLayerId);

            entity.HasOne(d => d.TensorAtom).WithMany(p => p.TensorAtomCoefficients).HasForeignKey(d => d.TensorAtomId);
        });

        modelBuilder.Entity<TensorAtomPayload>(entity =>
        {
            entity.HasKey(e => e.PayloadId);

            entity.HasIndex(e => e.TensorAtomId, "IX_TensorAtomPayloads_TensorAtomId").IsUnique();

            entity.HasIndex(e => e.RowGuid, "UQ_TensorAtomPayloads_RowGuid").IsUnique();

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Metadata).HasColumnType("json");
            entity.Property(e => e.RowGuid).HasDefaultValueSql("(newid())");

            entity.HasOne(d => d.TensorAtom).WithOne(p => p.TensorAtomPayload)
                .HasForeignKey<TensorAtomPayload>(d => d.TensorAtomId)
                .HasConstraintName("FK_TensorAtomPayloads_TensorAtoms");
        });

        modelBuilder.Entity<TestResult>(entity =>
        {
            entity.HasIndex(e => e.ExecutionTimeMs, "IX_TestResults_ExecutionTimeMs").IsDescending();

            entity.HasIndex(e => new { e.TestCategory, e.ExecutedAt }, "IX_TestResults_TestCategory_ExecutedAt").IsDescending();

            entity.HasIndex(e => e.TestStatus, "IX_TestResults_TestStatus");

            entity.HasIndex(e => new { e.TestSuite, e.ExecutedAt }, "IX_TestResults_TestSuite_ExecutedAt").IsDescending();

            entity.Property(e => e.Environment).HasMaxLength(100);
            entity.Property(e => e.ExecutedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.MemoryUsageMb).HasColumnName("MemoryUsageMB");
            entity.Property(e => e.TestCategory).HasMaxLength(50);
            entity.Property(e => e.TestName).HasMaxLength(200);
            entity.Property(e => e.TestStatus).HasMaxLength(50);
            entity.Property(e => e.TestSuite).HasMaxLength(100);
        });

        modelBuilder.Entity<TextDocument>(entity =>
        {
            entity.HasKey(e => e.DocId);

            entity.Property(e => e.GlobalEmbedding).HasMaxLength(1998);
            entity.Property(e => e.IngestionDate).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Language).HasMaxLength(10);
            entity.Property(e => e.Metadata).HasColumnType("json");
            entity.Property(e => e.SourcePath).HasMaxLength(500);
            entity.Property(e => e.SourceUrl).HasMaxLength(1000);
            entity.Property(e => e.TopicVector).HasMaxLength(1998);
        });

        modelBuilder.Entity<TokenVocabulary>(entity =>
        {
            entity.HasKey(e => e.VocabId);

            entity.ToTable("TokenVocabulary");

            entity.HasIndex(e => new { e.ModelId, e.Token }, "IX_TokenVocabulary_ModelId_Token");

            entity.HasIndex(e => new { e.ModelId, e.TokenId }, "IX_TokenVocabulary_ModelId_TokenId").IsUnique();

            entity.Property(e => e.Embedding).HasMaxLength(1998);
            entity.Property(e => e.Token).HasMaxLength(100);
            entity.Property(e => e.TokenType).HasMaxLength(20);

            entity.HasOne(d => d.Model).WithMany(p => p.TokenVocabularies).HasForeignKey(d => d.ModelId);
        });

        modelBuilder.Entity<TopicKeyword>(entity =>
        {
            entity.HasKey(e => e.KeywordId).HasName("PK__TopicKey__03E8D7CF404E244D");

            entity.Property(e => e.KeywordId).HasColumnName("keyword_id");
            entity.Property(e => e.Keyword)
                .HasMaxLength(100)
                .HasColumnName("keyword");
            entity.Property(e => e.TopicName)
                .HasMaxLength(50)
                .HasColumnName("topic_name");
            entity.Property(e => e.Weight)
                .HasDefaultValue(1.0)
                .HasColumnName("weight");
        });

        modelBuilder.Entity<TransformerInferenceResult>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Transfor__3214EC07D16DAE4F");

            entity.HasIndex(e => e.CreatedAt, "IX_TransformerInferenceResults_CreatedAt").IsDescending();

            entity.HasIndex(e => e.ProblemId, "IX_TransformerInferenceResults_ProblemId");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.LayerResults).HasColumnType("json");

            entity.HasOne(d => d.Model).WithMany(p => p.TransformerInferenceResults)
                .HasForeignKey(d => d.ModelId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TransformerInferenceResults_Models");
        });

        modelBuilder.Entity<Video>(entity =>
        {
            entity.HasIndex(e => e.IngestionDate, "IX_Videos_IngestionDate").IsDescending();

            entity.HasIndex(e => new { e.ResolutionWidth, e.ResolutionHeight }, "IX_Videos_ResolutionWidth_ResolutionHeight");

            entity.Property(e => e.Format).HasMaxLength(20);
            entity.Property(e => e.GlobalEmbedding).HasMaxLength(1998);
            entity.Property(e => e.IngestionDate).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Metadata).HasColumnType("json");
            entity.Property(e => e.SourcePath).HasMaxLength(500);
        });

        modelBuilder.Entity<VideoFrame>(entity =>
        {
            entity.HasKey(e => e.FrameId);

            entity.HasIndex(e => e.MotionVectors, "IX_VideoFrames_MotionVectors");

            entity.HasIndex(e => new { e.VideoId, e.FrameNumber }, "IX_VideoFrames_VideoId_FrameNumber").IsUnique();

            entity.HasIndex(e => new { e.VideoId, e.TimestampMs }, "IX_VideoFrames_VideoId_TimestampMs");

            entity.Property(e => e.FrameEmbedding).HasMaxLength(1998);
            entity.Property(e => e.MotionVectors).HasColumnType("geometry");
            entity.Property(e => e.ObjectRegions).HasColumnType("geometry");
            entity.Property(e => e.OpticalFlow).HasColumnType("geometry");
            entity.Property(e => e.PerceptualHash).HasMaxLength(8);
            entity.Property(e => e.PixelCloud).HasColumnType("geometry");

            entity.HasOne(d => d.Video).WithMany(p => p.VideoFrames).HasForeignKey(d => d.VideoId);
        });

        modelBuilder.Entity<VwAtomsWithLob>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_AtomsWithLOBs");

            entity.Property(e => e.AtomicValue).HasMaxLength(64);
            entity.Property(e => e.CanonicalText).HasMaxLength(256);
            entity.Property(e => e.ContentHash)
                .HasMaxLength(32)
                .IsFixedLength();
            entity.Property(e => e.ContentType).HasMaxLength(128);
            entity.Property(e => e.Metadata).HasColumnType("json");
            entity.Property(e => e.Modality).HasMaxLength(64);
            entity.Property(e => e.PayloadLocator).HasMaxLength(1024);
            entity.Property(e => e.SourceType).HasMaxLength(128);
            entity.Property(e => e.SourceUri).HasMaxLength(1024);
            entity.Property(e => e.SpatialKey).HasColumnType("geometry");
            entity.Property(e => e.Subtype).HasMaxLength(128);
        });

        modelBuilder.Entity<VwCurrentWeight>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_CurrentWeights");

            entity.Property(e => e.AtomDescription).HasMaxLength(4000);
            entity.Property(e => e.AtomSource).HasMaxLength(4000);
            entity.Property(e => e.AtomType).HasMaxLength(128);
            entity.Property(e => e.TensorRole).HasMaxLength(128);
        });

        modelBuilder.Entity<VwEmbeddingVector>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_EmbeddingVectors");
        });

        modelBuilder.Entity<VwWeightChangeHistory>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_WeightChangeHistory");

            entity.Property(e => e.TensorRole).HasMaxLength(128);
        });

        modelBuilder.Entity<Weight>(entity =>
        {
            entity.ToTable(tb => tb.IsTemporal(ttb =>
                    {
                        ttb.UseHistoryTable("Weights_History", "dbo");
                        ttb
                            .HasPeriodStart("ValidFrom")
                            .HasColumnName("ValidFrom");
                        ttb
                            .HasPeriodEnd("ValidTo")
                            .HasColumnName("ValidTo");
                    }));

            entity.HasIndex(e => e.ImportanceScore, "IX_Weights_Importance")
                .IsDescending()
                .HasFilter("([ImportanceScore]>(0.7))");

            entity.HasIndex(e => new { e.LayerId, e.NeuronIndex }, "IX_Weights_Layer");

            entity.Property(e => e.ImportanceScore).HasDefaultValue(0.5f);
            entity.Property(e => e.LastUpdated).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.LayerId).HasColumnName("LayerID");
            entity.Property(e => e.WeightType)
                .HasMaxLength(50)
                .HasDefaultValue("parameter");

            entity.HasOne(d => d.Layer).WithMany(p => p.Weights)
                .HasForeignKey(d => d.LayerId)
                .HasConstraintName("FK_Weights_Layers");
        });

        modelBuilder.Entity<WeightSnapshot>(entity =>
        {
            entity.HasKey(e => e.SnapshotId).HasName("PK__WeightSn__664F572B7BB90258");

            entity.HasIndex(e => e.SnapshotName, "UQ__WeightSn__FAC0EC4ACB574E03").IsUnique();

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.SnapshotName).HasMaxLength(255);
            entity.Property(e => e.SnapshotTime).HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.Model).WithMany(p => p.WeightSnapshots)
                .HasForeignKey(d => d.ModelId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_WeightSnapshots_Models");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
