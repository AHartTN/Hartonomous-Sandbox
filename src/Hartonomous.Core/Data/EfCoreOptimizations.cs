using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Hartonomous.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Hartonomous.Core.Data;

/// <summary>
/// Compiled queries for hot paths to eliminate EF Core expression tree translation overhead.
/// Provides 10-100x performance improvement for frequently executed queries.
/// </summary>
public static class CompiledQueries
{
    // ============================================================
    // Atom Queries
    // ============================================================

    /// <summary>
    /// Gets atom by content hash. Compiled for performance.
    /// </summary>
    public static readonly Func<HartonomousDbContext, byte[], CancellationToken, Task<Atom?>> GetAtomByContentHash =
        EF.CompileAsyncQuery((HartonomousDbContext ctx, byte[] hash, CancellationToken ct) =>
            ctx.Atoms
                .AsNoTracking()
                .FirstOrDefault(a => a.ContentHash == hash));

    /// <summary>
    /// Gets atom by ID with embeddings. Uses split query to avoid cartesian explosion.
    /// </summary>
    public static readonly Func<HartonomousDbContext, long, CancellationToken, Task<Atom?>> GetAtomByIdWithEmbeddings =
        EF.CompileAsyncQuery((HartonomousDbContext ctx, long atomId, CancellationToken ct) =>
            ctx.Atoms
                .AsNoTracking()
                .Include(a => a.Embeddings) // Split query recommended for collections
                .AsSplitQuery() // Prevents cartesian explosion
                .FirstOrDefault(a => a.AtomId == atomId));

    /// <summary>
    /// Gets active atoms by modality.
    /// </summary>
    public static readonly Func<HartonomousDbContext, string, int, CancellationToken, Task<List<Atom>>> GetActiveAtomsByModality =
        EF.CompileAsyncQuery((HartonomousDbContext ctx, string modality, int limit, CancellationToken ct) =>
            ctx.Atoms
                .AsNoTracking()
                .Where(a => a.Modality == modality && a.IsActive)
                .OrderByDescending(a => a.CreatedAt)
                .Take(limit)
                .ToList());

    /// <summary>
    /// Gets atoms with high reference counts (popular content).
    /// </summary>
    public static readonly Func<HartonomousDbContext, int, int, CancellationToken, Task<List<Atom>>> GetTopReferencedAtoms =
        EF.CompileAsyncQuery((HartonomousDbContext ctx, int minReferenceCount, int limit, CancellationToken ct) =>
            ctx.Atoms
                .AsNoTracking()
                .Where(a => a.ReferenceCount >= minReferenceCount && a.IsActive)
                .OrderByDescending(a => a.ReferenceCount)
                .ThenByDescending(a => a.CreatedAt)
                .Take(limit)
                .ToList());

    // ============================================================
    // AtomEmbedding Queries
    // ============================================================

    /// <summary>
    /// Gets embeddings by atom ID. Split query for safety.
    /// </summary>
    public static readonly Func<HartonomousDbContext, long, CancellationToken, Task<List<AtomEmbedding>>> GetEmbeddingsByAtomId =
        EF.CompileAsyncQuery((HartonomousDbContext ctx, long atomId, CancellationToken ct) =>
            ctx.AtomEmbeddings
                .AsNoTracking()
                .Where(e => e.AtomId == atomId)
                .ToList());

    /// <summary>
    /// Gets embedding by type and model. Compiled for deduplication checks.
    /// </summary>
    public static readonly Func<HartonomousDbContext, long, string, int?, CancellationToken, Task<AtomEmbedding?>> GetEmbeddingByTypeAndModel =
        EF.CompileAsyncQuery((HartonomousDbContext ctx, long atomId, string embeddingType, int? modelId, CancellationToken ct) =>
            ctx.AtomEmbeddings
                .AsNoTracking()
                .FirstOrDefault(e =>
                    e.AtomId == atomId &&
                    e.EmbeddingType == embeddingType &&
                    (modelId == null || e.ModelId == modelId)));

    // ============================================================
    // InferenceRequest Queries
    // ============================================================

