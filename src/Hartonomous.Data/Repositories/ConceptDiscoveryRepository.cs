using Hartonomous.Core.Entities;
using Hartonomous.Core.Shared;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using System.Text.Json;

namespace Hartonomous.Data.Repositories;

/// <summary>
/// Repository for concept discovery and binding operations using EF Core
/// Replaces sp_DiscoverAndBindConcepts stored procedure
/// </summary>
public class ConceptDiscoveryRepository : IConceptDiscoveryRepository
{
    private readonly HartonomousDbContext _context;

    public ConceptDiscoveryRepository(HartonomousDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Discovers concepts through clustering analysis
    /// </summary>
    public async Task<ConceptDiscoveryResult> DiscoverConceptsAsync(
        IReadOnlyList<EmbeddingVector> embeddingVectors,
        int minClusterSize = 5,
        CancellationToken cancellationToken = default)
    {
        var discoveryId = Guid.NewGuid();
        var concepts = new List<DiscoveredConcept>();

        if (embeddingVectors.Count < minClusterSize)
        {
            return new ConceptDiscoveryResult
            {
                DiscoveryId = discoveryId,
                VectorsProcessed = embeddingVectors.Count,
                ClustersFound = 0,
                ClusteringQuality = 0.0,
                Timestamp = DateTime.UtcNow
            };
        }

        // Simple spatial clustering based on existing spatial buckets
        // In a real implementation, this would use more sophisticated clustering algorithms
        var spatialClusters = embeddingVectors
            .Where(v => v.SpatialLocation != null)
            .GroupBy(v => new
            {
                BucketX = Math.Floor(v.SpatialLocation!.X / 10), // 10-unit buckets
                BucketY = Math.Floor(v.SpatialLocation!.Y / 10),
                BucketZ = !double.IsNaN(v.SpatialLocation!.Z) ? Math.Floor(v.SpatialLocation!.Z / 10) : 0
            })
            .Where(g => g.Count() >= minClusterSize)
            .ToList();

        foreach (var cluster in spatialClusters)
        {
            var memberVectors = cluster.ToList();
            var centroid = CalculateCentroid(memberVectors);

            // Calculate cluster bounds
            var minX = memberVectors.Min(v => v.SpatialLocation!.X);
            var maxX = memberVectors.Max(v => v.SpatialLocation!.X);
            var minY = memberVectors.Min(v => v.SpatialLocation!.Y);
            var maxY = memberVectors.Max(v => v.SpatialLocation!.Y);

            var bounds = new Polygon(new LinearRing(new[]
            {
                new Coordinate(minX, minY),
                new Coordinate(maxX, minY),
                new Coordinate(maxX, maxY),
                new Coordinate(minX, maxY),
                new Coordinate(minX, minY)
            }));

            var concept = new DiscoveredConcept
            {
                ConceptId = Guid.NewGuid(),
                ConceptName = $"SpatialCluster_{cluster.Key.BucketX}_{cluster.Key.BucketY}",
                Description = $"Spatial cluster containing {memberVectors.Count} vectors",
                Centroid = centroid,
                MemberVectors = memberVectors,
                ConfidenceScore = Math.Min(1.0, memberVectors.Count / 20.0), // Simple confidence based on size
                ClusterBounds = bounds
            };

            concepts.Add(concept);
        }

        // Calculate clustering quality (simplified)
        var totalVectors = embeddingVectors.Count;
        var clusteredVectors = concepts.Sum(c => c.MemberVectors.Count);
        var clusteringQuality = totalVectors > 0 ? (double)clusteredVectors / totalVectors : 0.0;

        return new ConceptDiscoveryResult
        {
            DiscoveryId = discoveryId,
            Concepts = concepts,
            VectorsProcessed = totalVectors,
            ClustersFound = concepts.Count,
            ClusteringQuality = clusteringQuality,
            Timestamp = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Binds discovered concepts to existing knowledge graph
    /// </summary>
    public async Task<ConceptBindingResult> BindConceptsAsync(
        IReadOnlyList<DiscoveredConcept> concepts,
        CancellationToken cancellationToken = default)
    {
        var bindingId = Guid.NewGuid();
        var boundConcepts = new List<BoundConcept>();
        var failedBindings = new List<FailedBinding>();
        var relationshipsCreated = 0;

        foreach (var discoveredConcept in concepts)
        {
            try
            {
                // Check if similar concept already exists (simplified - just check name for now)
                var existingConcept = await _context.Concepts
                    .FirstOrDefaultAsync(c =>
                        c.ConceptName == discoveredConcept.ConceptName,
                        cancellationToken);

                Concept conceptEntity;

                if (existingConcept != null)
                {
                    // Update existing concept
                    existingConcept.Description = discoveredConcept.Description;
                    existingConcept.MemberCount = discoveredConcept.MemberVectors.Count;
                    existingConcept.CoherenceScore = discoveredConcept.ConfidenceScore;
                    existingConcept.LastUpdatedAt = DateTime.UtcNow;
                    conceptEntity = existingConcept;
                }
                else
                {
                    // Create new concept
                    conceptEntity = new Concept
                    {
                        ConceptName = discoveredConcept.ConceptName,
                        Description = discoveredConcept.Description,
                        CentroidVector = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(discoveredConcept.Centroid),
                        VectorDimension = discoveredConcept.Centroid.Length,
                        MemberCount = discoveredConcept.MemberVectors.Count,
                        CoherenceScore = discoveredConcept.ConfidenceScore,
                        DiscoveryMethod = "spatial_clustering",
                        ModelId = 1, // Default model for now
                        DiscoveredAt = DateTime.UtcNow,
                        IsActive = true
                    };
                    _context.Concepts.Add(conceptEntity);
                }

                // Create relationships to member atoms (simplified - not using SQL Graph for now)
                var relationships = new List<string>();
                foreach (var vector in discoveredConcept.MemberVectors)
                {
                    if (vector.AtomId.HasValue)
                    {
                        // For now, just record the relationship in the list
                        // SQL Graph relationships would be created here in a full implementation
                        relationships.Add($"Atom {vector.AtomId} -> Concept {conceptEntity.ConceptName}");
                        relationshipsCreated++;
                    }
                }

                boundConcepts.Add(new BoundConcept
                {
                    Concept = discoveredConcept,
                    ConceptEntityId = conceptEntity.ConceptId,
                    Relationships = relationships
                });
            }
            catch (Exception ex)
            {
                failedBindings.Add(new FailedBinding
                {
                    Concept = discoveredConcept,
                    FailureReason = ex.Message
                });
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        return new ConceptBindingResult
        {
            BindingId = bindingId,
            BoundConcepts = boundConcepts,
            FailedBindings = failedBindings,
            RelationshipsCreated = relationshipsCreated,
            Timestamp = DateTime.UtcNow
        };
    }

    private double[] CalculateCentroid(IReadOnlyList<EmbeddingVector> vectors)
    {
        if (!vectors.Any()) return Array.Empty<double>();

        var dimension = vectors.First().Vector.Length;
        var centroid = new double[dimension];

        foreach (var vector in vectors)
        {
            for (int i = 0; i < dimension; i++)
            {
                centroid[i] += vector.Vector[i];
            }
        }

        for (int i = 0; i < dimension; i++)
        {
            centroid[i] /= vectors.Count;
        }

        return centroid;
    }
}