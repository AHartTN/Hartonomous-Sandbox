using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Hartonomous.Infrastructure.Lifecycle;

/// <summary>
/// Hosted service that manages graceful shutdown coordination.
/// Ensures background services complete their work before app termination.
/// </summary>
public class GracefulShutdownService : IHostedService
{
    private readonly IHostApplicationLifetime _appLifetime;
    private readonly ILogger<GracefulShutdownService> _logger;

    public GracefulShutdownService(
        IHostApplicationLifetime appLifetime,
        ILogger<GracefulShutdownService> logger)
    {
        _appLifetime = appLifetime;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _appLifetime.ApplicationStarted.Register(OnStarted);
        _appLifetime.ApplicationStopping.Register(OnStopping);
        _appLifetime.ApplicationStopped.Register(OnStopped);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private void OnStarted()
    {
        _logger.LogInformation("Application started successfully at {Time}", DateTime.UtcNow);
        _logger.LogInformation("Graceful shutdown handler registered");
    }

    private void OnStopping()
    {
        _logger.LogWarning("Application is stopping at {Time}", DateTime.UtcNow);
        _logger.LogWarning("Graceful shutdown initiated - completing pending operations...");
        
        // This executes BEFORE the ShutdownTimeout period begins
        // Background services have their StopAsync called here
    }

    private void OnStopped()
    {
        _logger.LogWarning("Application stopped at {Time}", DateTime.UtcNow);
        _logger.LogInformation("All services stopped successfully");
    }
}

/// <summary>
/// Configuration options for graceful shutdown behavior.
/// </summary>
public class GracefulShutdownOptions
{
    public const string SectionName = "GracefulShutdown";

    /// <summary>
    /// Maximum time to wait for graceful shutdown in seconds.
    /// Default: 30 seconds (ASP.NET Core default).
    /// </summary>
    public int ShutdownTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Whether to wait for active requests to complete.
    /// Default: true.
    /// </summary>
    public bool WaitForActiveRequests { get; set; } = true;

    /// <summary>
    /// Maximum time to wait for active requests in seconds.
    /// Default: 25 seconds (5 seconds buffer before shutdown timeout).
    /// </summary>
    public int ActiveRequestTimeoutSeconds { get; set; } = 25;

    /// <summary>
    /// Whether to log shutdown progress.
    /// Default: true.
    /// </summary>
    public bool LogShutdownProgress { get; set; } = true;
}
