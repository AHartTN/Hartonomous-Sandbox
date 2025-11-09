using CesConsumer.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CesConsumer;

/// <summary>
/// Hosted service that manages the CDC event processor lifecycle.
/// </summary>
public class CesConsumerService : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<CesConsumerService> _logger;
    private CancellationTokenSource? _cts;
    private Task? _processingTask;
    private AsyncServiceScope _scope;
    private bool _scopeCreated;
    private CdcEventProcessor? _processor;

    public CesConsumerService(IServiceScopeFactory scopeFactory, ILogger<CesConsumerService> logger)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting CES Consumer Service");

        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _scope = _scopeFactory.CreateAsyncScope();
        _scopeCreated = true;
        _processor = _scope.ServiceProvider.GetRequiredService<CdcEventProcessor>();
        _processingTask = _processor.StartAsync(_cts.Token);

        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping CES Consumer Service");

        if (_cts != null)
        {
            _cts.Cancel();
        }

        if (_processingTask != null)
        {
            try
            {
                await _processingTask.WaitAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // Expected during shutdown
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during processor shutdown");
            }
        }

        if (_scopeCreated)
        {
            await _scope.DisposeAsync();
            _scopeCreated = false;
            _processor = null;
        }

        _cts?.Dispose();
    }
}
