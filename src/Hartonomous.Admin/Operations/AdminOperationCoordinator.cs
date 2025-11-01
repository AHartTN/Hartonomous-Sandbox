using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;

namespace Hartonomous.Admin.Operations;

public sealed class AdminOperationCoordinator
{
    private readonly Channel<AdminOperationWorkItem> _operationChannel;
    private readonly Channel<AdminOperationStatus> _updateChannel;
    private readonly ConcurrentDictionary<Guid, AdminOperationStatus> _operations = new();
    private readonly ILogger<AdminOperationCoordinator> _logger;

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

    public event EventHandler<AdminOperationStatus>? OperationUpdated;

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
