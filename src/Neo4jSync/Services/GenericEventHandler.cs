using System;
using System.Threading;
using System.Threading.Tasks;
using Hartonomous.Core.Models;
using Microsoft.Extensions.Logging;
using Neo4j.Driver;

namespace Hartonomous.Neo4jSync.Services;

public sealed class GenericEventHandler : BaseEventHandler
{
    private readonly ProvenanceGraphBuilder _graphBuilder;
    private readonly IDriver _driver;
    private readonly ILogger<GenericEventHandler> _logger;

    public GenericEventHandler(ProvenanceGraphBuilder graphBuilder, IDriver driver, ILogger<GenericEventHandler> logger)
    {
        _graphBuilder = graphBuilder ?? throw new ArgumentNullException(nameof(graphBuilder));
        _driver = driver ?? throw new ArgumentNullException(nameof(driver));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override bool CanHandleCore(BaseEvent message)
        => true;

    protected override async Task HandleCoreAsync(BaseEvent message, CancellationToken cancellationToken)
    {
        await using var session = _driver.AsyncSession();
        await _graphBuilder.CreateGenericEventNodeAsync(session, message, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Created generic event node for {EventId}", message.Id);
    }
}
