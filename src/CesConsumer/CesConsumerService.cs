using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CesConsumer;

/// <summary>
/// Hosted service that manages the CES consumer lifecycle.
/// </summary>
public class CesConsumerService : IHostedService
{
    private readonly CdcListener _cdcListener;
    private readonly ILogger<CesConsumerService> _logger;
    private CancellationTokenSource? _cts;

    public CesConsumerService(CdcListener cdcListener, ILogger<CesConsumerService> logger)
    {
        _cdcListener = cdcListener;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting CES Consumer Service");

        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        try
        {
            await _cdcListener.StartListeningAsync(_cts.Token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in CES Consumer Service");
            throw;
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping CES Consumer Service");

        if (_cts != null)
        {
            _cts.Cancel();
        }

        try
        {
            await _cdcListener.StopAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping CDC listener");
        }
    }
}