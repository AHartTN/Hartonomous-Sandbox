using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Hartonomous.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace Hartonomous.Infrastructure.Services.CDC;

/// <summary>
/// File-based implementation of ICdcCheckpointManager
/// Suitable for development and single-instance deployments
/// </summary>
public class FileCdcCheckpointManager : ICdcCheckpointManager
{
    private readonly string _checkpointFilePath;
    private readonly ILogger<FileCdcCheckpointManager> _logger;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public FileCdcCheckpointManager(
        ILogger<FileCdcCheckpointManager> logger,
        string? checkpointFilePath = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _checkpointFilePath = checkpointFilePath ?? "cdc_checkpoint.txt";
    }

    public async Task<string?> GetLastProcessedLsnAsync(CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (File.Exists(_checkpointFilePath))
            {
                var lsn = await File.ReadAllTextAsync(_checkpointFilePath, cancellationToken);
                _logger.LogDebug("Loaded checkpoint LSN: {Lsn}", lsn);
                return string.IsNullOrWhiteSpace(lsn) ? null : lsn.Trim();
            }

            _logger.LogDebug("No checkpoint file found, starting from beginning");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read checkpoint file, starting from beginning");
            return null;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task UpdateLastProcessedLsnAsync(string lsn, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(lsn))
        {
            throw new ArgumentException("LSN cannot be null or empty", nameof(lsn));
        }

        await _lock.WaitAsync(cancellationToken);
        try
        {
            await File.WriteAllTextAsync(_checkpointFilePath, lsn, cancellationToken);
            _logger.LogDebug("Updated checkpoint LSN: {Lsn}", lsn);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update checkpoint file");
            throw;
        }
        finally
        {
            _lock.Release();
        }
    }
}
