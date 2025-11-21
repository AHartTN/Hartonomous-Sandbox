using System.Text;
using Hartonomous.Core.Interfaces.Reasoning;
using Hartonomous.Data.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Hartonomous.Infrastructure.Services;

/// <summary>
/// SQL-backed implementation of context retrieval for LLM prompt hydration.
/// Uses sp_SpatialKNN for semantic neighbor discovery.
/// </summary>
public class ContextRetrievalService : IContextRetrievalService
{
    private readonly HartonomousDbContext _dbContext;
    private readonly ILogger<ContextRetrievalService> _logger;
    private const int MaxContextTokens = 8000; // Conservative limit for context window
    private const int EstimatedCharsPerToken = 4;

    public ContextRetrievalService(
        HartonomousDbContext dbContext,
        ILogger<ContextRetrievalService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<HydratedContext> HydrateContextAsync(
        string prompt,
        IEnumerable<long> contextAtomIds,
        int maxNeighbors = 5,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Hydrating context for prompt with {AtomCount} atoms, maxNeighbors={MaxNeighbors}",
            contextAtomIds.Count(), maxNeighbors);

        var atomIdList = contextAtomIds.ToList();
        var contextAtoms = (await GetAtomContextsAsync(atomIdList, cancellationToken)).ToList();

        // Get semantic neighbors for each atom
        var allNeighbors = new List<SpatialNeighbor>();
        foreach (var atomId in atomIdList.Take(10)) // Limit to prevent explosion
        {
            var neighbors = await GetSpatialNeighborsAsync(atomId, maxNeighbors, cancellationToken);
            allNeighbors.AddRange(neighbors);
        }

        // Deduplicate neighbors
        var uniqueNeighbors = allNeighbors
            .GroupBy(n => n.AtomId)
            .Select(g => g.OrderByDescending(n => n.Similarity).First())
            .Where(n => !atomIdList.Contains(n.AtomId))
            .OrderByDescending(n => n.Similarity)
            .Take(maxNeighbors * 3)
            .ToList();

        // Build the system prompt
        var systemPrompt = BuildSystemPrompt(prompt, contextAtoms, uniqueNeighbors, out var wasTruncated, out var estimatedTokens);

        return new HydratedContext
        {
            OriginalPrompt = prompt,
            SystemPrompt = systemPrompt,
            ContextAtoms = contextAtoms,
            SemanticNeighbors = uniqueNeighbors,
            EstimatedTokens = estimatedTokens,
            WasTruncated = wasTruncated
        };
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<AtomContext>> GetAtomContextsAsync(
        IEnumerable<long> atomIds,
        CancellationToken cancellationToken = default)
    {
        var idList = atomIds.ToList();
        if (!idList.Any())
            return Array.Empty<AtomContext>();

        _logger.LogDebug("Fetching context for {Count} atoms", idList.Count);

        var atoms = await _dbContext.Atoms
            .AsNoTracking()
            .Where(a => idList.Contains(a.AtomId))
            .Select(a => new AtomContext
            {
                AtomId = a.AtomId,
                CanonicalText = a.CanonicalText ?? $"[Atom {a.AtomId}]",
                Modality = a.Modality,
                Subtype = a.Subtype,
                Metadata = a.Metadata,
                RelevanceScore = 1.0 // Direct context atoms have full relevance
            })
            .ToListAsync(cancellationToken);

        return atoms;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<SpatialNeighbor>> GetSpatialNeighborsAsync(
        long atomId,
        int k = 10,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Finding {K} spatial neighbors for atom {AtomId}", k, atomId);

        try
        {
            var connection = _dbContext.Database.GetDbConnection();
            await connection.OpenAsync(cancellationToken);

            await using var command = connection.CreateCommand();
            command.CommandText = "EXEC dbo.sp_SpatialKNN @AtomId, @K";
            command.Parameters.Add(new SqlParameter("@AtomId", atomId));
            command.Parameters.Add(new SqlParameter("@K", k));

            var neighbors = new List<SpatialNeighbor>();
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var distance = reader.GetDouble(reader.GetOrdinal("Distance"));
                neighbors.Add(new SpatialNeighbor
                {
                    AtomId = reader.GetInt64(reader.GetOrdinal("NeighborAtomId")),
                    Distance = distance,
                    Similarity = 1.0 / (1.0 + distance), // Convert distance to similarity
                    CanonicalText = reader.IsDBNull(reader.GetOrdinal("CanonicalText"))
                        ? null
                        : reader.GetString(reader.GetOrdinal("CanonicalText"))
                });
            }

            return neighbors;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "sp_SpatialKNN failed for atom {AtomId}, returning empty neighbors", atomId);
            return Array.Empty<SpatialNeighbor>();
        }
    }

    private string BuildSystemPrompt(
        string userPrompt,
        List<AtomContext> contextAtoms,
        List<SpatialNeighbor> neighbors,
        out bool wasTruncated,
        out int estimatedTokens)
    {
        var sb = new StringBuilder();
        wasTruncated = false;

        sb.AppendLine("You are an AI assistant analyzing the following connected knowledge concepts.");
        sb.AppendLine();
        sb.AppendLine("## Primary Context Atoms");
        sb.AppendLine();

        var currentChars = sb.Length;
        var maxChars = MaxContextTokens * EstimatedCharsPerToken;

        foreach (var atom in contextAtoms)
        {
            var atomBlock = FormatAtomBlock(atom);
            if (currentChars + atomBlock.Length > maxChars * 0.7) // Reserve 30% for neighbors and response
            {
                wasTruncated = true;
                break;
            }
            sb.Append(atomBlock);
            currentChars += atomBlock.Length;
        }

        if (neighbors.Any())
        {
            sb.AppendLine();
            sb.AppendLine("## Semantically Related Concepts");
            sb.AppendLine();

            foreach (var neighbor in neighbors.Take(10))
            {
                var neighborBlock = $"- **Atom {neighbor.AtomId}** (similarity: {neighbor.Similarity:P0}): {neighbor.CanonicalText ?? "[No text]"}\n";
                if (currentChars + neighborBlock.Length > maxChars * 0.9)
                {
                    wasTruncated = true;
                    break;
                }
                sb.Append(neighborBlock);
                currentChars += neighborBlock.Length;
            }
        }

        sb.AppendLine();
        sb.AppendLine("## Instructions");
        sb.AppendLine("Use the above context to answer the user's query. Reference specific atoms when relevant.");
        sb.AppendLine("If the context is insufficient, acknowledge limitations rather than hallucinating.");

        estimatedTokens = sb.Length / EstimatedCharsPerToken;
        return sb.ToString();
    }

    private static string FormatAtomBlock(AtomContext atom)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"### Atom {atom.AtomId} ({atom.Modality}/{atom.Subtype ?? "unknown"})");
        sb.AppendLine($"**Content:** {atom.CanonicalText}");
        if (!string.IsNullOrEmpty(atom.Metadata) && atom.Metadata.Length < 200)
        {
            sb.AppendLine($"**Metadata:** {atom.Metadata}");
        }
        sb.AppendLine();
        return sb.ToString();
    }
}
