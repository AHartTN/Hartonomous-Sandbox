using Azure.Core;
using Azure.Identity;
using Hartonomous.Core.Configuration;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Hartonomous.Infrastructure.Data;

/// <summary>
/// Default implementation of SQL connection factory with managed identity support.
/// Eliminates duplicate SetupConnectionAsync methods in 20+ service classes.
/// </summary>
public class SqlConnectionFactory : ISqlConnectionFactory
{
    private readonly string _connectionString;
    private readonly TokenCredential _credential;

    public SqlConnectionFactory(IOptions<DatabaseOptions> options)
    {
        if (options?.Value == null)
            throw new ArgumentNullException(nameof(options));
            
        _connectionString = options.Value.HartonomousDb 
            ?? throw new InvalidOperationException("HartonomousDb connection string not configured");
        _credential = new DefaultAzureCredential();
    }

    public async Task<SqlConnection> CreateConnectionAsync(CancellationToken cancellationToken = default)
    {
        var connection = CreateConnection();
        await SetupConnectionAsync(connection, cancellationToken);
        return connection;
    }

    public SqlConnection CreateConnection()
    {
        return new SqlConnection(_connectionString);
    }

    private async Task SetupConnectionAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        // Managed Identity authentication
        if (!_connectionString.Contains("Password=", StringComparison.OrdinalIgnoreCase) &&
            !_connectionString.Contains("Integrated Security=true", StringComparison.OrdinalIgnoreCase))
        {
            var tokenContext = new TokenRequestContext(new[] { "https://database.windows.net/.default" });
            var token = await _credential.GetTokenAsync(tokenContext, cancellationToken);
            connection.AccessToken = token.Token;
        }

        await connection.OpenAsync(cancellationToken);
    }
}
