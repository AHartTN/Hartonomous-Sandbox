using Microsoft.Data.SqlClient;
using System;
using System.Threading.Tasks;
using Testcontainers.MsSql;
using Xunit;

namespace Hartonomous.DatabaseTests.Infrastructure;

/// <summary>
/// Base class for database tests using Testcontainers (Docker) when available
/// - Provides isolated SQL Server instance per test class (if Docker available)
/// - Tests should be written to work without Docker by using EF Core in-memory provider
/// - Works consistently across local dev and CI/CD
/// </summary>
public abstract class DatabaseTestBase : IAsyncLifetime
{
    private MsSqlContainer? _sqlContainer;
    protected string ConnectionString { get; private set; } = null!;
    protected bool IsDockerAvailable { get; private set; } = false;

    public async Task InitializeAsync()
    {
        try
        {
            // Try to use Testcontainers
            _sqlContainer = new MsSqlBuilder()
                .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
                .WithPassword("YourStrong@Passw0rd123!")
                .Build();

            await _sqlContainer.StartAsync();
            ConnectionString = _sqlContainer.GetConnectionString();
            IsDockerAvailable = true;

            Console.WriteLine($"? SQL Server container started: {ConnectionString}");
        }
        catch (Exception ex) when (ex.Message.Contains("Docker"))
        {
            // Docker not available - tests should use EF Core in-memory provider instead
            Console.WriteLine("? Docker not available - tests should use EF Core in-memory provider");
            IsDockerAvailable = false;
            ConnectionString = string.Empty;
        }
    }

    public async Task DisposeAsync()
    {
        if (_sqlContainer != null)
        {
            await _sqlContainer.DisposeAsync();
            Console.WriteLine("? SQL Server container stopped");
        }
    }

    protected async Task<SqlConnection> GetConnectionAsync()
    {
        if (!IsDockerAvailable)
        {
            throw new InvalidOperationException("Docker not available - use EF Core in-memory provider instead");
        }
        
        var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();
        return connection;
    }
}
