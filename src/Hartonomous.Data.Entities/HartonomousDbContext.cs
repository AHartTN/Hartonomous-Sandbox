using System;
using System.Collections.Generic;
using Hartonomous.Data.Entities.Entities;
using Microsoft.EntityFrameworkCore;

namespace Hartonomous.Data.Entities;

public partial class HartonomousDbContext : DbContext
{
    public HartonomousDbContext()
    {
    }

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

    public virtual DbSet<AtomRelation> AtomRelations { get; set; }

    public virtual DbSet<AttentionGenerationLog> AttentionGenerationLogs { get; set; }

    public virtual DbSet<AttentionInferenceResult> AttentionInferenceResults { get; set; }

    public virtual DbSet<AutonomousComputeJob> AutonomousComputeJobs { get; set; }

    public virtual DbSet<AutonomousImprovementHistory> AutonomousImprovementHistories { get; set; }

    public virtual DbSet<BackgroundJob> BackgroundJobs { get; set; }

    public virtual DbSet<BillingInvoice> BillingInvoices { get; set; }

    public virtual DbSet<BillingMultiplier> BillingMultipliers { get; set; }

    public virtual DbSet<BillingOperationRate> BillingOperationRates { get; set; }

    public virtual DbSet<BillingPricingTier> BillingPricingTiers { get; set; }

    public virtual DbSet<BillingQuotaViolation> BillingQuotaViolations { get; set; }

    public virtual DbSet<BillingRatePlan> BillingRatePlans { get; set; }

    public virtual DbSet<BillingTenantQuotum> BillingTenantQuota { get; set; }

    public virtual DbSet<BillingUsageLedger> BillingUsageLedgers { get; set; }

    public virtual DbSet<CachedActivation> CachedActivations { get; set; }

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

    public virtual DbSet<InferenceCache> InferenceCaches { get; set; }

    public virtual DbSet<InferenceRequest> InferenceRequests { get; set; }

    public virtual DbSet<InferenceStep> InferenceSteps { get; set; }

    public virtual DbSet<IngestionJob> IngestionJobs { get; set; }

    public virtual DbSet<IngestionJobAtom> IngestionJobAtoms { get; set; }

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

    public virtual DbSet<SpatialLandmark> SpatialLandmarks { get; set; }

    public virtual DbSet<Status> Statuses { get; set; }

    public virtual DbSet<StreamFusionResult> StreamFusionResults { get; set; }

    public virtual DbSet<StreamOrchestrationResult> StreamOrchestrationResults { get; set; }

    public virtual DbSet<TenantAtom> TenantAtoms { get; set; }

    public virtual DbSet<TenantGuidMapping> TenantGuidMappings { get; set; }

    public virtual DbSet<TenantSecurityPolicy> TenantSecurityPolicies { get; set; }

    public virtual DbSet<TensorAtom> TensorAtoms { get; set; }

    public virtual DbSet<TensorAtomCoefficient> TensorAtomCoefficients { get; set; }

    public virtual DbSet<TestResult> TestResults { get; set; }

    public virtual DbSet<TokenVocabulary> TokenVocabularies { get; set; }

    public virtual DbSet<TopicKeyword> TopicKeywords { get; set; }

    public virtual DbSet<TransformerInferenceResult> TransformerInferenceResults { get; set; }

    public virtual DbSet<VwReconstructModelLayerWeight> VwReconstructModelLayerWeights { get; set; }

