using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;

namespace Hartonomous.Admin.Operations;

/// <summary>
/// Coordinates asynchronous admin operations using channels for work queuing and status broadcasting.
/// Maintains operation status dictionary and publishes updates via events for real-time monitoring.
/// </summary>
public sealed class AdminOperationCoordinator
{
    private readonly Channel<AdminOperationWorkItem> _operationChannel;
    private readonly Channel<AdminOperationStatus> _updateChannel;
    private readonly ConcurrentDictionary<Guid, AdminOperationStatus> _operations = new();
    private readonly ILogger<AdminOperationCoordinator> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AdminOperationCoordinator"/> class.
    /// Creates unbounded channels for operation queuing and status updates.
    /// </summary>
    /// <param name="logger">Logger for tracking operation coordination.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="logger"/> is null.</exception>
    public AdminOperationCoordinator(ILogger<AdminOperationCoordinator> logger)
    {
        _operationChannel = Channel.CreateUnbounded<AdminOperationWorkItem>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false,
            AllowSynchronousContinuations = false
        });

        _updateChannel = Channel.CreateUnbounded<AdminOperationStatus>(new UnboundedChannelOptions
        {
            SingleReader = false,
            SingleWriter = false,
            AllowSynchronousContinuations = false
        });

        _logger = logger;
    }

    /// <summary>
    /// Event raised when an operation status changes (queued, running, completed, failed).
    /// </summary>
    public event EventHandler<AdminOperationStatus>? OperationUpdated;

    /// <summary>
    /// Enqueues an admin operation for background execution and returns initial status immediately.
    /// </summary>
    /// <param name="operationType">Type identifier for the operation (e.g., "model_ingestion", "database_maintenance").</param>
    /// <param name="description">Human-readable description of the operation.</param>
    /// <param name="work">Async function that performs the work and returns outcome.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>Initial operation status with Queued state and unique operation ID.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="operationType"/> or <paramref name="description"/> is null or whitespace.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="work"/> is null.</exception>
    public async ValueTask<AdminOperationStatus> EnqueueAsync(
        string operationType,
        string description,
        Func<CancellationToken, Task<AdminOperationOutcome>> work,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operationType);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        ArgumentNullException.ThrowIfNull(work);

        var status = new AdminOperationStatus(
            Guid.NewGuid(),
            operationType,
            description,
            AdminOperationState.Queued,
            DateTimeOffset.UtcNow,
            null,
            null,
            null,
            null);

        var workItem = new AdminOperationWorkItem(status, work);

        while (await _operationChannel.Writer.WaitToWriteAsync(cancellationToken).ConfigureAwait(false))
        {
            if (_operationChannel.Writer.TryWrite(workItem))
            {
                _operations[status.OperationId] = status;
                Publish(status);
                return status;
            }
        }

        throw new InvalidOperationException("Unable to queue operation");
    }

    internal async ValueTask<AdminOperationExecution> DequeueAsync(CancellationToken cancellationToken)
    {
        var item = await _operationChannel.Reader.ReadAsync(cancellationToken).ConfigureAwait(false);
        return new AdminOperationExecution(item.Status, item.Work);
    }

    internal void Publish(AdminOperationStatus status)
    {
        _operations[status.OperationId] = status;

        OperationUpdated?.Invoke(this, status);

        if (!_updateChannel.Writer.TryWrite(status))
        {
            _logger.LogDebug("Failed to broadcast operation update for {OperationId}", status.OperationId);
        }
    }

    public IReadOnlyCollection<AdminOperationStatus> GetRecent(int take = 20)
    {
        return _operations.Values
            .OrderByDescending(o => o.EnqueuedAt)
            .Take(take)
            .ToArray();
    }

    public bool TryGet(Guid operationId, out AdminOperationStatus status)
    {
        return _operations.TryGetValue(operationId, out status!);
    }

    public IAsyncEnumerable<AdminOperationStatus> WatchAsync(CancellationToken cancellationToken = default)
    {
        return _updateChannel.Reader.ReadAllAsync(cancellationToken);
    }

    private sealed record AdminOperationWorkItem(
        AdminOperationStatus Status,
        Func<CancellationToken, Task<AdminOperationOutcome>> Work);

    internal readonly record struct AdminOperationExecution(
        AdminOperationStatus Status,
        Func<CancellationToken, Task<AdminOperationOutcome>> Work);
}
