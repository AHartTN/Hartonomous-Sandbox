using System;
using System.Threading;
using System.Threading.Tasks;
using Hartonomous.Core.Models;
using Microsoft.Extensions.Logging;
using Neo4j.Driver;

namespace Hartonomous.Neo4jSync.Services;

public sealed class KnowledgeEventHandler : BaseEventHandler
{
    private readonly ProvenanceGraphBuilder _graphBuilder;
    private readonly IDriver _driver;
    private readonly ILogger<KnowledgeEventHandler> _logger;

    public KnowledgeEventHandler(ProvenanceGraphBuilder graphBuilder, IDriver driver, ILogger<KnowledgeEventHandler> logger)
    {
        _graphBuilder = graphBuilder ?? throw new ArgumentNullException(nameof(graphBuilder));
        _driver = driver ?? throw new ArgumentNullException(nameof(driver));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override bool CanHandleCore(BaseEvent message)
        => TryGetSqlServerContext(message, out var table, out var operation)
            && string.Equals(table, "dbo.KnowledgeBase", StringComparison.OrdinalIgnoreCase)
            && string.Equals(operation, "insert", StringComparison.OrdinalIgnoreCase);

    protected override async Task HandleCoreAsync(BaseEvent message, CancellationToken cancellationToken)
    {
        await using var session = _driver.AsyncSession();
        await _graphBuilder.CreateKnowledgeNodeAsync(session, message, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Created knowledge node from event {EventId}", message.Id);
    }
}