    public virtual DbSet<WeightSnapshot> WeightSnapshots { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=localhost;Database=Hartonomous;Integrated Security=True;TrustServerCertificate=True;", x => x.UseNetTopologySuite());

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AgentTool>(entity =>
        {
            entity.HasKey(e => e.ToolId).HasName("PK__AgentToo__CC0CEB91A869BA9B");

            entity.HasIndex(e => e.ToolName, "UQ__AgentToo__006DA271DB649845").IsUnique();

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
            entity
                .ToTable("Atom")
                .ToTable(tb => tb.IsTemporal(ttb =>
                    {
                        ttb.UseHistoryTable("AtomHistory", "dbo");
                        ttb
                            .HasPeriodStart("CreatedAt")
                            .HasColumnName("CreatedAt");
                        ttb
                            .HasPeriodEnd("ModifiedAt")
                            .HasColumnName("ModifiedAt");
                    }));

            entity.HasIndex(e => e.ContentType, "IX_Atom_ContentType").HasFilter("([ContentType] IS NOT NULL)");

            entity.HasIndex(e => new { e.Modality, e.Subtype }, "IX_Atom_Modality");

            entity.HasIndex(e => e.ReferenceCount, "IX_Atom_ReferenceCount").IsDescending();

            entity.HasIndex(e => new { e.TenantId, e.Modality }, "IX_Atom_TenantId");

            entity.HasIndex(e => e.ContentHash, "UX_Atom_ContentHash").IsUnique();

            entity.Property(e => e.AtomicValue).HasMaxLength(64);
            entity.Property(e => e.ContentHash)
                .HasMaxLength(32)
                .IsFixedLength();
            entity.Property(e => e.ContentType).HasMaxLength(100);
            entity.Property(e => e.Modality)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ReferenceCount).HasDefaultValue(1L);
            entity.Property(e => e.SourceType).HasMaxLength(100);
            entity.Property(e => e.SourceUri).HasMaxLength(2048);
            entity.Property(e => e.Subtype)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<AtomComposition>(entity =>
        {
            entity.HasKey(e => e.CompositionId);

            entity.ToTable("AtomComposition");

            entity.Property(e => e.SpatialKey).HasColumnType("geometry");

            entity.HasOne(d => d.ComponentAtom).WithMany(p => p.AtomCompositionComponentAtoms)
                .HasForeignKey(d => d.ComponentAtomId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AtomCompositions_Component");

            entity.HasOne(d => d.ParentAtom).WithMany(p => p.AtomCompositionParentAtoms)
                .HasForeignKey(d => d.ParentAtomId)
                .HasConstraintName("FK_AtomCompositions_Parent");
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
            entity.ToTable("AtomEmbedding");

            entity.HasIndex(e => e.AtomId, "IX_AtomEmbedding_Atom");

            entity.HasIndex(e => e.AtomId, "IX_AtomEmbedding_AtomId");

            entity.HasIndex(e => new { e.Dimension, e.EmbeddingType }, "IX_AtomEmbedding_Dimension");

            entity.HasIndex(e => e.HilbertValue, "IX_AtomEmbedding_Hilbert").HasFilter("([HilbertValue] IS NOT NULL)");

            entity.HasIndex(e => new { e.SpatialBucketX, e.SpatialBucketY, e.SpatialBucketZ }, "IX_AtomEmbedding_SpatialBuckets").HasFilter("([SpatialBucketX] IS NOT NULL)");

            entity.HasIndex(e => new { e.TenantId, e.ModelId, e.EmbeddingType }, "IX_AtomEmbedding_TenantId_ModelId");

            entity.HasIndex(e => e.SpatialKey, "SIX_AtomEmbedding_SpatialKey");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.EmbeddingType)
                .HasMaxLength(50)
                .HasDefaultValue("semantic");
            entity.Property(e => e.EmbeddingVector).HasMaxLength(1998);
            entity.Property(e => e.SpatialKey).HasColumnType("geometry");

            entity.HasOne(d => d.Atom).WithMany(p => p.AtomEmbeddings)
                .HasForeignKey(d => d.AtomId)
                .HasConstraintName("FK_AtomEmbeddings_Atom");

            entity.HasOne(d => d.Model).WithMany(p => p.AtomEmbeddings)
                .HasForeignKey(d => d.ModelId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AtomEmbeddings_Model");
        });

        modelBuilder.Entity<AtomEmbeddingComponent>(entity =>
        {
            entity.ToTable("AtomEmbeddingComponent");

            entity.HasOne(d => d.AtomEmbedding).WithMany(p => p.AtomEmbeddingComponents)
                .HasForeignKey(d => d.AtomEmbeddingId)
                .HasConstraintName("FK_AtomEmbeddingComponents_AtomEmbeddings_AtomEmbeddingId");
        });

        modelBuilder.Entity<AtomEmbeddingSpatialMetadatum>(entity =>
        {
            entity.HasKey(e => e.MetadataId);

            entity.ToTable("AtomEmbeddingSpatialMetadatum");

            entity.HasIndex(e => new { e.SpatialBucketX, e.SpatialBucketY, e.SpatialBucketZ }, "IX_AtomEmbeddingSpatialMetadata_BucketXYZ").HasFilter("([SpatialBucketX] IS NOT NULL)");

            entity.HasIndex(e => new { e.SpatialBucketX, e.SpatialBucketY, e.SpatialBucketZ, e.HasZ }, "UX_AtomEmbeddingSpatialMetadatum_BucketXYZ").IsUnique();

            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(sysutcdatetime())");
        });

        modelBuilder.Entity<AtomGraphEdge>(entity =>
        {
            entity.HasKey(e => e.AtomRelationId);

            entity.ToTable("AtomGraphEdges", "graph");

            entity.HasIndex(e => e.AtomRelationId, "UX_AtomGraphEdges_AtomRelationId").IsUnique();

            entity.Property(e => e.AtomRelationId).ValueGeneratedNever();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.EdgeId47be36ae1c4b4f3bb7f0e08849fa977e)
                .HasMaxLength(1000)
                .HasColumnName("$edge_id_47BE36AE1C4B4F3BB7F0E08849FA977E");
            entity.Property(e => e.FromIdA3fdce16d30d4b9691ba136908f40759)
                .HasMaxLength(1000)
                .HasColumnName("$from_id_A3FDCE16D30D4B9691BA136908F40759");
            entity.Property(e => e.FromIdFc1219066bdc4776b44a5a0412bcf924).HasColumnName("from_id_FC1219066BDC4776B44A5A0412BCF924");
            entity.Property(e => e.FromObjId071043cc800b4943aeffc058465ef7fa).HasColumnName("from_obj_id_071043CC800B4943AEFFC058465EF7FA");
            entity.Property(e => e.GraphId429ab56d0ebd458fb38c43c3b8267534).HasColumnName("graph_id_429AB56D0EBD458FB38C43C3B8267534");
            entity.Property(e => e.Metadata).HasColumnType("json");
            entity.Property(e => e.RelationType).HasMaxLength(128);
            entity.Property(e => e.SpatialExpression).HasColumnType("geometry");
            entity.Property(e => e.ToId579809caf8c941d88f251a1c6ee5fd07)
                .HasMaxLength(1000)
                .HasColumnName("$to_id_579809CAF8C941D88F251A1C6EE5FD07");
            entity.Property(e => e.ToIdF2762fc34e0f412c8fc5f6ed00f26692).HasColumnName("to_id_F2762FC34E0F412C8FC5F6ED00F26692");
            entity.Property(e => e.ToObjId80f993575fbd4a9ea686d1cab73ad763).HasColumnName("to_obj_id_80F993575FBD4A9EA686D1CAB73AD763");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(sysutcdatetime())");
        });

        modelBuilder.Entity<AtomGraphEdge1>(entity =>
        {
            entity.HasKey(e => e.EdgeId).HasName("PK__AtomGrap__DD62104622697664");

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

            entity.HasIndex(e => e.AtomId, "UX_AtomGraphNodes_AtomId").IsUnique();

            entity.Property(e => e.AtomId).ValueGeneratedNever();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.GraphId494f8ff335f24c699e7cd43e9f927f38).HasColumnName("graph_id_494F8FF335F24C699E7CD43E9F927F38");
            entity.Property(e => e.Metadata).HasColumnType("json");
            entity.Property(e => e.Modality).HasMaxLength(64);
            entity.Property(e => e.NodeId4da534f68b2342b9b1d83cbdaea1bdab)
                .HasMaxLength(1000)
                .HasColumnName("$node_id_4DA534F68B2342B9B1D83CBDAEA1BDAB");
            entity.Property(e => e.PayloadLocator).HasMaxLength(512);
            entity.Property(e => e.Semantics).HasColumnType("json");
            entity.Property(e => e.SourceType).HasMaxLength(128);
            entity.Property(e => e.SourceUri).HasMaxLength(2048);
            entity.Property(e => e.SpatialKey).HasColumnType("geometry");
            entity.Property(e => e.Subtype).HasMaxLength(64);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(sysutcdatetime())");
        });

        modelBuilder.Entity<AtomRelation>(entity =>
        {
            entity
                .ToTable("AtomRelation")
                .ToTable(tb => tb.IsTemporal(ttb =>
                    {
                        ttb.UseHistoryTable("AtomRelations_History", "dbo");
                        ttb
                            .HasPeriodStart("ValidFrom")
                            .HasColumnName("ValidFrom");
                        ttb
                            .HasPeriodEnd("ValidTo")
                            .HasColumnName("ValidTo");
                    }));

            entity.HasIndex(e => e.RelationType, "IX_AtomRelation_RelationType");

            entity.HasIndex(e => new { e.SourceAtomId, e.SequenceIndex }, "IX_AtomRelation_SequenceIndex");

            entity.HasIndex(e => new { e.SourceAtomId, e.TargetAtomId }, "IX_AtomRelation_SourceTarget");

            entity.HasIndex(e => e.SpatialBucket, "IX_AtomRelation_SpatialBucket");

            entity.HasIndex(e => new { e.TargetAtomId, e.SourceAtomId }, "IX_AtomRelation_TargetSource");

            entity.HasIndex(e => new { e.TenantId, e.RelationType }, "IX_AtomRelation_Tenant");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Metadata).HasColumnType("json");
            entity.Property(e => e.RelationType).HasMaxLength(128);
            entity.Property(e => e.SpatialExpression).HasColumnType("geometry");

            entity.HasOne(d => d.SourceAtom).WithMany(p => p.AtomRelationSourceAtoms)
                .HasForeignKey(d => d.SourceAtomId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AtomRelations_Atoms_SourceAtomId");

            entity.HasOne(d => d.TargetAtom).WithMany(p => p.AtomRelationTargetAtoms)
                .HasForeignKey(d => d.TargetAtomId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AtomRelations_Atoms_TargetAtomId");
        });

        modelBuilder.Entity<AttentionGenerationLog>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Attentio__3214EC07B16D6C8C");

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
                .HasConstraintName("FK_AttentionGenerationLog_Model");
        });

