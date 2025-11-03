using System;
using System.Threading;
using System.Threading.Tasks;
using Hartonomous.Core.Models;
using Microsoft.Extensions.Logging;
using Neo4j.Driver;

namespace Hartonomous.Neo4jSync.Services;

public sealed class InferenceEventHandler : BaseEventHandler
{
    private readonly ProvenanceGraphBuilder _graphBuilder;
    private readonly IDriver _driver;
    private readonly ILogger<InferenceEventHandler> _logger;

    public InferenceEventHandler(ProvenanceGraphBuilder graphBuilder, IDriver driver, ILogger<InferenceEventHandler> logger)
    {
        _graphBuilder = graphBuilder ?? throw new ArgumentNullException(nameof(graphBuilder));
        _driver = driver ?? throw new ArgumentNullException(nameof(driver));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override bool CanHandleCore(BaseEvent message)
        => TryGetSqlServerContext(message, out var table, out var operation)
            && string.Equals(table, "dbo.InferenceRequests", StringComparison.OrdinalIgnoreCase)
            && string.Equals(operation, "insert", StringComparison.OrdinalIgnoreCase);

    protected override async Task HandleCoreAsync(BaseEvent message, CancellationToken cancellationToken)
    {
        await using var session = _driver.AsyncSession();
        await _graphBuilder.CreateInferenceNodeAsync(session, message, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Created inference node from event {EventId}", message.Id);
    }
}
