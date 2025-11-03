using Hartonomous.Core.Entities;
using Hartonomous.Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Hartonomous.Infrastructure.Repositories;

/// <summary>
/// Repository for managing inference requests and tracking inference execution history.
/// </summary>
public class InferenceRepository : EfRepository<Inference, long>, IInferenceRepository
{
    public InferenceRepository(
        HartonomousDbContext context,
        ILogger<InferenceRepository> logger)
        : base(context, logger)
    {
    }

    /// <summary>
    /// Retrieves all inference requests for a specific model, ordered by most recent first.
    /// </summary>
    /// <param name="modelId">The model identifier to filter by.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of inference requests for the specified model.</returns>
    public async Task<IReadOnlyList<Inference>> GetByModelIdAsync(
        int modelId,
        CancellationToken cancellationToken = default)
    {
        return await Context.Inferences
            .Where(i => i.ModelId == modelId)
            .OrderByDescending(i => i.CreatedAt)
            .Take(100)
            .AsNoTracking()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Retrieves the most recent inference requests across all models.
    /// </summary>
    /// <param name="count">Number of recent inferences to retrieve.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of recent inference requests.</returns>
    public async Task<IReadOnlyList<Inference>> GetRecentAsync(
        int count = 50,
        CancellationToken cancellationToken = default)
    {
        return await Context.Inferences
            .OrderByDescending(i => i.CreatedAt)
            .Take(count)
            .AsNoTracking()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Retrieves all inferences by type (e.g., "semantic_search", "ensemble", "generation").
    /// </summary>
    /// <param name="inferenceType">The type of inference to filter by.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of inferences matching the specified type.</returns>
    public async Task<IReadOnlyList<Inference>> GetByTypeAsync(
        string inferenceType,
        CancellationToken cancellationToken = default)
    {
        return await Context.Inferences
            .Where(i => i.InferenceType == inferenceType)
            .OrderByDescending(i => i.CreatedAt)
            .Take(100)
            .AsNoTracking()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Retrieves failed inference requests for debugging and analysis.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of failed inference requests.</returns>
    public async Task<IReadOnlyList<Inference>> GetFailedAsync(
        CancellationToken cancellationToken = default)
    {
        return await Context.Inferences
            .Where(i => i.Status == "failed")
            .OrderByDescending(i => i.CreatedAt)
            .Take(50)
            .AsNoTracking()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
