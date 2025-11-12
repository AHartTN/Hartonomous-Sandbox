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
                // Check if similar concept already exists using vector similarity
                // Query existing concepts and compare centroid embeddings
                var existingConcepts = await _context.Concepts
                    .Where(c => c.IsActive && c.VectorDimension == discoveredConcept.Centroid.Length)
                    .Take(100) // Limit for performance
                    .ToListAsync(cancellationToken);

                Concept? existingConcept = null;
                double bestSimilarity = 0.0;

                foreach (var candidate in existingConcepts)
                {
                    if (candidate.CentroidVector == null) continue;

                    var candidateVector = System.Text.Json.JsonSerializer.Deserialize<float[]>(candidate.CentroidVector);
                    if (candidateVector == null || candidateVector.Length != discoveredConcept.Centroid.Length) continue;

                    // Compute cosine similarity
                    double dotProduct = 0.0;
                    double norm1 = 0.0;
                    double norm2 = 0.0;
                    for (int i = 0; i < candidateVector.Length; i++)
                    {
                        dotProduct += candidateVector[i] * discoveredConcept.Centroid[i];
                        norm1 += candidateVector[i] * candidateVector[i];
                        norm2 += discoveredConcept.Centroid[i] * discoveredConcept.Centroid[i];
                    }
                    var similarity = dotProduct / (Math.Sqrt(norm1) * Math.Sqrt(norm2));

                    // Concepts are considered similar if cosine similarity > 0.85
                    if (similarity > 0.85 && similarity > bestSimilarity)
                    {
                        bestSimilarity = similarity;
                        existingConcept = candidate;
                    }
                }

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

                await _context.SaveChangesAsync(cancellationToken);

                // Create SQL Graph relationships using MATCH syntax
                // Assumes Atoms and Concepts are NODE tables with IS_MEMBER_OF EDGE table
                var relationships = new List<string>();
                foreach (var vector in discoveredConcept.MemberVectors)
                {
                    if (vector.AtomId.HasValue)
                    {
                        // Create graph edge relationship: Atom -[IS_MEMBER_OF]-> Concept
                        // NOTE: This requires Atoms, Concepts to be created as NODE tables and IS_MEMBER_OF as EDGE table
                        // For now, using raw SQL since EF Core doesn't have full SQL Graph support
                        try
                        {
                            await _context.Database.ExecuteSqlRawAsync(
                                @"INSERT INTO dbo.IS_MEMBER_OF ($from_id, $to_id, MembershipScore, CreatedAt)
                                  SELECT a.$node_id, c.$node_id, @score, @timestamp
                                  FROM dbo.Atoms AS a, dbo.Concepts AS c
                                  WHERE a.AtomId = @atomId AND c.ConceptId = @conceptId",
                                new[]
                                {
                                    new Microsoft.Data.SqlClient.SqlParameter("@atomId", vector.AtomId.Value),
                                    new Microsoft.Data.SqlClient.SqlParameter("@conceptId", conceptEntity.ConceptId),
                                    new Microsoft.Data.SqlClient.SqlParameter("@score", discoveredConcept.ConfidenceScore),
                                    new Microsoft.Data.SqlClient.SqlParameter("@timestamp", DateTime.UtcNow)
                                },
                                cancellationToken);

                            relationships.Add($"Atom {vector.AtomId} -[IS_MEMBER_OF]-> Concept {conceptEntity.ConceptName}");
                            relationshipsCreated++;
                        }
                        catch (Exception)
                        {
                            // If SQL Graph tables don't exist yet, fall back to logging the relationship
                            // SQL Graph configuration requires Atoms, Concepts as NODE tables and IS_MEMBER_OF as EDGE table
                            relationships.Add($"Atom {vector.AtomId} -> Concept {conceptEntity.ConceptName} (fallback)");
                        }
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