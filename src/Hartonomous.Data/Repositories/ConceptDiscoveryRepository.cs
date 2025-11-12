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
    /// Discovers concepts through clustering analysis using SQL CLR DBSCAN
    /// Delegates to dbo.fn_DiscoverConcepts (DBSCAN clustering with VECTOR_DISTANCE coherence)
    /// </summary>
    public async Task<ConceptDiscoveryResult> DiscoverConceptsAsync(
        IReadOnlyList<EmbeddingVector> embeddingVectors,
        int minClusterSize = 5,
        CancellationToken cancellationToken = default)
    {
        var discoveryId = Guid.NewGuid();
        
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

        // Call SQL CLR function for DBSCAN clustering
        // fn_DiscoverConcepts performs density-based clustering on AtomEmbeddings with spatial bucketing
        var clrSql = @"
            SELECT 
                ConceptId,
                Centroid,
                AtomCount,
                Coherence,
                SpatialBucket
            FROM dbo.fn_DiscoverConcepts(
                @MinClusterSize,
                @CoherenceThreshold,
                @MaxConcepts,
                @TenantId
            )
            ORDER BY Coherence DESC";

        var concepts = new List<DiscoveredConcept>();
        
        using var connection = _context.Database.GetDbConnection();
        await connection.OpenAsync(cancellationToken);
        
        using var command = connection.CreateCommand();
        command.CommandText = clrSql;
        
        var minSizeParam = command.CreateParameter();
        minSizeParam.ParameterName = "@MinClusterSize";
        minSizeParam.Value = minClusterSize;
        command.Parameters.Add(minSizeParam);
        
        var coherenceParam = command.CreateParameter();
        coherenceParam.ParameterName = "@CoherenceThreshold";
        coherenceParam.Value = 0.7; // Minimum average cosine similarity to centroid
        command.Parameters.Add(coherenceParam);
        
        var maxConceptsParam = command.CreateParameter();
        maxConceptsParam.ParameterName = "@MaxConcepts";
        maxConceptsParam.Value = 100;
        command.Parameters.Add(maxConceptsParam);
        
        var tenantParam = command.CreateParameter();
        tenantParam.ParameterName = "@TenantId";
        tenantParam.Value = 0; // TODO: Get from context
        command.Parameters.Add(tenantParam);
        
        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        
        while (await reader.ReadAsync(cancellationToken))
        {
            var conceptId = reader.GetGuid(0);
            var centroidBytes = (byte[])reader.GetValue(1);
            var atomCount = reader.GetInt32(2);
            var coherence = reader.GetDouble(3);
            var spatialBucket = reader.GetInt32(4);
            
            // Convert VARBINARY centroid to double array
            var centroid = ConvertVarbinaryToDoubleArray(centroidBytes);
            
            var concept = new DiscoveredConcept
            {
                ConceptId = conceptId,
                ConceptName = $"Concept_{spatialBucket}",
                Description = $"DBSCAN cluster with {atomCount} atoms, coherence {coherence:F3}",
                Centroid = centroid,
                MemberVectors = new List<EmbeddingVector>(), // Populated by separate query if needed
                ModelId = 0, // Multi-model by default
                ConfidenceScore = coherence, // Use CLR-computed coherence (avg cosine similarity)
                ClusterBounds = null // Can compute from spatial bucket if needed
            };
            
            concepts.Add(concept);
        }

        var clusteringQuality = concepts.Count > 0 ? concepts.Average(c => c.ConfidenceScore) : 0.0;

        return new ConceptDiscoveryResult
        {
            DiscoveryId = discoveryId,
            Concepts = concepts,
            VectorsProcessed = embeddingVectors.Count, // Approximation
            ClustersFound = concepts.Count,
            ClusteringQuality = clusteringQuality,
            Timestamp = DateTime.UtcNow
        };
    }
    
    private double[] ConvertVarbinaryToDoubleArray(byte[] varbinary)
    {
        if (varbinary == null || varbinary.Length == 0)
            return Array.Empty<double>();
        
        // VECTOR(1998) is stored as float32 array
        var floatCount = varbinary.Length / sizeof(float);
        var result = new double[floatCount];
        
        for (int i = 0; i < floatCount; i++)
        {
            var floatValue = BitConverter.ToSingle(varbinary, i * sizeof(float));
            result[i] = floatValue;
        }
        
        return result;
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
                // Check if similar concept already exists using SQL Server VECTOR_DISTANCE
                // Spatial index enables O(log n) candidate filtering, exact k-NN refinement
                var centroidBytes = ConvertDoubleArrayToVarbinary(discoveredConcept.Centroid);
                
                var similarityThreshold = 0.85;
                var similaritySql = @"
                    SELECT TOP 1
                        c.ConceptId,
                        c.ConceptName,
                        c.CentroidVector,
                        c.VectorDimension,
                        c.MemberCount,
                        c.CoherenceScore,
                        c.DiscoveryMethod,
                        c.ModelId,
                        1.0 - VECTOR_DISTANCE('cosine', c.CentroidVector, @QueryCentroid) AS Similarity
                    FROM dbo.Concepts c
                    WHERE c.IsActive = 1
                      AND c.VectorDimension = @Dimension
                      AND (1.0 - VECTOR_DISTANCE('cosine', c.CentroidVector, @QueryCentroid)) > @Threshold
                    ORDER BY Similarity DESC";

                Concept? existingConcept = null;
                double bestSimilarity = 0.0;
                
                using var connection = _context.Database.GetDbConnection();
                if (connection.State != System.Data.ConnectionState.Open)
                    await connection.OpenAsync(cancellationToken);
                
                using var command = connection.CreateCommand();
                command.CommandText = similaritySql;
                
                var centroidParam = command.CreateParameter();
                centroidParam.ParameterName = "@QueryCentroid";
                centroidParam.Value = centroidBytes;
                command.Parameters.Add(centroidParam);
                
                var dimParam = command.CreateParameter();
                dimParam.ParameterName = "@Dimension";
                dimParam.Value = discoveredConcept.Centroid.Length;
                command.Parameters.Add(dimParam);
                
                var thresholdParam = command.CreateParameter();
                thresholdParam.ParameterName = "@Threshold";
                thresholdParam.Value = similarityThreshold;
                command.Parameters.Add(thresholdParam);
                
                using var reader = await command.ExecuteReaderAsync(cancellationToken);
                
                if (await reader.ReadAsync(cancellationToken))
                {
                    var conceptId = reader.GetInt64(0);
                    bestSimilarity = reader.GetDouble(8);
                    
                    // Load full entity for update
                    existingConcept = await _context.Concepts
                        .FirstOrDefaultAsync(c => c.ConceptId == conceptId, cancellationToken);
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
                        ModelId = discoveredConcept.ModelId,
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
                        // Create graph edge relationship via graph.AtomGraphEdges
                        // Architecture: graph.AtomGraphNodes (AS NODE), graph.AtomGraphEdges (AS EDGE)
                        // EdgeType='BindsToConcept' indicates unsupervised concept binding
                        try
                        {
                            // Step 1: Ensure AtomGraphNode exists for this Atom
                            await _context.Database.ExecuteSqlRawAsync(
                                @"IF NOT EXISTS (SELECT 1 FROM graph.AtomGraphNodes WHERE AtomId = @atomId)
                                  BEGIN
                                      INSERT INTO graph.AtomGraphNodes (AtomId, NodeType, Metadata, CreatedUtc)
                                      VALUES (@atomId, 'Atom', NULL, SYSUTCDATETIME())
                                  END",
                                new[]
                                {
                                    new Microsoft.Data.SqlClient.SqlParameter("@atomId", vector.AtomId.Value)
                                },
                                cancellationToken);
                            
                            // Step 2: Ensure AtomGraphNode exists for this Concept
                            await _context.Database.ExecuteSqlRawAsync(
                                @"IF NOT EXISTS (SELECT 1 FROM graph.AtomGraphNodes WHERE AtomId = @conceptId AND NodeType = 'Concept')
                                  BEGIN
                                      INSERT INTO graph.AtomGraphNodes (AtomId, NodeType, Metadata, CreatedUtc)
                                      VALUES (@conceptId, 'Concept', @metadata, SYSUTCDATETIME())
                                  END",
                                new[]
                                {
                                    new Microsoft.Data.SqlClient.SqlParameter("@conceptId", conceptEntity.ConceptId),
                                    new Microsoft.Data.SqlClient.SqlParameter("@metadata", 
                                        System.Text.Json.JsonSerializer.Serialize(new { ConceptName = conceptEntity.ConceptName }))
                                },
                                cancellationToken);
                            
                            // Step 3: Create edge relationship using MATCH syntax
                            await _context.Database.ExecuteSqlRawAsync(
                                @"INSERT INTO graph.AtomGraphEdges (EdgeType, Weight, Metadata, CreatedUtc, $from_id, $to_id)
                                  SELECT 'BindsToConcept', @weight, @metadata, SYSUTCDATETIME(), src.$node_id, dest.$node_id
                                  FROM graph.AtomGraphNodes AS src, graph.AtomGraphNodes AS dest
                                  WHERE src.AtomId = @atomId AND src.NodeType = 'Atom'
                                    AND dest.AtomId = @conceptId AND dest.NodeType = 'Concept'
                                    AND NOT EXISTS (
                                        SELECT 1 FROM graph.AtomGraphEdges e, graph.AtomGraphNodes s, graph.AtomGraphNodes d
                                        WHERE MATCH(s-(e)->d)
                                          AND s.AtomId = @atomId AND d.AtomId = @conceptId
                                          AND e.EdgeType = 'BindsToConcept'
                                    )",
                                new[]
                                {
                                    new Microsoft.Data.SqlClient.SqlParameter("@atomId", vector.AtomId.Value),
                                    new Microsoft.Data.SqlClient.SqlParameter("@conceptId", conceptEntity.ConceptId),
                                    new Microsoft.Data.SqlClient.SqlParameter("@weight", (float)discoveredConcept.ConfidenceScore),
                                    new Microsoft.Data.SqlClient.SqlParameter("@metadata", 
                                        System.Text.Json.JsonSerializer.Serialize(new { 
                                            confidence = discoveredConcept.ConfidenceScore,
                                            method = "dbscan_clustering",
                                            timestamp = DateTime.UtcNow
                                        }))
                                },
                                cancellationToken);

                            relationships.Add($"Atom {vector.AtomId} -[BindsToConcept]-> Concept {conceptEntity.ConceptName}");
                            relationshipsCreated++;
                        }
                        catch (Exception ex)
                        {
                            // Log error but continue processing other relationships
                            relationships.Add($"Atom {vector.AtomId} -> Concept {conceptEntity.ConceptName} (failed: {ex.Message})");
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
    
    private byte[] ConvertDoubleArrayToVarbinary(double[] vector)
    {
        var bytes = new byte[vector.Length * sizeof(float)];
        for (int i = 0; i < vector.Length; i++)
        {
            var floatValue = (float)vector[i];
            var floatBytes = BitConverter.GetBytes(floatValue);
            Array.Copy(floatBytes, 0, bytes, i * sizeof(float), sizeof(float));
        }
        return bytes;
    }
}