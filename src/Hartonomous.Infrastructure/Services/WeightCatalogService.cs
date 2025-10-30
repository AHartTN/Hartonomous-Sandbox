using Hartonomous.Core.Entities;
using Hartonomous.Core.Interfaces;
using Hartonomous.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Hartonomous.Infrastructure.Services;

/// <summary>
/// Service for managing WeightCatalog and content-based deduplication.
/// </summary>
public class WeightCatalogService : IWeightCatalogService
{
    private readonly HartonomousDbContext _context;
    private readonly ILogger<WeightCatalogService> _logger;

    public WeightCatalogService(
        HartonomousDbContext context,
        ILogger<WeightCatalogService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<WeightCatalog> AddToCatalogAsync(
        long weightId,
        int modelId,
        int layerIdx,
        string componentType,
        byte[] contentHash,
        float? importanceScore = null,
        string? positionMetadata = null,
        CancellationToken cancellationToken = default)
    {
        var catalog = new WeightCatalog
        {
            WeightId = weightId,
            ModelId = modelId,
            LayerIdx = layerIdx,
            ComponentType = componentType,
            ContentHash = contentHash,
            ImportanceScore = importanceScore,
            PositionMetadata = positionMetadata,
            CreatedDate = DateTime.UtcNow
        };

        _context.WeightCatalogs.Add(catalog);
        await _context.SaveChangesAsync(cancellationToken);

        return catalog;
    }

    public async Task<IReadOnlyList<(byte[] Hash, int Count, IReadOnlyList<int> ModelIds)>> FindDuplicatesAsync(
        int? modelId = null,
        int minDuplicates = 2,
        CancellationToken cancellationToken = default)
    {
        var query = _context.WeightCatalogs
            .Join(_context.ModelArchitectures,
                wc => wc.ModelId,
                ma => ma.ModelId,
                (wc, ma) => new { WeightCatalog = wc, ModelArch = ma });

        if (modelId.HasValue)
        {
            // Get dimension of specified model
            var targetModel = await _context.ModelArchitectures
                .FirstOrDefaultAsync(m => m.ModelId == modelId.Value, cancellationToken);

            if (targetModel == null)
            {
                return Array.Empty<(byte[], int, IReadOnlyList<int>)>();
            }

            // Only find duplicates within same dimension
            query = query.Where(x => x.ModelArch.EmbeddingDimension == targetModel.EmbeddingDimension);
        }

        var duplicates = await query
            .GroupBy(x => new { x.WeightCatalog.ContentHash, x.ModelArch.EmbeddingDimension })
            .Select(g => new
            {
                Hash = g.Key.ContentHash,
                Dimension = g.Key.EmbeddingDimension,
                Count = g.Count(),
                ModelIds = g.Select(x => x.WeightCatalog.ModelId).Distinct().ToList()
            })
            .Where(x => x.Count >= minDuplicates)
            .OrderByDescending(x => x.Count)
            .ToListAsync(cancellationToken);

        return duplicates
            .Select(d => (d.Hash, d.Count, (IReadOnlyList<int>)d.ModelIds))
            .ToList();
    }

    public async Task<long?> FindExistingWeightByHashAsync(
        int modelId,
        byte[] contentHash,
        CancellationToken cancellationToken = default)
    {
        var existing = await _context.WeightCatalogs
            .Where(wc => wc.ModelId == modelId && wc.ContentHash == contentHash)
            .Select(wc => wc.WeightId)
            .FirstOrDefaultAsync(cancellationToken);

        return existing == 0 ? null : existing;
    }

    public async Task<IReadOnlyList<WeightCatalog>> GetByModelAsync(
        int modelId,
        CancellationToken cancellationToken = default)
    {
        return await _context.WeightCatalogs
            .Where(wc => wc.ModelId == modelId)
            .OrderBy(wc => wc.LayerIdx)
            .ThenBy(wc => wc.ComponentType)
            .ToListAsync(cancellationToken);
    }

    public async Task UpdateImportanceScoreAsync(
        long catalogId,
        float importanceScore,
        CancellationToken cancellationToken = default)
    {
        var catalog = await _context.WeightCatalogs.FindAsync(new object[] { catalogId }, cancellationToken);
        if (catalog != null)
        {
            catalog.ImportanceScore = importanceScore;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
