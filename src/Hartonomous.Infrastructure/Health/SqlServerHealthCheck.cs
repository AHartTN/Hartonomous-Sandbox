using Azure.Core;
using Azure.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace Hartonomous.Infrastructure.Health;

/// <summary>
/// Health check for SQL Server connectivity using Arc-enabled managed identity authentication.
/// </summary>
public sealed class SqlServerHealthCheck : IHealthCheck
{
    private readonly ILogger<SqlServerHealthCheck> _logger;
    private readonly IConfiguration _configuration;
    private readonly TokenCredential _credential;

    public SqlServerHealthCheck(
        ILogger<SqlServerHealthCheck> logger,
        IConfiguration configuration)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _credential = new DefaultAzureCredential();
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var connectionString = _configuration.GetConnectionString("HartonomousDb");
            if (string.IsNullOrEmpty(connectionString))
            {
                return HealthCheckResult.Unhealthy("SQL Server connection string not configured.");
            }

            await using var connection = new SqlConnection(connectionString);

            // Authenticate using Arc-enabled managed identity
            var tokenRequestContext = new TokenRequestContext(["https://database.windows.net/.default"]);
            var token = await _credential.GetTokenAsync(tokenRequestContext, cancellationToken);
            connection.AccessToken = token.Token;

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
