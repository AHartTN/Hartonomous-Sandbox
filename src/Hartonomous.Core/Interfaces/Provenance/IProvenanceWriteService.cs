using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Hartonomous.Core.Interfaces.Provenance;

/// <summary>
/// Service for provenance write operations (complements existing read-only IProvenanceQueryService).
/// </summary>
public interface IProvenanceWriteService
{
    /// <summary>
    /// Links parent atoms to child atom in provenance graph.
    /// Calls sp_LinkProvenance stored procedure.
    /// </summary>
    Task LinkProvenanceAsync(
        string parentAtomIds,
        long childAtomId,
        string dependencyType = "DerivedFrom",
        int tenantId = 0,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Queries upstream/downstream atom provenance lineage.
    /// Calls sp_QueryLineage stored procedure.
    /// </summary>
    Task<LineageResult> QueryLineageAsync(
        long atomId,
        int tenantId = 0,
        string direction = "both",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports provenance as JSON/GraphML/CSV with recursive lineage.
    /// Calls sp_ExportProvenance stored procedure.
    /// </summary>
    Task<string> ExportProvenanceAsync(
        long atomId,
        string format = "JSON",
        int tenantId = 0,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates provenance stream with comprehensive checks.
    /// Calls sp_ValidateOperationProvenance stored procedure.
    /// </summary>
    Task<ValidationResult> ValidateProvenanceAsync(
        Guid operationId,
        string? expectedScope = null,
        string? expectedModel = null,
        int minSegments = 1,
        int maxAgeHours = 24,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Audits provenance chains over date range with anomaly detection.
    /// Calls sp_AuditProvenanceChain stored procedure.
    /// </summary>
    Task<AuditResult> AuditProvenanceAsync(
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? scope = null,
        float minValidationScore = 0.8f,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds downstream impacted atoms via recursive graph traversal.
    /// Calls sp_FindImpactedAtoms stored procedure.
    /// </summary>
    Task<IEnumerable<ImpactedAtom>> FindImpactedAtomsAsync(
        long atomId,
        int tenantId = 0,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds related documents via vector similarity, text search, and 1-hop graph neighbors.
    /// Calls sp_FindRelatedDocuments stored procedure.
    /// </summary>
    Task<IEnumerable<RelatedDocument>> FindRelatedDocumentsAsync(
        long atomId,
        int topK = 10,
        int tenantId = 0,
        bool includeSemanticText = true,
        bool includeVectorSimilarity = true,
        bool includeGraphNeighbors = true,
        CancellationToken cancellationToken = default);
}

public record LineageResult(
    long AtomId,
    IEnumerable<LineageNode> Upstream,
    IEnumerable<LineageNode> Downstream);

public record LineageNode(
    long AtomId,
    int Depth,
    string RelationType);

public record ValidationResult(
    bool IsValid,
    int SegmentsValidated,
    IEnumerable<string> Errors);

public record AuditResult(
    int ChainsAudited,
    int AnomaliesDetected,
    IEnumerable<AuditAnomaly> Anomalies);

public record AuditAnomaly(
    Guid OperationId,
    string AnomalyType,
    string Description,
    float Severity);

public record ImpactedAtom(
    long AtomId,
    int Depth,
    string ImpactPath);

public record RelatedDocument(
    long AtomId,
    string RelationType,
    float RelevanceScore);
