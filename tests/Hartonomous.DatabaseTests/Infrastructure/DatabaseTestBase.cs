using FluentAssertions;
using Microsoft.Data.SqlClient;
using System;
using System.Threading.Tasks;
using Testcontainers.MsSql;
using Xunit;

namespace Hartonomous.DatabaseTests.Infrastructure;

/// <summary>
/// Base class for database tests with hybrid LocalDB/Docker support
/// - Local dev: Uses LocalDB (fast, no Docker)
/// - CI/CD: Uses Testcontainers (consistent, isolated)
/// - Staging/Prod: Uses Azure SQL (configured via connection string)
/// </summary>
public abstract class DatabaseTestBase : IAsyncLifetime
{
    private MsSqlContainer? _sqlContainer;
    protected string ConnectionString { get; private set; } = null!;
    private readonly TestEnvironment _environment;

    protected DatabaseTestBase()
    {
        _environment = DetectEnvironment();
    }

    public async Task InitializeAsync()
    {
        switch (_environment)
        {
            case TestEnvironment.LocalDevelopment:
                await InitializeLocalDbAsync();
                break;
                
            case TestEnvironment.CiCd:
                await InitializeDockerAsync();
                break;
                
            case TestEnvironment.AzureSql:
                InitializeAzureSql();
                break;
        }
    }

    public async Task DisposeAsync()
    {
        if (_sqlContainer != null)
        {
            await _sqlContainer.DisposeAsync();
        }
    }

    private TestEnvironment DetectEnvironment()
    {
        // Check for Azure SQL connection string (staging/prod)
        var azureConnectionString = Environment.GetEnvironmentVariable("HARTONOMOUS_TEST_DB");
        if (!string.IsNullOrEmpty(azureConnectionString))
        {
            return TestEnvironment.AzureSql;
        }

        // Check for CI/CD environment
        var isCI = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CI")) ||
                   !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GITHUB_ACTIONS")) ||
                   !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("TF_BUILD")); // Azure DevOps

        return isCI ? TestEnvironment.CiCd : TestEnvironment.LocalDevelopment;
    }

    private async Task InitializeLocalDbAsync()
    {
        const string masterConnectionString = @"Server=(localdb)\mssqllocaldb;Database=master;Integrated Security=True;TrustServerCertificate=True;";
        ConnectionString = @"Server=(localdb)\mssqllocaldb;Database=HartonomousTests;Integrated Security=True;TrustServerCertificate=True;";

        // Create test database if it doesn't exist
        await using var connection = new SqlConnection(masterConnectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = @"
            IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'HartonomousTests')
            BEGIN
                CREATE DATABASE HartonomousTests;
            END";
        await command.ExecuteNonQueryAsync();

        Console.WriteLine($"? Using LocalDB (fast local development)");
    }

    private async Task InitializeDockerAsync()
    {
        // Use Testcontainers for CI/CD
        _sqlContainer = new MsSqlBuilder()
            .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
            .WithPassword("YourStrong@Passw0rd123!")
            .Build();

        await _sqlContainer.StartAsync();
        ConnectionString = _sqlContainer.GetConnectionString();

        Console.WriteLine($"? Using Docker container (CI/CD environment)");
    }

    private void InitializeAzureSql()
    {
        ConnectionString = Environment.GetEnvironmentVariable("HARTONOMOUS_TEST_DB")!;
        Console.WriteLine($"? Using Azure SQL (staging/production testing)");
    }

    protected async Task<SqlConnection> GetConnectionAsync()
    {
        var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();
        return connection;
    }
}

public enum TestEnvironment
{
    LocalDevelopment,  // LocalDB (fast)
    CiCd,             // Docker (consistent)
    AzureSql          // Azure SQL (production-like)
}
