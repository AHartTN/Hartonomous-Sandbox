using System.Text.Json;

namespace Hartonomous.UnitTests;

/// <summary>
/// Centralized test configuration for connection strings and other test settings.
/// Priority: Environment variable > Local config file > CI Default (Testcontainers)
/// </summary>
public static class TestConfiguration
{
    /// <summary>
    /// Gets the SQL Server connection string for integration tests.
    /// Supports multiple configuration sources for flexibility across environments.
    /// </summary>
    public static string GetConnectionString()
    {
        // Priority 1: Environment variable (CI/CD, Azure Pipelines)
        var envConnectionString = Environment.GetEnvironmentVariable("TEST_CONNECTION_STRING");
        if (!string.IsNullOrWhiteSpace(envConnectionString))
        {
            return envConnectionString;
        }

        // Priority 2: Local config file (developer workstations)
        var localConnectionString = GetLocalConnectionString();
        if (!string.IsNullOrWhiteSpace(localConnectionString))
        {
            return localConnectionString;
        }

        // Priority 3: Check if running in CI
        var isCI = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CI")) ||
                   !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GITHUB_ACTIONS"));

        // Priority 4: Default based on environment
        return isCI ? GetCIDefaultConnectionString() : GetDefaultConnectionString();
    }

    private static string? GetLocalConnectionString()
    {
        try
        {
            // Read from local developer config (not committed to git)
            var configPath = Path.Combine(AppContext.BaseDirectory, "local.config.json");
            if (!File.Exists(configPath))
            {
                return null;
            }

            var jsonContent = File.ReadAllText(configPath);
            var config = JsonSerializer.Deserialize<LocalTestConfig>(jsonContent);
            return config?.ConnectionString;
        }
        catch
        {
            // If config file is malformed, fall back to default
            return null;
        }
    }

    private static string GetDefaultConnectionString()
    {
        // Default for local development using SQL Server LocalDB (Windows only)
        return @"Server=(localdb)\mssqllocaldb;Database=Hartonomous_UnitTests;Trusted_Connection=True;ConnectRetryCount=0;TrustServerCertificate=True;";
    }

    private static string GetCIDefaultConnectionString()
    {
        // CI environments should use DatabaseTestBase with Testcontainers
        // This connection string won't actually be used because SqlServerTestFixture
        // will detect CI and skip - tests should use DatabaseTestBase instead
        throw new PlatformNotSupportedException(
            "LocalDB is not supported in CI environments. " +
            "Tests requiring SQL Server should inherit from DatabaseTestBase which uses Testcontainers in CI.");
    }

    private class LocalTestConfig
    {
        public string? ConnectionString { get; set; }
    }
}
