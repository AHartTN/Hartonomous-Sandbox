using System;
using Hartonomous.Core.Configuration;
using Hartonomous.Core.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Hartonomous.Infrastructure.Data;

/// <summary>
/// Centralised command executor that applies connection management, timeouts, and logging for raw SQL operations.
/// </summary>
public sealed class SqlCommandExecutor : ISqlCommandExecutor
{
    private readonly ISqlServerConnectionFactory _connectionFactory;
    private readonly IOptionsMonitor<SqlServerOptions> _options;
    private readonly ILogger<SqlCommandExecutor> _logger;

    public SqlCommandExecutor(
        ISqlServerConnectionFactory connectionFactory,
        IOptionsMonitor<SqlServerOptions> options,
        ILogger<SqlCommandExecutor> logger)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<TResult> ExecuteAsync<TResult>(Func<SqlCommand, CancellationToken, Task<TResult>> operation, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandTimeout = Math.Max(1, _options.CurrentValue.CommandTimeoutSeconds);

        try
        {
            return await operation(command, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SQL command execution failed.");
            throw;
        }
    }

    public async Task ExecuteAsync(Func<SqlCommand, CancellationToken, Task> operation, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandTimeout = Math.Max(1, _options.CurrentValue.CommandTimeoutSeconds);

        try
        {
            await operation(command, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SQL command execution failed.");
            throw;
        }
    }
}
