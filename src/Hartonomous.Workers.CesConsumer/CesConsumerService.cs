using CesConsumer.Services;
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
    private readonly CdcEventProcessor _processor;
    private readonly ILogger<CesConsumerService> _logger;
    private CancellationTokenSource? _cts;
    private Task? _processingTask;

    public CesConsumerService(CdcEventProcessor processor, ILogger<CesConsumerService> logger)
    {
        _processor = processor;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting CES Consumer Service");

        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
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
                await _processingTask;
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
    }
}