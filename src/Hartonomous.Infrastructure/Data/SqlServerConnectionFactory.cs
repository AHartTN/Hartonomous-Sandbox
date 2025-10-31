using System;
using Hartonomous.Core.Configuration;
using Hartonomous.Core.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Hartonomous.Infrastructure.Data;

/// <summary>
/// Default implementation of <see cref="ISqlServerConnectionFactory"/> that opens connections using the configured primary connection string.
/// </summary>
public sealed class SqlServerConnectionFactory : ISqlServerConnectionFactory
{
    private readonly IOptionsMonitor<SqlServerOptions> _options;
    private readonly ILogger<SqlServerConnectionFactory> _logger;

    public SqlServerConnectionFactory(
        IOptionsMonitor<SqlServerOptions> options,
        ILogger<SqlServerConnectionFactory> logger)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<SqlConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken = default)
    {
        var connectionString = _options.CurrentValue.ConnectionString;
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("SQL Server connection string is not configured.");
        }

        var connection = new SqlConnection(connectionString);

        try
        {
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            return connection;
        }
        catch
        {
            await connection.DisposeAsync().ConfigureAwait(false);
            _logger.LogError("Failed to open SQL connection using configured connection string.");
            throw;
        }
    }
}
