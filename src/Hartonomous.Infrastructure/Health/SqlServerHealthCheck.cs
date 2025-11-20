using Azure.Core;
using Azure.Identity;
using Hartonomous.Core.Configuration;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Hartonomous.Infrastructure.Health;

/// <summary>
/// Health check for SQL Server connectivity using Arc-enabled managed identity authentication.
/// </summary>
public sealed class SqlServerHealthCheck : IHealthCheck
{
    private readonly ILogger<SqlServerHealthCheck> _logger;
    private readonly string _connectionString;
    private readonly TokenCredential _credential;

    public SqlServerHealthCheck(
        ILogger<SqlServerHealthCheck> logger,
        IOptions<DatabaseOptions> options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        var databaseOptions = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _connectionString = databaseOptions.HartonomousDb;
        _credential = new DefaultAzureCredential();
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrEmpty(_connectionString))
            {
                return HealthCheckResult.Unhealthy("SQL Server connection string not configured.");
            }

            await using var connection = new SqlConnection(_connectionString);

            // Only use Azure managed identity authentication if not using integrated security
            if (!_connectionString.Contains("Integrated Security", StringComparison.OrdinalIgnoreCase) &&
                !_connectionString.Contains("Trusted_Connection", StringComparison.OrdinalIgnoreCase))
            {
                // Authenticate using Arc-enabled managed identity
                var tokenRequestContext = new TokenRequestContext(["https://database.windows.net/.default"]);
                var token = await _credential.GetTokenAsync(tokenRequestContext, cancellationToken);
                connection.AccessToken = token.Token;
            }

            await connection.OpenAsync(cancellationToken);

            // Execute simple query to verify connectivity
            await using var command = new SqlCommand("SELECT 1", connection);
            var result = await command.ExecuteScalarAsync(cancellationToken);

            if (result is int value && value == 1)
            {
                _logger.LogDebug("SQL Server health check passed.");
                return HealthCheckResult.Healthy("SQL Server is responsive.");
            }

            return HealthCheckResult.Unhealthy("SQL Server query returned unexpected result.");
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "SQL Server health check failed.");
            return HealthCheckResult.Unhealthy("SQL Server connection failed.", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SQL Server health check encountered an error.");
            return HealthCheckResult.Unhealthy("SQL Server health check error.", ex);
        }
    }
}