    /// <summary>
    /// Gets inference request by ID with steps and models.
    /// Uses split query to avoid N+1 and cartesian explosion.
    /// </summary>
    public static readonly Func<HartonomousDbContext, long, CancellationToken, Task<InferenceRequest?>> GetInferenceRequestWithSteps =
        EF.CompileAsyncQuery((HartonomousDbContext ctx, long inferenceId, CancellationToken ct) =>
            ctx.InferenceRequests
                .AsNoTracking()
                .Include(i => i.Steps)
                    .ThenInclude(s => s.Model) // Nested Include
                .AsSplitQuery() // Critical: Prevents cartesian explosion on nested collections
                .FirstOrDefault(i => i.InferenceId == inferenceId));

    /// <summary>
    /// Gets recent inference requests by correlation ID.
    /// </summary>
    public static readonly Func<HartonomousDbContext, string, int, CancellationToken, Task<List<InferenceRequest>>> GetInferenceRequestsByCorrelationId =
        EF.CompileAsyncQuery((HartonomousDbContext ctx, string correlationId, int limit, CancellationToken ct) =>
            ctx.InferenceRequests
                .AsNoTracking()
                .Where(i => i.CorrelationId == correlationId)
                .OrderByDescending(i => i.CreatedAt)
                .Take(limit)
                .ToList());

    /// <summary>
    /// Gets inference requests with high confidence.
    /// </summary>
    public static readonly Func<HartonomousDbContext, double, int, CancellationToken, Task<List<InferenceRequest>>> GetHighConfidenceInferences =
        EF.CompileAsyncQuery((HartonomousDbContext ctx, double minConfidence, int limit, CancellationToken ct) =>
            ctx.InferenceRequests
                .AsNoTracking()
                .Where(i => i.Confidence >= minConfidence)
                .OrderByDescending(i => i.Confidence)
                .ThenByDescending(i => i.CreatedAt)
                .Take(limit)
                .ToList());

    // ============================================================
    // Model Queries
    // ============================================================

    /// <summary>
    /// Gets active models ordered by weight.
    /// </summary>
    public static readonly Func<HartonomousDbContext, CancellationToken, Task<List<Model>>> GetActiveModelsByWeight =
        EF.CompileAsyncQuery((HartonomousDbContext ctx, CancellationToken ct) =>
            ctx.Models
                .AsNoTracking()
                .Where(m => m.IsActive)
                .OrderByDescending(m => m.Weight ?? 1.0)
                .ThenBy(m => m.ModelName)
                .ToList());

    /// <summary>
    /// Gets models by IDs. Compiled for ensemble invocation.
    /// </summary>
    public static readonly Func<HartonomousDbContext, List<int>, CancellationToken, Task<List<Model>>> GetModelsByIds =
        EF.CompileAsyncQuery((HartonomousDbContext ctx, List<int> modelIds, CancellationToken ct) =>
            ctx.Models
                .AsNoTracking()
                .Where(m => modelIds.Contains(m.ModelId))
                .ToList());

    // ============================================================
    // DeduplicationPolicy Queries
    // ============================================================

    /// <summary>
    /// Gets active deduplication policy by name.
    /// </summary>
    public static readonly Func<HartonomousDbContext, string, CancellationToken, Task<DeduplicationPolicy?>> GetActivePolicyByName =
        EF.CompileAsyncQuery((HartonomousDbContext ctx, string policyName, CancellationToken ct) =>
            ctx.DeduplicationPolicies
                .AsNoTracking()
                .FirstOrDefault(p => p.PolicyName == policyName && p.IsActive));

    /// <summary>
    /// Gets all active deduplication policies.
    /// </summary>
    public static readonly Func<HartonomousDbContext, CancellationToken, Task<List<DeduplicationPolicy>>> GetActivePolicies =
        EF.CompileAsyncQuery((HartonomousDbContext ctx, CancellationToken ct) =>
            ctx.DeduplicationPolicies
                .AsNoTracking()
                .Where(p => p.IsActive)
                .OrderBy(p => p.PolicyName)
                .ToList());
}

/// <summary>
/// Extension methods for EF Core performance optimizations.
/// </summary>
public static class EfCoreOptimizationExtensions
{
    /// <summary>
    /// Applies standard no-tracking query optimizations for read-only queries.
    /// Use this when you don't need to update entities.
    /// </summary>
    public static IQueryable<T> AsOptimizedReadOnly<T>(this IQueryable<T> query)
        where T : class
    {
        return query
            .AsNoTracking() // Disables change tracking (faster)
            .AsNoTrackingWithIdentityResolution(); // Resolves same entity references without tracking
    }

