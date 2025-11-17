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

    public virtual DbSet<AgentTools> AgentTools { get; set; }

    public virtual DbSet<Atom> Atom { get; set; }

    public virtual DbSet<AtomComposition> AtomComposition { get; set; }

    public virtual DbSet<AtomConcepts> AtomConcepts { get; set; }

    public virtual DbSet<AtomEmbedding> AtomEmbedding { get; set; }

    public virtual DbSet<AtomEmbeddingComponent> AtomEmbeddingComponent { get; set; }

    public virtual DbSet<AtomEmbeddingSpatialMetadatum> AtomEmbeddingSpatialMetadatum { get; set; }

    public virtual DbSet<AtomGraphEdges> AtomGraphEdges { get; set; }

    public virtual DbSet<AtomGraphEdges1> AtomGraphEdges1 { get; set; }

    public virtual DbSet<AtomGraphNodes> AtomGraphNodes { get; set; }

    public virtual DbSet<AtomRelation> AtomRelation { get; set; }

    public virtual DbSet<AttentionGenerationLog> AttentionGenerationLog { get; set; }

    public virtual DbSet<AttentionInferenceResults> AttentionInferenceResults { get; set; }

    public virtual DbSet<AutonomousComputeJobs> AutonomousComputeJobs { get; set; }

    public virtual DbSet<AutonomousImprovementHistory> AutonomousImprovementHistory { get; set; }

    public virtual DbSet<BackgroundJob> BackgroundJob { get; set; }

    public virtual DbSet<BillingInvoice> BillingInvoice { get; set; }

    public virtual DbSet<BillingMultiplier> BillingMultiplier { get; set; }

    public virtual DbSet<BillingOperationRate> BillingOperationRate { get; set; }

    public virtual DbSet<BillingPricingTier> BillingPricingTier { get; set; }

    public virtual DbSet<BillingQuotaViolation> BillingQuotaViolation { get; set; }

    public virtual DbSet<BillingRatePlan> BillingRatePlan { get; set; }

    public virtual DbSet<BillingTenantQuota> BillingTenantQuota { get; set; }

    public virtual DbSet<BillingUsageLedger> BillingUsageLedger { get; set; }

    public virtual DbSet<CachedActivation> CachedActivation { get; set; }

    public virtual DbSet<CdcCheckpoint> CdcCheckpoint { get; set; }

    public virtual DbSet<Cicdbuild> Cicdbuild { get; set; }

    public virtual DbSet<CodeAtom> CodeAtom { get; set; }

    public virtual DbSet<ConceptEvolution> ConceptEvolution { get; set; }

    public virtual DbSet<Concepts> Concepts { get; set; }

    public virtual DbSet<DeduplicationPolicy> DeduplicationPolicy { get; set; }

    public virtual DbSet<EmbeddingMigrationProgress> EmbeddingMigrationProgress { get; set; }

    public virtual DbSet<EventAtoms> EventAtoms { get; set; }

    public virtual DbSet<EventGenerationResults> EventGenerationResults { get; set; }

    public virtual DbSet<EventHubCheckpoints> EventHubCheckpoints { get; set; }

    public virtual DbSet<GenerationStreamSegment> GenerationStreamSegment { get; set; }

    public virtual DbSet<GenerationStreams> GenerationStreams { get; set; }

    public virtual DbSet<InferenceCache> InferenceCache { get; set; }

    public virtual DbSet<InferenceRequest> InferenceRequest { get; set; }

    public virtual DbSet<InferenceStep> InferenceStep { get; set; }

    public virtual DbSet<IngestionJob> IngestionJob { get; set; }

    public virtual DbSet<IngestionJobAtom> IngestionJobAtom { get; set; }

    public virtual DbSet<Model> Model { get; set; }

    public virtual DbSet<ModelLayer> ModelLayer { get; set; }

    public virtual DbSet<ModelMetadata> ModelMetadata { get; set; }

    public virtual DbSet<ModelVersionHistory> ModelVersionHistory { get; set; }

    public virtual DbSet<MultiPathReasoning> MultiPathReasoning { get; set; }

    public virtual DbSet<Neo4jSyncLog> Neo4jSyncLog { get; set; }

    public virtual DbSet<OperationProvenance> OperationProvenance { get; set; }

    public virtual DbSet<PendingActions> PendingActions { get; set; }

    public virtual DbSet<ProvenanceAuditResults> ProvenanceAuditResults { get; set; }

    public virtual DbSet<ProvenanceValidationResults> ProvenanceValidationResults { get; set; }

    public virtual DbSet<ReasoningChains> ReasoningChains { get; set; }

    public virtual DbSet<SelfConsistencyResults> SelfConsistencyResults { get; set; }

    public virtual DbSet<SemanticFeatures> SemanticFeatures { get; set; }

    public virtual DbSet<SessionPaths> SessionPaths { get; set; }

    public virtual DbSet<SpatialLandmarks> SpatialLandmarks { get; set; }

    public virtual DbSet<Status> Status { get; set; }

    public virtual DbSet<StreamFusionResults> StreamFusionResults { get; set; }

    public virtual DbSet<StreamOrchestrationResults> StreamOrchestrationResults { get; set; }

    public virtual DbSet<TenantAtom> TenantAtom { get; set; }

    public virtual DbSet<TenantGuidMapping> TenantGuidMapping { get; set; }

    public virtual DbSet<TenantSecurityPolicy> TenantSecurityPolicy { get; set; }

    public virtual DbSet<TensorAtom> TensorAtom { get; set; }

    public virtual DbSet<TensorAtomCoefficient> TensorAtomCoefficient { get; set; }

    public virtual DbSet<TestResult> TestResult { get; set; }

    public virtual DbSet<TokenVocabulary> TokenVocabulary { get; set; }

    public virtual DbSet<TopicKeywords> TopicKeywords { get; set; }

    public virtual DbSet<TransformerInferenceResults> TransformerInferenceResults { get; set; }

    public virtual DbSet<VwReconstructModelLayerWeights> VwReconstructModelLayerWeights { get; set; }

    public virtual DbSet<WeightSnapshot> WeightSnapshot { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AgentTools>(entity =>
        {
            entity.HasKey(e => e.ToolId).HasName("PK__AgentToo__CC0CEB91508E4679");

            entity.HasIndex(e => e.ToolName, "UQ__AgentToo__006DA2713DA67B8A").IsUnique();

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
            entity.ToTable(tb => tb.IsTemporal(ttb =>
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

            entity.Property(e => e.SpatialKey).HasColumnType("geometry");

            entity.HasOne(d => d.ComponentAtom).WithMany(p => p.AtomCompositionComponentAtom)
                .HasForeignKey(d => d.ComponentAtomId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AtomCompositions_Component");

            entity.HasOne(d => d.ParentAtom).WithMany(p => p.AtomCompositionParentAtom)
                .HasForeignKey(d => d.ParentAtomId)
                .HasConstraintName("FK_AtomCompositions_Parent");
        });

        modelBuilder.Entity<AtomConcepts>(entity =>
        {
            entity.HasKey(e => e.AtomConceptId);

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

            entity.HasOne(d => d.Atom).WithMany(p => p.AtomEmbedding)
                .HasForeignKey(d => d.AtomId)
                .HasConstraintName("FK_AtomEmbeddings_Atom");

            entity.HasOne(d => d.Model).WithMany(p => p.AtomEmbedding)
                .HasForeignKey(d => d.ModelId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AtomEmbeddings_Model");
        });

        modelBuilder.Entity<AtomEmbeddingComponent>(entity =>
        {
            entity.HasOne(d => d.AtomEmbedding).WithMany(p => p.AtomEmbeddingComponent)
                .HasForeignKey(d => d.AtomEmbeddingId)
                .HasConstraintName("FK_AtomEmbeddingComponents_AtomEmbeddings_AtomEmbeddingId");
        });

        modelBuilder.Entity<AtomEmbeddingSpatialMetadatum>(entity =>
        {
            entity.HasKey(e => e.MetadataId);

            entity.HasIndex(e => new { e.SpatialBucketX, e.SpatialBucketY, e.SpatialBucketZ }, "IX_AtomEmbeddingSpatialMetadata_BucketXYZ").HasFilter("([SpatialBucketX] IS NOT NULL)");

            entity.HasIndex(e => new { e.SpatialBucketX, e.SpatialBucketY, e.SpatialBucketZ, e.HasZ }, "UX_AtomEmbeddingSpatialMetadatum_BucketXYZ").IsUnique();

            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(sysutcdatetime())");
        });

        modelBuilder.Entity<AtomGraphEdges>(entity =>
        {
            entity.HasKey(e => e.AtomRelationId);

            entity.ToTable("AtomGraphEdges", "graph");

            entity.HasIndex(e => e.AtomRelationId, "UX_AtomGraphEdges_AtomRelationId").IsUnique();

            entity.Property(e => e.AtomRelationId).ValueGeneratedNever();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.EdgeId6854515447584ef793d8b88940f42811)
                .HasMaxLength(1000)
                .HasColumnName("$edge_id_6854515447584EF793D8B88940F42811");
            entity.Property(e => e.FromId33a86cbf5fef41cb8bd2367f9f7fb292).HasColumnName("from_id_33A86CBF5FEF41CB8BD2367F9F7FB292");
            entity.Property(e => e.FromId8832880a53e242528f26affcb9f6dde9)
                .HasMaxLength(1000)
                .HasColumnName("$from_id_8832880A53E242528F26AFFCB9F6DDE9");
            entity.Property(e => e.FromObjId44e30bdc381441b480860e5c26c9cbe6).HasColumnName("from_obj_id_44E30BDC381441B480860E5C26C9CBE6");
            entity.Property(e => e.GraphId54ce94e6f75d41f68f1e2b740e8ed972).HasColumnName("graph_id_54CE94E6F75D41F68F1E2B740E8ED972");
            entity.Property(e => e.Metadata).HasColumnType("json");
            entity.Property(e => e.RelationType).HasMaxLength(128);
            entity.Property(e => e.SpatialExpression).HasColumnType("geometry");
            entity.Property(e => e.ToId7d4d4ca90296499ab2a761f4032ef0c7).HasColumnName("to_id_7D4D4CA90296499AB2A761F4032EF0C7");
            entity.Property(e => e.ToId848cfeedb29947e18ecbc48b9a9fed21)
                .HasMaxLength(1000)
                .HasColumnName("$to_id_848CFEEDB29947E18ECBC48B9A9FED21");
            entity.Property(e => e.ToObjId25d973ef142042ba981b6cd8bbad1beb).HasColumnName("to_obj_id_25D973EF142042BA981B6CD8BBAD1BEB");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(sysutcdatetime())");
        });

        modelBuilder.Entity<AtomGraphEdges1>(entity =>
        {
            entity.HasKey(e => e.EdgeId).HasName("PK__AtomGrap__DD621046738D1668");

            entity.ToTable("AtomGraphEdges", "provenance");

            entity.HasIndex(e => e.DependencyType, "IX_AtomGraphEdges_DependencyType");

            entity.HasIndex(e => e.FromAtomId, "IX_AtomGraphEdges_FromId");

            entity.HasIndex(e => e.ToAtomId, "IX_AtomGraphEdges_ToId");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.DependencyType).HasMaxLength(50);
            entity.Property(e => e.EdgeType).HasMaxLength(50);
        });

        modelBuilder.Entity<AtomGraphNodes>(entity =>
        {
            entity.HasKey(e => e.AtomId);

            entity.ToTable("AtomGraphNodes", "graph");

            entity.HasIndex(e => e.AtomId, "UX_AtomGraphNodes_AtomId").IsUnique();

            entity.Property(e => e.AtomId).ValueGeneratedNever();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.GraphId8316578acbaa4d43b0aea45baf11ee8a).HasColumnName("graph_id_8316578ACBAA4D43B0AEA45BAF11EE8A");
            entity.Property(e => e.Metadata).HasColumnType("json");
            entity.Property(e => e.Modality).HasMaxLength(64);
            entity.Property(e => e.NodeIdE82207a9821c447c97a4a1ba75b3025d)
                .HasMaxLength(1000)
                .HasColumnName("$node_id_E82207A9821C447C97A4A1BA75B3025D");
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

            entity.HasOne(d => d.SourceAtom).WithMany(p => p.AtomRelationSourceAtom)
                .HasForeignKey(d => d.SourceAtomId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AtomRelations_Atoms_SourceAtomId");

            entity.HasOne(d => d.TargetAtom).WithMany(p => p.AtomRelationTargetAtom)
                .HasForeignKey(d => d.TargetAtomId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AtomRelations_Atoms_TargetAtomId");
        });

        modelBuilder.Entity<AttentionGenerationLog>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Attentio__3214EC0735DE1450");

            entity.HasIndex(e => e.CreatedAt, "IX_AttentionGenerationLog_CreatedAt").IsDescending();

            entity.HasIndex(e => e.GenerationStreamId, "IX_AttentionGenerationLog_GenerationStreamId");

            entity.HasIndex(e => e.ModelId, "IX_AttentionGenerationLog_ModelId");

            entity.Property(e => e.ContextJson).HasColumnType("json");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.GeneratedAtomIds).HasColumnType("json");
            entity.Property(e => e.InputAtomIds).HasColumnType("json");

            entity.HasOne(d => d.Model).WithMany(p => p.AttentionGenerationLog)
                .HasForeignKey(d => d.ModelId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AttentionGenerationLog_Model");
        });

        modelBuilder.Entity<AttentionInferenceResults>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Attentio__3214EC07D5BF6FF8");

            entity.HasIndex(e => e.CreatedAt, "IX_AttentionInferenceResults_CreatedAt").IsDescending();

            entity.HasIndex(e => e.ProblemId, "IX_AttentionInferenceResults_ProblemId");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.ReasoningSteps).HasColumnType("json");

            entity.HasOne(d => d.Model).WithMany(p => p.AttentionInferenceResults)
                .HasForeignKey(d => d.ModelId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AttentionInferenceResult_Model");
        });

        modelBuilder.Entity<AutonomousComputeJobs>(entity =>
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

            entity.ToTable(tb => tb.HasComment("Background job queue for asynchronous task processing with priority-based execution and retry logic."));

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

            entity.HasOne(d => d.RatePlan).WithMany(p => p.BillingMultiplier)
                .HasForeignKey(d => d.RatePlanId)
                .HasConstraintName("FK_BillingMultipliers_BillingRatePlans_RatePlanId");
        });

        modelBuilder.Entity<BillingOperationRate>(entity =>
        {
            entity.HasKey(e => e.OperationRateId).HasName("PK_BillingOperationRates");

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

            entity.HasOne(d => d.RatePlan).WithMany(p => p.BillingOperationRate)
                .HasForeignKey(d => d.RatePlanId)
                .HasConstraintName("FK_BillingOperationRates_BillingRatePlans_RatePlanId");
        });

        modelBuilder.Entity<BillingPricingTier>(entity =>
        {
            entity.HasKey(e => e.TierId).HasName("PK_BillingPricingTiers");

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

            entity.HasIndex(e => new { e.TenantId, e.ViolatedUtc }, "IX_BillingQuotaViolations_Tenant").IsDescending(false, true);

            entity.HasIndex(e => e.Resolved, "IX_BillingQuotaViolations_Unresolved").HasFilter("([Resolved]=(0))");

            entity.Property(e => e.UsageType).HasMaxLength(50);
            entity.Property(e => e.ViolatedUtc).HasDefaultValueSql("(sysutcdatetime())");
        });

        modelBuilder.Entity<BillingRatePlan>(entity =>
        {
            entity.HasKey(e => e.RatePlanId).HasName("PK_BillingRatePlans");

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

            entity.Property(e => e.CreatedDate).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.InputHash)
                .HasMaxLength(32)
                .IsFixedLength();
            entity.Property(e => e.LastAccessed).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.OutputShape).HasMaxLength(100);

            entity.HasOne(d => d.Layer).WithMany(p => p.CachedActivation)
                .HasForeignKey(d => d.LayerId)
                .HasConstraintName("FK_CachedActivations_ModelLayers_LayerId");

            entity.HasOne(d => d.Model).WithMany(p => p.CachedActivation)
                .HasForeignKey(d => d.ModelId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CachedActivations_Models_ModelId");
        });

        modelBuilder.Entity<CdcCheckpoint>(entity =>
        {
            entity.HasKey(e => new { e.ConsumerGroup, e.PartitionId }).HasName("PK_CdcCheckpoints");

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

        modelBuilder.Entity<ConceptEvolution>(entity =>
        {
            entity.HasKey(e => e.EvolutionId);

            entity.ToTable("ConceptEvolution", "provenance");

            entity.HasIndex(e => new { e.ConceptId, e.RecordedAt }, "IX_ConceptEvolution_ConceptId_RecordedAt").IsDescending(false, true);

            entity.Property(e => e.EvolutionReason).HasMaxLength(200);
            entity.Property(e => e.EvolutionType).HasMaxLength(50);
            entity.Property(e => e.RecordedAt).HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.Concept).WithMany(p => p.ConceptEvolution)
                .HasForeignKey(d => d.ConceptId)
                .HasConstraintName("FK_ConceptEvolution_Concepts");
        });

        modelBuilder.Entity<Concepts>(entity =>
        {
            entity.HasKey(e => e.ConceptId);

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

        modelBuilder.Entity<DeduplicationPolicy>(entity =>
        {
            entity.HasKey(e => e.DeduplicationPolicyId).HasName("PK_DeduplicationPolicies");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Metadata).HasColumnType("json");
            entity.Property(e => e.PolicyName).HasMaxLength(128);
        });

        modelBuilder.Entity<EmbeddingMigrationProgress>(entity =>
        {
            entity.HasKey(e => e.AtomEmbeddingId);

            entity.Property(e => e.AtomEmbeddingId).ValueGeneratedNever();
            entity.Property(e => e.MigratedAt).HasDefaultValueSql("(sysutcdatetime())");
        });

        modelBuilder.Entity<EventAtoms>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__EventAto__3214EC076D90EE2A");

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

        modelBuilder.Entity<EventGenerationResults>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__EventGen__3214EC07EC0EC134");

            entity.HasIndex(e => e.CreatedAt, "IX_EventGenerationResults_CreatedAt").IsDescending();

            entity.HasIndex(e => e.EventType, "IX_EventGenerationResults_EventType");

            entity.HasIndex(e => e.StreamId, "IX_EventGenerationResults_StreamId");

            entity.Property(e => e.ClusteringMethod).HasMaxLength(50);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.EventType).HasMaxLength(100);

            entity.HasOne(d => d.Stream).WithMany(p => p.EventGenerationResults)
                .HasForeignKey(d => d.StreamId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__EventGene__Strea__2B947552");
        });

        modelBuilder.Entity<EventHubCheckpoints>(entity =>
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

        modelBuilder.Entity<GenerationStreamSegment>(entity =>
        {
            entity.HasKey(e => e.SegmentId).HasName("PK_GenerationStreamSegments");

            entity.HasIndex(e => e.CreatedAt, "IX_GenerationStreamSegments_CreatedAt").IsDescending();

            entity.HasIndex(e => new { e.GenerationStreamId, e.SegmentOrdinal }, "IX_GenerationStreamSegments_GenerationStreamId");

            entity.HasIndex(e => e.SegmentKind, "IX_GenerationStreamSegments_SegmentKind").HasFilter("([EmbeddingVector] IS NOT NULL)");

            entity.Property(e => e.ContentType).HasMaxLength(128);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.EmbeddingVector).HasMaxLength(1998);
            entity.Property(e => e.SegmentKind).HasMaxLength(50);

            entity.HasOne(d => d.GenerationStream).WithMany(p => p.GenerationStreamSegment)
                .HasPrincipalKey(p => p.GenerationStreamId)
                .HasForeignKey(d => d.GenerationStreamId)
                .HasConstraintName("FK_GenerationStreamSegments_GenerationStreams");
        });

        modelBuilder.Entity<GenerationStreams>(entity =>
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

        modelBuilder.Entity<InferenceCache>(entity =>
        {
            entity.HasKey(e => e.CacheId);

            entity.Property(e => e.CacheKey).HasMaxLength(64);
            entity.Property(e => e.CreatedUtc).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.InferenceType).HasMaxLength(100);

            entity.HasOne(d => d.Model).WithMany(p => p.InferenceCache)
                .HasForeignKey(d => d.ModelId)
                .HasConstraintName("FK_InferenceCache_Models_ModelId");
        });

        modelBuilder.Entity<InferenceRequest>(entity =>
        {
            entity.HasKey(e => e.InferenceId);

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

            entity.HasOne(d => d.Model).WithMany(p => p.InferenceRequest)
                .HasForeignKey(d => d.ModelId)
                .HasConstraintName("FK_InferenceRequests_Models_ModelId");
        });

        modelBuilder.Entity<InferenceStep>(entity =>
        {
            entity.HasKey(e => e.StepId).HasName("PK_InferenceSteps");

            entity.Property(e => e.IndexUsed).HasMaxLength(200);
            entity.Property(e => e.OperationType).HasMaxLength(50);

            entity.HasOne(d => d.Inference).WithMany(p => p.InferenceStep)
                .HasForeignKey(d => d.InferenceId)
                .HasConstraintName("FK_InferenceSteps_InferenceRequests_InferenceId");

            entity.HasOne(d => d.Model).WithMany(p => p.InferenceStep)
                .HasForeignKey(d => d.ModelId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_InferenceSteps_Models_ModelId");
        });

        modelBuilder.Entity<IngestionJob>(entity =>
        {
            entity.HasKey(e => e.IngestionJobId).HasName("PK_IngestionJobs");

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

            entity.HasOne(d => d.ParentAtom).WithMany(p => p.IngestionJob)
                .HasForeignKey(d => d.ParentAtomId)
                .HasConstraintName("FK_IngestionJobs_ParentAtom");
        });

        modelBuilder.Entity<IngestionJobAtom>(entity =>
        {
            entity.HasKey(e => e.IngestionJobAtomId).HasName("PK_IngestionJobAtoms");

            entity.Property(e => e.Notes).HasMaxLength(1024);

            entity.HasOne(d => d.Atom).WithMany(p => p.IngestionJobAtom)
                .HasForeignKey(d => d.AtomId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_IngestionJobAtoms_Atoms_AtomId");

            entity.HasOne(d => d.IngestionJob).WithMany(p => p.IngestionJobAtom)
                .HasForeignKey(d => d.IngestionJobId)
                .HasConstraintName("FK_IngestionJobAtoms_IngestionJobs_IngestionJobId");
        });

        modelBuilder.Entity<Model>(entity =>
        {
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

            entity.HasOne(d => d.LayerAtom).WithMany(p => p.ModelLayer)
                .HasForeignKey(d => d.LayerAtomId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_ModelLayers_Atoms_LayerAtomId");

            entity.HasOne(d => d.Model).WithMany(p => p.ModelLayer)
                .HasForeignKey(d => d.ModelId)
                .HasConstraintName("FK_ModelLayers_Models_ModelId");
        });

        modelBuilder.Entity<ModelMetadata>(entity =>
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

            entity.HasOne(d => d.Model).WithMany(p => p.ModelVersionHistory)
                .HasForeignKey(d => d.ModelId)
                .HasConstraintName("FK_ModelVersionHistory_Models");

            entity.HasOne(d => d.ParentVersion).WithMany(p => p.InverseParentVersion)
                .HasForeignKey(d => d.ParentVersionId)
                .HasConstraintName("FK_ModelVersionHistory_ParentVersion");
        });

        modelBuilder.Entity<MultiPathReasoning>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__MultiPat__3214EC07E099CA48");

            entity.HasIndex(e => e.CreatedAt, "IX_MultiPathReasoning_CreatedAt").IsDescending();

            entity.HasIndex(e => e.ProblemId, "IX_MultiPathReasoning_ProblemId");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.ReasoningTree).HasColumnType("json");
        });

        modelBuilder.Entity<Neo4jSyncLog>(entity =>
        {
            entity.HasKey(e => e.LogId);

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
            entity.HasKey(e => e.Id).HasName("PK__tmp_ms_x__3214EC072FDB1F48");

            entity.HasIndex(e => e.CreatedAt, "IX_OperationProvenance_CreatedAt").IsDescending();

            entity.HasIndex(e => e.OperationId, "IX_OperationProvenance_OperationId");

            entity.HasIndex(e => e.OperationId, "UQ__tmp_ms_x__A4F5FC45773C82AC").IsUnique();

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
        });

        modelBuilder.Entity<PendingActions>(entity =>
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

        modelBuilder.Entity<ProvenanceAuditResults>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Provenan__3214EC0759C4F0F7");

            entity.HasIndex(e => new { e.AuditPeriodStart, e.AuditPeriodEnd }, "IX_ProvenanceAuditResults_AuditPeriod");

            entity.HasIndex(e => e.AuditedAt, "IX_ProvenanceAuditResults_AuditedAt").IsDescending();

            entity.Property(e => e.Anomalies).HasColumnType("json");
            entity.Property(e => e.AuditedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Scope).HasMaxLength(100);
        });

        modelBuilder.Entity<ProvenanceValidationResults>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Provenan__3214EC0776469B8D");

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

        modelBuilder.Entity<ReasoningChains>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Reasonin__3214EC07C77DF208");

            entity.HasIndex(e => e.CreatedAt, "IX_ReasoningChains_CreatedAt").IsDescending();

            entity.HasIndex(e => e.ProblemId, "IX_ReasoningChains_ProblemId");

            entity.Property(e => e.ChainData).HasColumnType("json");
            entity.Property(e => e.CoherenceMetrics).HasColumnType("json");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.ReasoningType)
                .HasMaxLength(50)
                .HasDefaultValue("chain_of_thought");
        });

        modelBuilder.Entity<SelfConsistencyResults>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__SelfCons__3214EC0772F3DA49");

            entity.HasIndex(e => e.CreatedAt, "IX_SelfConsistencyResults_CreatedAt").IsDescending();

            entity.HasIndex(e => e.ProblemId, "IX_SelfConsistencyResults_ProblemId");

            entity.Property(e => e.ConsensusMetrics).HasColumnType("json");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.SampleData).HasColumnType("json");
        });

        modelBuilder.Entity<SemanticFeatures>(entity =>
        {
            entity.HasKey(e => e.AtomEmbeddingId).HasName("PK__Semantic__AB86F630436C3B97");

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

            entity.HasOne(d => d.AtomEmbedding).WithOne(p => p.SemanticFeatures)
                .HasForeignKey<SemanticFeatures>(d => d.AtomEmbeddingId)
                .HasConstraintName("FK_SemanticFeature_AtomEmbedding");
        });

        modelBuilder.Entity<SessionPaths>(entity =>
        {
            entity.HasKey(e => e.SessionPathId);

            entity.HasIndex(e => e.SessionId, "IX_SessionPaths_SessionId").IsUnique();

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.EndTime).HasComputedColumnSql("([Path].[STPointN]([Path].[STNumPoints]()).M)", false);
            entity.Property(e => e.Path).HasColumnType("geometry");
            entity.Property(e => e.PathLength).HasComputedColumnSql("([Path].[STLength]())", false);
            entity.Property(e => e.StartTime).HasComputedColumnSql("([Path].[STPointN]((1)).M)", false);
        });

        modelBuilder.Entity<SpatialLandmarks>(entity =>
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

        modelBuilder.Entity<StreamFusionResults>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__StreamFu__3214EC074E0FB3EC");

            entity.HasIndex(e => e.CreatedAt, "IX_StreamFusionResults_CreatedAt").IsDescending();

            entity.HasIndex(e => e.FusionType, "IX_StreamFusionResults_FusionType");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.FusionType).HasMaxLength(50);
            entity.Property(e => e.StreamIds).HasColumnType("json");
            entity.Property(e => e.Weights).HasColumnType("json");
        });

        modelBuilder.Entity<StreamOrchestrationResults>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__StreamOr__3214EC07392041D3");

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

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())", "DF_TenantAtoms_CreatedAt");

            entity.HasOne(d => d.Atom).WithMany(p => p.TenantAtom)
                .HasForeignKey(d => d.AtomId)
                .HasConstraintName("FK_TenantAtoms_Atoms");
        });

        modelBuilder.Entity<TenantGuidMapping>(entity =>
        {
            entity.HasKey(e => e.TenantId);

            entity.ToTable(tb => tb.HasComment("Maps Azure Entra External ID tenant GUIDs to internal integer tenant IDs. Replaces unsafe GetHashCode() approach. Each Azure AD tenant GUID gets a stable, unique integer ID for use throughout the system."));

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
            entity.Property(e => e.AtomType).HasMaxLength(128);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.GeometryFootprint).HasColumnType("geometry");
            entity.Property(e => e.Metadata).HasColumnType("json");
            entity.Property(e => e.SpatialSignature).HasColumnType("geometry");

            entity.HasOne(d => d.Atom).WithMany(p => p.TensorAtom)
                .HasForeignKey(d => d.AtomId)
                .HasConstraintName("FK_TensorAtoms_Atoms_AtomId");

            entity.HasOne(d => d.Layer).WithMany(p => p.TensorAtom)
                .HasForeignKey(d => d.LayerId)
                .HasConstraintName("FK_TensorAtoms_ModelLayers_LayerId");

            entity.HasOne(d => d.Model).WithMany(p => p.TensorAtom)
                .HasForeignKey(d => d.ModelId)
                .HasConstraintName("FK_TensorAtoms_Models_ModelId");
        });

        modelBuilder.Entity<TensorAtomCoefficient>(entity =>
        {
            entity.HasKey(e => new { e.TensorAtomId, e.ModelId, e.LayerIdx, e.PositionX, e.PositionY, e.PositionZ }).HasName("PK_TensorAtomCoefficients");

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

            entity.HasIndex(e => e.SpatialKey, "SIX_TensorAtomCoefficients_SpatialKey");

            entity.Property(e => e.SpatialKey)
                .HasComputedColumnSql("([GEOMETRY]::Point([PositionX],[PositionY],(0)))", true)
                .HasColumnType("geometry");
            entity.Property(e => e.TensorRole).HasMaxLength(128);

            entity.HasOne(d => d.Model).WithMany(p => p.TensorAtomCoefficient)
                .HasForeignKey(d => d.ModelId)
                .HasConstraintName("FK_TensorAtomCoefficients_Model");

            entity.HasOne(d => d.TensorAtom).WithMany(p => p.TensorAtomCoefficient)
                .HasForeignKey(d => d.TensorAtomId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TensorAtomCoefficients_Atom");
        });

        modelBuilder.Entity<TestResult>(entity =>
        {
            entity.HasKey(e => e.TestResultId).HasName("PK_TestResults");

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

            entity.Property(e => e.Embedding).HasMaxLength(1998);
            entity.Property(e => e.Token).HasMaxLength(100);
            entity.Property(e => e.TokenType).HasMaxLength(20);

            entity.HasOne(d => d.Model).WithMany(p => p.TokenVocabulary)
                .HasForeignKey(d => d.ModelId)
                .HasConstraintName("FK_TokenVocabulary_Models_ModelId");
        });

        modelBuilder.Entity<TopicKeywords>(entity =>
        {
            entity.HasKey(e => e.KeywordId).HasName("PK__TopicKey__03E8D7CF14EAE54F");

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

        modelBuilder.Entity<TransformerInferenceResults>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Transfor__3214EC07DCB9E181");

            entity.HasIndex(e => e.CreatedAt, "IX_TransformerInferenceResults_CreatedAt").IsDescending();

            entity.HasIndex(e => e.ProblemId, "IX_TransformerInferenceResults_ProblemId");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.LayerResults).HasColumnType("json");

            entity.HasOne(d => d.Model).WithMany(p => p.TransformerInferenceResults)
                .HasForeignKey(d => d.ModelId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TransformerInferenceResult_Model");
        });

        modelBuilder.Entity<VwReconstructModelLayerWeights>(entity =>
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
            entity.HasKey(e => e.SnapshotId).HasName("PK__WeightSn__664F572BBCAA26A2");

            entity.HasIndex(e => e.SnapshotName, "UQ__WeightSn__FAC0EC4A338BFCE3").IsUnique();

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.SnapshotName).HasMaxLength(255);
            entity.Property(e => e.SnapshotTime).HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.Model).WithMany(p => p.WeightSnapshot)
                .HasForeignKey(d => d.ModelId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_WeightSnapshot_Model");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
