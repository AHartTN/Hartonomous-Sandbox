using Hartonomous.Core.Entities;

namespace Hartonomous.Core.Interfaces;

/// <summary>
/// Repository abstraction for ingestion job tracking.
/// </summary>
public interface IIngestionJobRepository
{
    Task<IngestionJob> StartJobAsync(IngestionJob job, CancellationToken cancellationToken = default);
    Task CompleteJobAsync(long jobId, string? status, CancellationToken cancellationToken = default);
    Task AddJobAtomsAsync(IEnumerable<IngestionJobAtom> jobAtoms, CancellationToken cancellationToken = default);
}