        modelBuilder.Entity<AttentionInferenceResult>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Attentio__3214EC07C27A7FB2");

            entity.HasIndex(e => e.CreatedAt, "IX_AttentionInferenceResults_CreatedAt").IsDescending();

            entity.HasIndex(e => e.ProblemId, "IX_AttentionInferenceResults_ProblemId");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.ReasoningSteps).HasColumnType("json");

            entity.HasOne(d => d.Model).WithMany(p => p.AttentionInferenceResults)
                .HasForeignKey(d => d.ModelId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AttentionInferenceResult_Model");
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

            entity.Property(e => e.ImprovementId).HasDefaultValueSql("(newid())");
            entity.Property(e => e.ChangeType).HasMaxLength(50);
            entity.Property(e => e.EstimatedImpact).HasMaxLength(20);
            entity.Property(e => e.GitCommitHash).HasMaxLength(64);
            entity.Property(e => e.PerformanceDelta).HasColumnType("decimal(10, 4)");
            entity.Property(e => e.RiskLevel).HasMaxLength(20);
            entity.Property(e => e.SuccessScore).HasColumnType("decimal(5, 4)");
            entity.Property(e => e.TargetFile).HasMaxLength(512);
        });

        modelBuilder.Entity<BackgroundJob>(entity =>
        {
            entity.HasKey(e => e.JobId);

            entity.ToTable("BackgroundJob", tb => tb.HasComment("Background job queue for asynchronous task processing with priority-based execution and retry logic."));

            entity.HasIndex(e => e.CorrelationId, "IX_BackgroundJob_CorrelationId").HasFilter("([CorrelationId] IS NOT NULL)");

            entity.HasIndex(e => new { e.JobType, e.Status }, "IX_BackgroundJob_JobType_Status");

            entity.HasIndex(e => e.ScheduledAtUtc, "IX_BackgroundJob_ScheduledAtUtc").HasFilter("([ScheduledAtUtc] IS NOT NULL)");

            entity.HasIndex(e => new { e.Status, e.Priority, e.CreatedAtUtc }, "IX_BackgroundJob_Status_Priority").IsDescending(false, true, false);

            entity.HasIndex(e => e.TenantId, "IX_BackgroundJob_TenantId").HasFilter("([TenantId] IS NOT NULL)");

            entity.Property(e => e.CompletedAtUtc).HasPrecision(3);
            entity.Property(e => e.CorrelationId).HasMaxLength(128);
            entity.Property(e => e.CreatedAtUtc)
                .HasPrecision(3)
                .HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.CreatedBy).HasMaxLength(256);
            entity.Property(e => e.JobType).HasMaxLength(128);
            entity.Property(e => e.MaxRetries).HasDefaultValue(3);
            entity.Property(e => e.ScheduledAtUtc).HasPrecision(3);
            entity.Property(e => e.StartedAtUtc).HasPrecision(3);
        });

        modelBuilder.Entity<BillingInvoice>(entity =>
        {
            entity.HasKey(e => e.InvoiceId).HasName("PK_BillingInvoices");

            entity.ToTable("BillingInvoice");

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
            entity.HasKey(e => e.MultiplierId).HasName("PK_BillingMultipliers");

            entity.ToTable("BillingMultiplier");

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

            entity.HasOne(d => d.RatePlan).WithMany(p => p.BillingMultipliers)
                .HasForeignKey(d => d.RatePlanId)
                .HasConstraintName("FK_BillingMultipliers_BillingRatePlans_RatePlanId");
        });

        modelBuilder.Entity<BillingOperationRate>(entity =>
        {
            entity.HasKey(e => e.OperationRateId).HasName("PK_BillingOperationRates");

            entity.ToTable("BillingOperationRate");

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

            entity.HasOne(d => d.RatePlan).WithMany(p => p.BillingOperationRates)
                .HasForeignKey(d => d.RatePlanId)
                .HasConstraintName("FK_BillingOperationRates_BillingRatePlans_RatePlanId");
        });

        modelBuilder.Entity<BillingPricingTier>(entity =>
        {
            entity.HasKey(e => e.TierId).HasName("PK_BillingPricingTiers");

            entity.ToTable("BillingPricingTier");

            entity.HasIndex(e => new { e.UsageType, e.UnitType, e.EffectiveFrom }, "IX_BillingPricingTiers_UsageType").IsDescending(false, false, true);

            entity.Property(e => e.CreatedUtc).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(18, 8)");
            entity.Property(e => e.UnitType).HasMaxLength(50);
            entity.Property(e => e.UsageType).HasMaxLength(50);
        });

        modelBuilder.Entity<BillingQuotaViolation>(entity =>
        {
            entity.HasKey(e => e.ViolationId).HasName("PK_BillingQuotaViolations");

            entity.ToTable("BillingQuotaViolation");

            entity.HasIndex(e => new { e.TenantId, e.ViolatedUtc }, "IX_BillingQuotaViolations_Tenant").IsDescending(false, true);

            entity.HasIndex(e => e.Resolved, "IX_BillingQuotaViolations_Unresolved").HasFilter("([Resolved]=(0))");

            entity.Property(e => e.UsageType).HasMaxLength(50);
            entity.Property(e => e.ViolatedUtc).HasDefaultValueSql("(sysutcdatetime())");
        });

        modelBuilder.Entity<BillingRatePlan>(entity =>
        {
            entity.HasKey(e => e.RatePlanId).HasName("PK_BillingRatePlans");

            entity.ToTable("BillingRatePlan");

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

        modelBuilder.Entity<BillingTenantQuotum>(entity =>
        {
            entity.HasKey(e => e.QuotaId).HasName("PK_BillingTenantQuotas");

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

            entity.HasIndex(e => new { e.TenantId, e.TimestampUtc }, "IX_BillingUsageLedger_Tenant").IsDescending(false, true);

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

        modelBuilder.Entity<CachedActivation>(entity =>
        {
            entity.HasKey(e => e.CacheId).HasName("PK_CachedActivations");

            entity.ToTable("CachedActivation");

            entity.Property(e => e.CreatedDate).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.InputHash)
                .HasMaxLength(32)
                .IsFixedLength();
            entity.Property(e => e.LastAccessed).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.OutputShape).HasMaxLength(100);

            entity.HasOne(d => d.Layer).WithMany(p => p.CachedActivations)
                .HasForeignKey(d => d.LayerId)
                .HasConstraintName("FK_CachedActivations_ModelLayers_LayerId");

            entity.HasOne(d => d.Model).WithMany(p => p.CachedActivations)
                .HasForeignKey(d => d.ModelId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CachedActivations_Models_ModelId");
        });

        modelBuilder.Entity<CdcCheckpoint>(entity =>
        {
            entity.HasKey(e => new { e.ConsumerGroup, e.PartitionId }).HasName("PK_CdcCheckpoints");

            entity.ToTable("CdcCheckpoint");

            entity.Property(e => e.ConsumerGroup).HasMaxLength(100);
            entity.Property(e => e.PartitionId).HasMaxLength(50);
            entity.Property(e => e.LastModified).HasDefaultValueSql("(sysutcdatetime())", "DF_CdcCheckpoints_LastModified");
        });

        modelBuilder.Entity<Cicdbuild>(entity =>
        {
            entity.HasKey(e => e.BuildId).HasName("PK_CICDBuilds");

            entity.ToTable("CICDBuild");

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
            entity.HasKey(e => e.CodeAtomId).HasName("PK_CodeAtoms");

            entity.ToTable("CodeAtom");

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

            entity.HasIndex(e => e.CentroidSpatialKey, "SIX_Concepts_CentroidSpatialKey");

            entity.HasIndex(e => e.ConceptDomain, "SIX_Concepts_ConceptDomain");

            entity.Property(e => e.CentroidSpatialKey).HasColumnType("geometry");
            entity.Property(e => e.ConceptDomain).HasColumnType("geometry");
            entity.Property(e => e.ConceptName).HasMaxLength(200);
            entity.Property(e => e.DiscoveredAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.DiscoveryMethod).HasMaxLength(100);
            entity.Property(e => e.IsActive).HasDefaultValue(true);

            entity.HasOne(d => d.Model).WithMany(p => p.Concepts)
                .HasForeignKey(d => d.ModelId)
                .HasConstraintName("FK_Concepts_Models_ModelId");
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
            entity.HasKey(e => e.DeduplicationPolicyId).HasName("PK_DeduplicationPolicies");

            entity.ToTable("DeduplicationPolicy");

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
            entity.HasKey(e => e.Id).HasName("PK__EventAto__3214EC07893214E8");

            entity.HasIndex(e => e.ClusterId, "IX_EventAtoms_ClusterId");

            entity.HasIndex(e => e.CreatedAt, "IX_EventAtoms_CreatedAt").IsDescending();

            entity.HasIndex(e => e.EventType, "IX_EventAtoms_EventType");

            entity.HasIndex(e => e.StreamId, "IX_EventAtoms_StreamId");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.EventType).HasMaxLength(100);

            entity.HasOne(d => d.CentroidAtom).WithMany(p => p.EventAtoms)
                .HasForeignKey(d => d.CentroidAtomId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_EventAtom_Atom");

            entity.HasOne(d => d.Stream).WithMany(p => p.EventAtoms)
                .HasForeignKey(d => d.StreamId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_EventAtoms_StreamOrchestration");
        });

        modelBuilder.Entity<EventGenerationResult>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__EventGen__3214EC07480A58DD");

            entity.HasIndex(e => e.CreatedAt, "IX_EventGenerationResults_CreatedAt").IsDescending();

            entity.HasIndex(e => e.EventType, "IX_EventGenerationResults_EventType");

            entity.HasIndex(e => e.StreamId, "IX_EventGenerationResults_StreamId");

            entity.Property(e => e.ClusteringMethod).HasMaxLength(50);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.EventType).HasMaxLength(100);

            entity.HasOne(d => d.Stream).WithMany(p => p.EventGenerationResults)
                .HasForeignKey(d => d.StreamId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__EventGene__Strea__324172E1");
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
            entity.HasKey(e => e.SegmentId).HasName("PK_GenerationStreamSegments");

            entity.ToTable("GenerationStreamSegment");

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

        modelBuilder.Entity<InferenceCache>(entity =>
        {
            entity.HasKey(e => e.CacheId);

            entity.ToTable("InferenceCache");

            entity.Property(e => e.CacheKey).HasMaxLength(64);
            entity.Property(e => e.CreatedUtc).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.InferenceType).HasMaxLength(100);

            entity.HasOne(d => d.Model).WithMany(p => p.InferenceCaches)
                .HasForeignKey(d => d.ModelId)
                .HasConstraintName("FK_InferenceCache_Models_ModelId");
        });

        modelBuilder.Entity<InferenceRequest>(entity =>
        {
            entity.HasKey(e => e.InferenceId);

            entity.ToTable("InferenceRequest");

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

            entity.HasOne(d => d.Model).WithMany(p => p.InferenceRequests)
                .HasForeignKey(d => d.ModelId)
                .HasConstraintName("FK_InferenceRequests_Models_ModelId");
        });

        modelBuilder.Entity<InferenceStep>(entity =>
        {
            entity.HasKey(e => e.StepId).HasName("PK_InferenceSteps");

            entity.ToTable("InferenceStep");

            entity.Property(e => e.IndexUsed).HasMaxLength(200);
            entity.Property(e => e.OperationType).HasMaxLength(50);

            entity.HasOne(d => d.Inference).WithMany(p => p.InferenceSteps)
                .HasForeignKey(d => d.InferenceId)
                .HasConstraintName("FK_InferenceSteps_InferenceRequests_InferenceId");

            entity.HasOne(d => d.Model).WithMany(p => p.InferenceSteps)
                .HasForeignKey(d => d.ModelId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_InferenceSteps_Models_ModelId");
        });

        modelBuilder.Entity<IngestionJob>(entity =>
        {
            entity.HasKey(e => e.IngestionJobId).HasName("PK_IngestionJobs");

            entity.ToTable("IngestionJob");

            entity.HasIndex(e => new { e.JobStatus, e.TenantId }, "IX_IngestionJobs_Status");

            entity.HasIndex(e => new { e.TenantId, e.CreatedAt }, "IX_IngestionJobs_TenantId").IsDescending(false, true);

            entity.Property(e => e.AtomChunkSize).HasDefaultValue(1000000);
            entity.Property(e => e.AtomQuota).HasDefaultValueSql("((5000000000.))");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.JobStatus)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValue("Pending");
            entity.Property(e => e.LastUpdatedAt).HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.ParentAtom).WithMany(p => p.IngestionJobs)
                .HasForeignKey(d => d.ParentAtomId)
                .HasConstraintName("FK_IngestionJobs_ParentAtom");
        });

        modelBuilder.Entity<IngestionJobAtom>(entity =>
        {
            entity.HasKey(e => e.IngestionJobAtomId).HasName("PK_IngestionJobAtoms");

            entity.ToTable("IngestionJobAtom");

            entity.Property(e => e.Notes).HasMaxLength(1024);

            entity.HasOne(d => d.Atom).WithMany(p => p.IngestionJobAtoms)
                .HasForeignKey(d => d.AtomId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_IngestionJobAtoms_Atoms_AtomId");

            entity.HasOne(d => d.IngestionJob).WithMany(p => p.IngestionJobAtoms)
                .HasForeignKey(d => d.IngestionJobId)
                .HasConstraintName("FK_IngestionJobAtoms_IngestionJobs_IngestionJobId");
        });

        modelBuilder.Entity<Model>(entity =>
        {
            entity.ToTable("Model");

            entity.HasIndex(e => e.ModelName, "IX_Model_ModelName");

            entity.HasIndex(e => e.ModelType, "IX_Model_ModelType");

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

            entity.ToTable("ModelLayer");

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
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_ModelLayers_Atoms_LayerAtomId");

            entity.HasOne(d => d.Model).WithMany(p => p.ModelLayers)
                .HasForeignKey(d => d.ModelId)
                .HasConstraintName("FK_ModelLayers_Models_ModelId");
        });

        modelBuilder.Entity<ModelMetadatum>(entity =>
        {
            entity.HasKey(e => e.MetadataId);

            entity.Property(e => e.License).HasMaxLength(100);
            entity.Property(e => e.PerformanceMetrics).HasColumnType("json");
            entity.Property(e => e.SourceUrl).HasMaxLength(500);
            entity.Property(e => e.SupportedModalities).HasColumnType("json");
            entity.Property(e => e.SupportedTasks).HasColumnType("json");
            entity.Property(e => e.TrainingDataset).HasMaxLength(500);

            entity.HasOne(d => d.Model).WithMany(p => p.ModelMetadata).HasForeignKey(d => d.ModelId);
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
            entity.HasKey(e => e.Id).HasName("PK__MultiPat__3214EC07DBB07D7C");

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
            entity.HasKey(e => e.Id).HasName("PK__Operatio__3214EC0763D18795");

            entity.ToTable("OperationProvenance");

            entity.HasIndex(e => e.CreatedAt, "IX_OperationProvenance_CreatedAt").IsDescending();

            entity.HasIndex(e => e.OperationId, "IX_OperationProvenance_OperationId");

            entity.HasIndex(e => e.OperationId, "UQ__Operatio__A4F5FC45A4EF3184").IsUnique();

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
        });

        modelBuilder.Entity<PendingAction>(entity =>
        {
            entity.HasKey(e => e.ActionId);

            entity.HasIndex(e => e.CreatedUtc, "IX_PendingActions_Created").IsDescending();

            entity.HasIndex(e => new { e.Priority, e.CreatedUtc }, "IX_PendingActions_Priority")
                .IsDescending()
                .HasFilter("([Status]='PendingApproval')");

            entity.HasIndex(e => e.Status, "IX_PendingActions_Status");

            entity.Property(e => e.ActionType).HasMaxLength(100);
            entity.Property(e => e.ApprovedBy).HasMaxLength(128);
            entity.Property(e => e.CreatedUtc).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.EstimatedImpact).HasMaxLength(20);
            entity.Property(e => e.Priority).HasDefaultValue(5);
            entity.Property(e => e.RiskLevel)
                .HasMaxLength(20)
                .HasDefaultValue("medium");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasDefaultValue("PendingApproval");
        });

        modelBuilder.Entity<ProvenanceAuditResult>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Provenan__3214EC0718FF022C");

            entity.HasIndex(e => new { e.AuditPeriodStart, e.AuditPeriodEnd }, "IX_ProvenanceAuditResults_AuditPeriod");

            entity.HasIndex(e => e.AuditedAt, "IX_ProvenanceAuditResults_AuditedAt").IsDescending();

            entity.Property(e => e.Anomalies).HasColumnType("json");
            entity.Property(e => e.AuditedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Scope).HasMaxLength(100);
        });

        modelBuilder.Entity<ProvenanceValidationResult>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Provenan__3214EC07DD157CE6");

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
            entity.HasKey(e => e.Id).HasName("PK__Reasonin__3214EC072588B4EB");

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
            entity.HasKey(e => e.Id).HasName("PK__SelfCons__3214EC07E5D799DB");

            entity.HasIndex(e => e.CreatedAt, "IX_SelfConsistencyResults_CreatedAt").IsDescending();

            entity.HasIndex(e => e.ProblemId, "IX_SelfConsistencyResults_ProblemId");

            entity.Property(e => e.ConsensusMetrics).HasColumnType("json");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.SampleData).HasColumnType("json");
        });

        modelBuilder.Entity<SemanticFeature>(entity =>
        {
            entity.HasKey(e => e.AtomEmbeddingId).HasName("PK__Semantic__AB86F6307F012076");

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
                .HasConstraintName("FK_SemanticFeature_AtomEmbedding");
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
            entity.HasKey(e => e.Id).HasName("PK__StreamFu__3214EC0761255901");

            entity.HasIndex(e => e.CreatedAt, "IX_StreamFusionResults_CreatedAt").IsDescending();

            entity.HasIndex(e => e.FusionType, "IX_StreamFusionResults_FusionType");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.FusionType).HasMaxLength(50);
            entity.Property(e => e.StreamIds).HasColumnType("json");
            entity.Property(e => e.Weights).HasColumnType("json");
        });

        modelBuilder.Entity<StreamOrchestrationResult>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__StreamOr__3214EC07DEB10CE9");

            entity.HasIndex(e => e.CreatedAt, "IX_StreamOrchestrationResults_CreatedAt").IsDescending();

            entity.HasIndex(e => e.SensorType, "IX_StreamOrchestrationResults_SensorType");

            entity.HasIndex(e => new { e.TimeWindowStart, e.TimeWindowEnd }, "IX_StreamOrchestrationResults_TimeWindow");

            entity.Property(e => e.AggregationLevel).HasMaxLength(50);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.SensorType).HasMaxLength(100);
        });

        modelBuilder.Entity<TenantAtom>(entity =>
        {
            entity.HasKey(e => new { e.TenantId, e.AtomId }).HasName("PK_TenantAtoms");

            entity.ToTable("TenantAtom");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())", "DF_TenantAtoms_CreatedAt");

            entity.HasOne(d => d.Atom).WithMany(p => p.TenantAtoms)
                .HasForeignKey(d => d.AtomId)
                .HasConstraintName("FK_TenantAtoms_Atoms");
        });

        modelBuilder.Entity<TenantGuidMapping>(entity =>
        {
            entity.HasKey(e => e.TenantId);

            entity.ToTable("TenantGuidMapping", tb => tb.HasComment("Maps Azure Entra External ID tenant GUIDs to internal integer tenant IDs. Replaces unsafe GetHashCode() approach. Each Azure AD tenant GUID gets a stable, unique integer ID for use throughout the system."));

            entity.HasIndex(e => e.IsActive, "IX_TenantGuidMapping_IsActive");

            entity.HasIndex(e => e.TenantGuid, "UQ_TenantGuidMapping_TenantGuid").IsUnique();

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())", "DF_TenantGuidMapping_CreatedAt");
            entity.Property(e => e.CreatedBy).HasMaxLength(100);
            entity.Property(e => e.IsActive).HasDefaultValue(true, "DF_TenantGuidMapping_IsActive");
            entity.Property(e => e.ModifiedBy).HasMaxLength(100);
            entity.Property(e => e.TenantName).HasMaxLength(200);
        });

        modelBuilder.Entity<TenantSecurityPolicy>(entity =>
        {
            entity.HasKey(e => e.PolicyId);

            entity.ToTable("TenantSecurityPolicy");

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
            entity.ToTable("TensorAtom");

            entity.Property(e => e.AtomType).HasMaxLength(128);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.GeometryFootprint).HasColumnType("geometry");
            entity.Property(e => e.Metadata).HasColumnType("json");
            entity.Property(e => e.SpatialSignature).HasColumnType("geometry");

            entity.HasOne(d => d.Atom).WithMany(p => p.TensorAtoms)
                .HasForeignKey(d => d.AtomId)
                .HasConstraintName("FK_TensorAtoms_Atoms_AtomId");

            entity.HasOne(d => d.Layer).WithMany(p => p.TensorAtoms)
                .HasForeignKey(d => d.LayerId)
                .HasConstraintName("FK_TensorAtoms_ModelLayers_LayerId");

            entity.HasOne(d => d.Model).WithMany(p => p.TensorAtoms)
                .HasForeignKey(d => d.ModelId)
                .HasConstraintName("FK_TensorAtoms_Models_ModelId");
        });

        modelBuilder.Entity<TensorAtomCoefficient>(entity =>
        {
            entity.HasKey(e => new { e.TensorAtomId, e.ModelId, e.LayerIdx, e.PositionX, e.PositionY, e.PositionZ }).HasName("PK_TensorAtomCoefficients");

            entity
                .ToTable("TensorAtomCoefficient")
                .ToTable(tb => tb.IsTemporal(ttb =>
                    {
                        ttb.UseHistoryTable("TensorAtomCoefficients_History", "dbo");
                        ttb
                            .HasPeriodStart("ValidFrom")
                            .HasColumnName("ValidFrom");
                        ttb
                            .HasPeriodEnd("ValidTo")
                            .HasColumnName("ValidTo");
                    }));

            entity.HasIndex(e => e.SpatialKey, "SIX_TensorAtomCoefficients_SpatialKey");

            entity.Property(e => e.SpatialKey)
                .HasComputedColumnSql("([GEOMETRY]::Point([PositionX],[PositionY],(0)))", true)
                .HasColumnType("geometry");
            entity.Property(e => e.TensorRole).HasMaxLength(128);

            entity.HasOne(d => d.Model).WithMany(p => p.TensorAtomCoefficients)
                .HasForeignKey(d => d.ModelId)
                .HasConstraintName("FK_TensorAtomCoefficients_Model");

            entity.HasOne(d => d.TensorAtom).WithMany(p => p.TensorAtomCoefficients)
                .HasForeignKey(d => d.TensorAtomId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TensorAtomCoefficients_Atom");
        });

        modelBuilder.Entity<TestResult>(entity =>
        {
            entity.HasKey(e => e.TestResultId).HasName("PK_TestResults");

            entity.ToTable("TestResult");

            entity.Property(e => e.Environment).HasMaxLength(100);
            entity.Property(e => e.ExecutedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.MemoryUsageMb).HasColumnName("MemoryUsageMB");
            entity.Property(e => e.TestCategory).HasMaxLength(50);
            entity.Property(e => e.TestName).HasMaxLength(200);
            entity.Property(e => e.TestStatus).HasMaxLength(50);
            entity.Property(e => e.TestSuite).HasMaxLength(100);
        });

        modelBuilder.Entity<TokenVocabulary>(entity =>
        {
            entity.HasKey(e => e.VocabId);

            entity.ToTable("TokenVocabulary");

            entity.Property(e => e.Embedding).HasMaxLength(1998);
            entity.Property(e => e.Token).HasMaxLength(100);
            entity.Property(e => e.TokenType).HasMaxLength(20);

            entity.HasOne(d => d.Model).WithMany(p => p.TokenVocabularies)
                .HasForeignKey(d => d.ModelId)
                .HasConstraintName("FK_TokenVocabulary_Models_ModelId");
        });

        modelBuilder.Entity<TopicKeyword>(entity =>
        {
            entity.HasKey(e => e.KeywordId).HasName("PK__TopicKey__03E8D7CF773B24EF");

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
            entity.HasKey(e => e.Id).HasName("PK__Transfor__3214EC07B78E1F99");

            entity.HasIndex(e => e.CreatedAt, "IX_TransformerInferenceResults_CreatedAt").IsDescending();

            entity.HasIndex(e => e.ProblemId, "IX_TransformerInferenceResults_ProblemId");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.LayerResults).HasColumnType("json");

            entity.HasOne(d => d.Model).WithMany(p => p.TransformerInferenceResults)
                .HasForeignKey(d => d.ModelId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TransformerInferenceResult_Model");
        });

        modelBuilder.Entity<VwReconstructModelLayerWeight>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_ReconstructModelLayerWeights");

            entity.Property(e => e.LayerName).HasMaxLength(100);
            entity.Property(e => e.ModelName).HasMaxLength(200);
            entity.Property(e => e.WeightValueBinary).HasMaxLength(64);
        });

        modelBuilder.Entity<WeightSnapshot>(entity =>
        {
            entity.HasKey(e => e.SnapshotId).HasName("PK__WeightSn__664F572B5DD10DC0");

            entity.ToTable("WeightSnapshot");

            entity.HasIndex(e => e.SnapshotName, "UQ__WeightSn__FAC0EC4A209B715F").IsUnique();

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.SnapshotName).HasMaxLength(255);
            entity.Property(e => e.SnapshotTime).HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.Model).WithMany(p => p.WeightSnapshots)
                .HasForeignKey(d => d.ModelId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_WeightSnapshot_Model");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
