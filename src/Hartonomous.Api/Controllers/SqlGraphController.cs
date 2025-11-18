using Hartonomous.Api.DTOs.Graph;
using Hartonomous.Core.Interfaces;
using Hartonomous.Core.Performance;
using Hartonomous.Shared.Contracts.Errors;
using Hartonomous.Shared.Contracts.Responses;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;

namespace Hartonomous.Api.Controllers;

/// <summary>
/// Controller for SQL Server graph operations.
/// Handles node and edge management in SQL Server graph tables.
/// </summary>
[ApiController]
[Route("api/v1/graph")]
public class SqlGraphController : ApiControllerBase
{
    private readonly ILogger<SqlGraphController> _logger;
    private readonly string _connectionString;
    private readonly PerformanceMonitor _performanceMonitor;

    public SqlGraphController(
        ILogger<SqlGraphController> logger,
        IConfiguration configuration,
        PerformanceMonitor performanceMonitor)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new ArgumentNullException("DefaultConnection connection string not found");
        _performanceMonitor = performanceMonitor ?? throw new ArgumentNullException(nameof(performanceMonitor));
    }

    [HttpPost("sql/nodes")]
    [ProducesResponseType(typeof(ApiResponse<SqlGraphCreateNodeResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse<SqlGraphCreateNodeResponse>), 400)]
    public async Task<IActionResult> CreateSqlGraphNode(
        [FromBody] SqlGraphCreateNodeRequest request,
        CancellationToken cancellationToken)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.NodeType))
        {
            return BadRequest(Failure<SqlGraphCreateNodeResponse>(new[]
            {
                ErrorDetailFactory.Create(ErrorCodes.Validation.InvalidRequest, "NodeType is required", "NodeType cannot be empty")
            }));
        }

        try
        {
            _logger.LogInformation("Creating SQL graph node of type {NodeType}", request.NodeType);

            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            // Create dynamic table name based on node type
            var tableName = $"dbo.{request.NodeType}";
            
            if (request.Metadata == null || !request.Metadata.Any())
            {
                // Simple insert without metadata
                var simpleQuery = $@"
                    INSERT INTO {tableName} (atom_id)
                    OUTPUT INSERTED.$node_id AS NodeId
                    VALUES (@atomId)";

                await using var simpleCommand = new SqlCommand(simpleQuery, connection);
                simpleCommand.Parameters.AddWithValue("@atomId", request.AtomId);

                var simpleNodeId = (long)await simpleCommand.ExecuteScalarAsync(cancellationToken);

                _logger.LogInformation("Created SQL graph node {NodeId} of type {NodeType}", simpleNodeId, request.NodeType);

                return Ok(Success(new SqlGraphCreateNodeResponse
                {
                    NodeId = simpleNodeId,
                    AtomId = request.AtomId,
                    NodeType = request.NodeType,
                    Success = true
                }));
            }

            var columnNames = string.Join(", ", request.Metadata.Keys);
            var parameterNames = string.Join(", ", request.Metadata.Keys.Select(k => $"@{k}"));

            var complexQuery = $@"
                INSERT INTO {tableName} (atom_id, {columnNames})
                OUTPUT INSERTED.$node_id AS NodeId
                VALUES (@atomId, {parameterNames})";

            await using var complexCommand = new SqlCommand(complexQuery, connection);
            complexCommand.Parameters.AddWithValue("@atomId", request.AtomId);

            // Add parameters
            foreach (var property in request.Metadata)
            {
                complexCommand.Parameters.AddWithValue($"@{property.Key}", property.Value ?? DBNull.Value);
            }

            var complexNodeId = (long)await complexCommand.ExecuteScalarAsync(cancellationToken);

            _logger.LogInformation("Created SQL graph node {NodeId} of type {NodeType}", complexNodeId, request.NodeType);

            return Ok(Success(new SqlGraphCreateNodeResponse
            {
                NodeId = complexNodeId,
                AtomId = request.AtomId,
                NodeType = request.NodeType,
                Success = true
            }));
        }
        catch (SqlException ex) when (ex.Number == 208) // Invalid object name
        {
            _logger.LogWarning(ex, "SQL graph table {NodeType} does not exist", request.NodeType);
            var error = ErrorDetailFactory.Create(ErrorCodes.NotFound.Resource,
                $"Graph table {request.NodeType} does not exist", "Ensure the node type table exists in the database");
            return NotFound(Failure<SqlGraphCreateNodeResponse>(new[] { error }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create SQL graph node");
            var error = ErrorDetailFactory.Create(ErrorCodes.Infrastructure.DatabaseUnavailable, "Failed to create SQL graph node", ex.Message);
            return StatusCode(500, Failure<SqlGraphCreateNodeResponse>(new[] { error }));
        }
    }

    [HttpPost("sql/edges")]
    [ProducesResponseType(typeof(ApiResponse<SqlGraphCreateEdgeResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse<SqlGraphCreateEdgeResponse>), 400)]
    public async Task<IActionResult> CreateSqlGraphEdge(
        [FromBody] SqlGraphCreateEdgeRequest request,
        CancellationToken cancellationToken)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.EdgeType))
        {
            return BadRequest(Failure<SqlGraphCreateEdgeResponse>(new[]
            {
                ErrorDetailFactory.Create(ErrorCodes.Validation.InvalidRequest, "EdgeType is required", "EdgeType cannot be empty")
            }));
        }

        if (request.FromNodeId == 0 || request.ToNodeId == 0)
        {
            return BadRequest(Failure<SqlGraphCreateEdgeResponse>(new[]
            {
                ErrorDetailFactory.Create(ErrorCodes.Validation.InvalidRequest,
                    "FromNodeId and ToNodeId are required", "Both node IDs must be provided and non-zero")
            }));
        }

        try
        {
            _logger.LogInformation("Creating SQL graph edge {EdgeType} from {FromNodeId} to {ToNodeId}",
                request.EdgeType, request.FromNodeId, request.ToNodeId);

            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            // Create dynamic table name based on edge type
            var tableName = $"dbo.{request.EdgeType}";
            var columnNames = new List<string> { "from_node_id", "to_node_id" };
            var parameterNames = new List<string> { "@from_node_id", "@to_node_id" };

            if (request.Metadata != null && request.Metadata.Any())
            {
                columnNames.AddRange(request.Metadata.Keys);
                parameterNames.AddRange(request.Metadata.Keys.Select(k => $"@{k}"));
            }

            var columns = string.Join(", ", columnNames);
            var parameters = string.Join(", ", parameterNames);

            var createQuery = $@"
                INSERT INTO {tableName} ({columns})
                OUTPUT INSERTED.$edge_id AS EdgeId
                VALUES ({parameters})";

            await using var command = new SqlCommand(createQuery, connection);

            // Add parameters
            command.Parameters.AddWithValue("@from_node_id", request.FromNodeId);
            command.Parameters.AddWithValue("@to_node_id", request.ToNodeId);

            if (request.Metadata != null)
            {
                foreach (var property in request.Metadata)
                {
                    command.Parameters.AddWithValue($"@{property.Key}", property.Value ?? DBNull.Value);
                }
            }

            var edgeId = (long)await command.ExecuteScalarAsync(cancellationToken);

            _logger.LogInformation("Created SQL graph edge {EdgeId} of type {EdgeType}", edgeId, request.EdgeType);

            return Ok(Success(new SqlGraphCreateEdgeResponse
            {
                EdgeId = edgeId,
                EdgeType = request.EdgeType,
                FromNodeId = request.FromNodeId,
                ToNodeId = request.ToNodeId,
                Success = true
            }));
        }
        catch (SqlException ex) when (ex.Number == 208) // Invalid object name
        {
            _logger.LogWarning(ex, "SQL graph table {EdgeType} does not exist", request.EdgeType);
            var error = ErrorDetailFactory.Create(ErrorCodes.NotFound.Resource,
                $"Graph table {request.EdgeType} does not exist", "Ensure the edge type table exists in the database");
            return NotFound(Failure<SqlGraphCreateEdgeResponse>(new[] { error }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create SQL graph edge");
            var error = ErrorDetailFactory.Create(ErrorCodes.Infrastructure.DatabaseUnavailable, "Failed to create SQL graph edge", ex.Message);
            return StatusCode(500, Failure<SqlGraphCreateEdgeResponse>(new[] { error }));
        }
    }

    [HttpPost("sql/traverse")]
    [ProducesResponseType(typeof(ApiResponse<SqlGraphTraverseResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse<SqlGraphTraverseResponse>), 400)]
    public async Task<IActionResult> TraverseSqlGraph(
        [FromBody] SqlGraphTraverseRequest request,
        CancellationToken cancellationToken)
    {
        if (request == null || request.StartAtomId == 0)
        {
            return BadRequest(Failure<SqlGraphTraverseResponse>(new[]
            {
                ErrorDetailFactory.Create(ErrorCodes.Validation.InvalidRequest, "StartAtomId is required", "StartAtomId must be provided and non-zero")
            }));
        }

        try
        {
            _logger.LogInformation("Traversing SQL graph from atom {StartAtomId} with max depth {MaxDepth}",
                request.StartAtomId, request.MaxDepth);

            // Monitor the database operation with enterprise-grade performance tracking
            var operationResult = await _performanceMonitor.MonitorAsync(
                "sql_graph_traverse",
                async () =>
                {
                    await using var connection = new SqlConnection(_connectionString);
                    await connection.OpenAsync(cancellationToken);

                    // Build dynamic traversal query
                    var edgeFilter = request.EdgeTypeFilter ?? "SimilarTo";
                    var direction = request.Direction.ToLower();

                    var traversalQuery = $@"
                        WITH RecursiveTraversal AS (
                            SELECT
                                n.$node_id AS node_id,
                                n.atom_id,
                                n.modality,
                                n.canonical_text,
                                CAST(n.atom_id AS NVARCHAR(MAX)) AS path,
                                0 AS depth,
                                CAST(NULL AS BIGINT) AS from_edge_id,
                                CAST(NULL AS NVARCHAR(100)) AS edge_type
                            FROM dbo.Atom n
                            WHERE n.atom_id = @startAtomId

                            UNION ALL

                            SELECT
                                next_n.$node_id,
                                next_n.atom_id,
                                next_n.modality,
                                next_n.canonical_text,
                                CAST(rt.path + '->' + CAST(next_n.atom_id AS NVARCHAR(MAX)) AS NVARCHAR(MAX)),
                                rt.depth + 1,
                                e.$edge_id,
                                OBJECT_NAME(e.$edge_id) AS edge_type
                            FROM RecursiveTraversal rt
                            INNER JOIN dbo.SimilarTo e ON rt.node_id = e.$from_id
                            INNER JOIN dbo.Atom next_n ON e.$to_id = next_n.$node_id
                            WHERE rt.depth < @maxDepth
                              AND (@edgeTypeFilter IS NULL OR OBJECT_NAME(e.$edge_id) = @edgeTypeFilter)
                              AND CHARINDEX(CAST(next_n.atom_id AS NVARCHAR(MAX)), rt.path) = 0
                        )
                        SELECT DISTINCT
                            node_id,
                            atom_id,
                            modality,
                            canonical_text,
                            path,
                            depth,
                            from_edge_id,
                            edge_type
                        FROM RecursiveTraversal
                        WHERE depth > 0
                        ORDER BY depth, node_id";

                    await using var command = new SqlCommand(traversalQuery, connection);
                    command.Parameters.AddWithValue("@startAtomId", request.StartAtomId);
                    command.Parameters.AddWithValue("@maxDepth", request.MaxDepth);
                    command.Parameters.AddWithValue("@edgeTypeFilter", request.EdgeTypeFilter ?? (object)DBNull.Value);

                    var paths = new List<SqlGraphPathEntry>();
                    var currentPath = new SqlGraphPathEntry
                    {
                        NodeIds = new List<long>(),
                        AtomIds = new List<long>(),
                        EdgeTypes = new List<string>(),
                        PathLength = 0,
                        TotalWeight = 0
                    };

                    await using var reader = await command.ExecuteReaderAsync(cancellationToken);

                    while (await reader.ReadAsync(cancellationToken))
                    {
                        var nodeId = reader.GetInt64(0);
                        var atomId = reader.GetInt64(1);
                        var modality = reader.GetString(2);
                        var canonicalText = reader.IsDBNull(3) ? null : reader.GetString(3);
                        var pathStr = reader.GetString(4);
                        var depth = reader.GetInt32(5);
                        var fromEdgeId = reader.IsDBNull(6) ? (long?)null : reader.GetInt64(6);
                        var edgeType = reader.IsDBNull(7) ? null : reader.GetString(7);

                        // Parse path to get atom sequence
                        var atomIds = pathStr.Split("->").Select(id => long.Parse(id.Trim())).ToList();

                        if (depth == 1)
                        {
                            // Start new path
                            if (currentPath.AtomIds.Any())
                            {
                                paths.Add(currentPath);
                            }

                            currentPath = new SqlGraphPathEntry
                            {
                                NodeIds = new List<long>(),
                                AtomIds = new List<long>(),
                                EdgeTypes = new List<string>(),
                                PathLength = depth,
                                TotalWeight = 0
                            };
                        }

                        // Add node to current path
                        currentPath.NodeIds.Add(nodeId);
                        currentPath.AtomIds.Add(atomId);

                        // Add edge if exists
                        if (fromEdgeId.HasValue && edgeType != null)
                        {
                            currentPath.EdgeTypes.Add(edgeType);
                        }
                    }

                    // Add final path
                    if (currentPath.AtomIds.Any())
                    {
                        paths.Add(currentPath);
                    }

                    return new SqlGraphTraverseResponse
                    {
                        StartAtomId = request.StartAtomId,
                        EndAtomId = request.EndAtomId,
                        Paths = paths,
                        TotalPathsFound = paths.Count,
                        ExecutionTimeMs = 0 // Will be set by performance monitor
                    };
                },
                new KeyValuePair<string, object?>("start_atom_id", request.StartAtomId),
                new KeyValuePair<string, object?>("max_depth", request.MaxDepth),
                new KeyValuePair<string, object?>("edge_type_filter", request.EdgeTypeFilter));

            _logger.LogInformation("Found {PathCount} paths in SQL graph traversal", operationResult.Result.TotalPathsFound);

            // Update the execution time from the performance monitor
            operationResult.Result.ExecutionTimeMs = (int)operationResult.DurationMs;

            return Ok(Success(operationResult.Result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SQL graph traversal failed");
            var error = ErrorDetailFactory.Create(ErrorCodes.Infrastructure.DatabaseUnavailable, "SQL graph traversal failed", ex.Message);
            return StatusCode(500, Failure<SqlGraphTraverseResponse>(new[] { error }));
        }
    }

    [HttpPost("sql/shortest-path")]
    [ProducesResponseType(typeof(ApiResponse<SqlGraphShortestPathResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse<SqlGraphShortestPathResponse>), 400)]
    public async Task<IActionResult> FindShortestPath(
        [FromBody] SqlGraphShortestPathRequest request,
        CancellationToken cancellationToken)
    {
        if (request == null || request.StartAtomId == 0 || request.EndAtomId == 0)
        {
            return BadRequest(Failure<SqlGraphShortestPathResponse>(new[]
            {
                ErrorDetailFactory.Create(ErrorCodes.Validation.InvalidRequest,
                    "StartAtomId and EndAtomId are required", "Both atom IDs must be provided and non-zero")
            }));
        }

        try
        {
            _logger.LogInformation("Finding shortest path from {StartAtomId} to {EndAtomId}",
                request.StartAtomId, request.EndAtomId);

            // Monitor the database operation with enterprise-grade performance tracking
            var operationResult = await _performanceMonitor.MonitorAsync(
                "sql_graph_shortest_path",
                async () =>
                {
                    await using var connection = new SqlConnection(_connectionString);
                    await connection.OpenAsync(cancellationToken);

                    // Use SQL Server's shortest_path function if available, otherwise implement BFS
                    var shortestPathQuery = @"
                        DECLARE @startAtom BIGINT = @startAtomId;
                        DECLARE @endAtom BIGINT = @endAtomId;

                        WITH ShortestPathCTE AS (
                            SELECT
                                n.$node_id AS node_id,
                                n.atom_id,
                                n.modality,
                                n.canonical_text,
                                CAST(n.atom_id AS NVARCHAR(MAX)) AS path,
                                0 AS cost,
                                0 AS depth
                            FROM dbo.Atom n
                            WHERE n.atom_id = @startAtom

                            UNION ALL

                            SELECT
                                next_n.$node_id,
                                next_n.atom_id,
                                next_n.modality,
                                next_n.canonical_text,
                                CAST(sp.path + '->' + CAST(next_n.atom_id AS NVARCHAR(MAX)) AS NVARCHAR(MAX)),
                                sp.cost + COALESCE(e.weight, 1.0),
                                sp.depth + 1
                            FROM ShortestPathCTE sp
                            INNER JOIN dbo.SimilarTo e ON sp.node_id = e.$from_id
                            INNER JOIN dbo.Atom next_n ON e.$to_id = next_n.$node_id
                            WHERE sp.atom_id <> @endAtom
                              AND CHARINDEX(CAST(next_n.atom_id AS NVARCHAR(MAX)), sp.path) = 0
                        )
                        SELECT TOP 1
                            node_id,
                            atom_id,
                            modality,
                            canonical_text,
                            path,
                            cost,
                            depth
                        FROM ShortestPathCTE
                        WHERE atom_id = @endAtom
                        ORDER BY cost ASC, depth ASC";

                    await using var command = new SqlCommand(shortestPathQuery, connection);
                    command.Parameters.AddWithValue("@startAtomId", request.StartAtomId);
                    command.Parameters.AddWithValue("@endAtomId", request.EndAtomId);

                    await using var reader = await command.ExecuteReaderAsync(cancellationToken);

                    if (!await reader.ReadAsync(cancellationToken))
                    {
                        return new SqlGraphShortestPathResponse
                        {
                            StartAtomId = request.StartAtomId,
                            EndAtomId = request.EndAtomId,
                            PathFound = false,
                            ExecutionTimeMs = 0
                        };
                    }

                    var nodeId = reader.GetInt64(0);
                    var atomId = reader.GetInt64(1);
                    var modality = reader.GetString(2);
                    var canonicalText = reader.IsDBNull(3) ? null : reader.GetString(3);
                    var pathStr = reader.GetString(4);
                    var cost = reader.GetDouble(5);
                    var depth = reader.GetInt32(6);

                    // Parse path
                    var atomIds = pathStr.Split("->").Select(id => long.Parse(id.Trim())).ToList();
                    var pathEntry = new SqlGraphPathEntry
                    {
                        NodeIds = new List<long>(),
                        AtomIds = atomIds,
                        EdgeTypes = new List<string>(),
                        PathLength = depth,
                        TotalWeight = cost
                    };

                    // Get node details for all nodes in path
                    var nodeDetailsQuery = @"
                        SELECT atom_id, $node_id
                        FROM dbo.Atom
                        WHERE atom_id IN (" + string.Join(",", atomIds) + ")";

                    await using var nodeCommand = new SqlCommand(nodeDetailsQuery, connection);
                    await using var nodeReader = await nodeCommand.ExecuteReaderAsync(cancellationToken);

                    var nodeMap = new Dictionary<long, long>();
                    while (await nodeReader.ReadAsync(cancellationToken))
                    {
                        nodeMap[nodeReader.GetInt64(0)] = nodeReader.GetInt64(1);
                    }

                    // Build node IDs list
                    foreach (var aid in atomIds)
                    {
                        if (nodeMap.TryGetValue(aid, out var nid))
                        {
                            pathEntry.NodeIds.Add(nid);
                        }
                    }

                    // Add default edge types (we don't have actual edge info from this query)
                    for (int i = 0; i < atomIds.Count - 1; i++)
                    {
                        pathEntry.EdgeTypes.Add("SimilarTo");
                    }

                    return new SqlGraphShortestPathResponse
                    {
                        StartAtomId = request.StartAtomId,
                        EndAtomId = request.EndAtomId,
                        ShortestPath = pathEntry,
                        PathFound = true,
                        ExecutionTimeMs = 0 // Will be set by performance monitor
                    };
                },
                new KeyValuePair<string, object?>("start_atom_id", request.StartAtomId),
                new KeyValuePair<string, object?>("end_atom_id", request.EndAtomId));

            // Update the execution time from the performance monitor
            operationResult.Result.ExecutionTimeMs = (int)operationResult.DurationMs;

            return Ok(Success(operationResult.Result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Shortest path search failed");
            var error = ErrorDetailFactory.Create(ErrorCodes.Infrastructure.DatabaseUnavailable, "Shortest path search failed", ex.Message);
            return StatusCode(500, Failure<SqlGraphShortestPathResponse>(new[] { error }));
        }
    }

    private static string GetSqlType(object value)
    {
        return value switch
        {
            string => "NVARCHAR(MAX)",
            int or long => "BIGINT",
            double or float => "FLOAT",
            bool => "BIT",
            DateTime => "DATETIME2",
            _ => "NVARCHAR(MAX)"
        };
    }
}