    /// <summary>
    /// Applies split query for collections to avoid cartesian explosion.
    /// Use when querying entities with Include() on collections.
    /// </summary>
    public static IQueryable<T> AsSplitQueryOptimized<T>(this IQueryable<T> query)
        where T : class
    {
        return query
            .AsNoTracking()
            .AsSplitQuery(); // Executes 1 query per collection (avoids cartesian explosion)
    }

    /// <summary>
    /// Streams results using IAsyncEnumerable to avoid ToList() buffering.
    /// Critical when using EF retry strategies (prevents double-buffering).
    /// </summary>
    public static async IAsyncEnumerable<T> StreamResultsAsync<T>(
        this IQueryable<T> query,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        where T : class
    {
        await foreach (var item in query.AsAsyncEnumerable().WithCancellation(cancellationToken))
        {
            yield return item;
        }
    }
}

/// <summary>
/// Batch operation helpers for efficient bulk inserts/updates.
/// </summary>
public static class EfCoreBatchOperations
{
    /// <summary>
    /// Batch inserts entities with optimized batch size.
    /// Default EF Core batch size is 42 statements.
    /// </summary>
    public static async Task BatchInsertAsync<T>(
        this HartonomousDbContext context,
        IEnumerable<T> entities,
        int batchSize = 42,
        CancellationToken cancellationToken = default)
        where T : class
    {
        var batch = new List<T>(batchSize);

        foreach (var entity in entities)
        {
            batch.Add(entity);

            if (batch.Count >= batchSize)
            {
                await context.AddRangeAsync(batch, cancellationToken);
                await context.SaveChangesAsync(cancellationToken);

                batch.Clear();
                context.ChangeTracker.Clear(); // Clear tracking to free memory
            }
        }

        // Insert remaining entities
        if (batch.Any())
        {
            await context.AddRangeAsync(batch, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
        }
    }

    /// <summary>
    /// Batch updates entities with optimized batch size.
    /// </summary>
    public static async Task BatchUpdateAsync<T>(
        this HartonomousDbContext context,
        IEnumerable<T> entities,
        int batchSize = 42,
        CancellationToken cancellationToken = default)
        where T : class
    {
        var batch = new List<T>(batchSize);

        foreach (var entity in entities)
        {
            batch.Add(entity);

            if (batch.Count >= batchSize)
            {
                context.UpdateRange(batch);
                await context.SaveChangesAsync(cancellationToken);

                batch.Clear();
                context.ChangeTracker.Clear();
            }
        }

        if (batch.Any())
        {
            context.UpdateRange(batch);
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}

/// <summary>
/// Example repository demonstrating compiled query usage.
/// </summary>
public sealed class OptimizedAtomRepository : IAtomRepository
{
    private readonly HartonomousDbContext _context;

    public OptimizedAtomRepository(HartonomousDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public Task<Atom?> GetByContentHashAsync(byte[] contentHash, CancellationToken cancellationToken = default)
    {
        // Uses compiled query (10-100x faster than ad-hoc query)
        return CompiledQueries.GetAtomByContentHash(_context, contentHash, cancellationToken);
    }

    public Task<Atom?> GetByIdAsync(long atomId, CancellationToken cancellationToken = default)
    {
        // Uses compiled query with split query for collections
        return CompiledQueries.GetAtomByIdWithEmbeddings(_context, atomId, cancellationToken);
    }

    public Task<List<Atom>> GetActiveByModalityAsync(string modality, int limit, CancellationToken cancellationToken = default)
    {
        // Uses compiled query
        return CompiledQueries.GetActiveAtomsByModality(_context, modality, limit, cancellationToken);
    }

    public Task<List<Atom>> GetTopReferencedAsync(int minReferenceCount, int limit, CancellationToken cancellationToken = default)
    {
        // Uses compiled query
        return CompiledQueries.GetTopReferencedAtoms(_context, minReferenceCount, limit, cancellationToken);
    }

    public async Task<Atom> AddAsync(Atom atom, CancellationToken cancellationToken = default)
    {
        await _context.Atoms.AddAsync(atom, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return atom;
    }

    public async Task IncrementReferenceCountAsync(long atomId, int increment, CancellationToken cancellationToken = default)
    {
        // Use ExecuteUpdate for efficient single-field update (no tracking)
        await _context.Atoms
            .Where(a => a.AtomId == atomId)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(a => a.ReferenceCount, a => a.ReferenceCount + increment),
                cancellationToken);
    }
}
