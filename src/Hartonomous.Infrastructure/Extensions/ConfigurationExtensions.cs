using System;
using Microsoft.Extensions.Configuration;

namespace Hartonomous.Infrastructure.Extensions;

/// <summary>
/// Extension methods for safe configuration reading with fallbacks
/// </summary>
public static class ConfigurationExtensions
{
    /// <summary>
    /// Gets a configuration value or environment variable with a fallback default
    /// </summary>
    public static string GetConfigurationOrEnvironment(
        this IConfiguration configuration,
        string configKey,
        string? environmentKey = null,
        string? defaultValue = null)
    {
        // Try configuration first
        var value = configuration[configKey];
        if (!string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        // Try environment variable
        if (!string.IsNullOrWhiteSpace(environmentKey))
        {
            value = Environment.GetEnvironmentVariable(environmentKey);
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        // Return default or throw
        if (defaultValue != null)
        {
            return defaultValue;
        }

        throw new InvalidOperationException(
            $"Configuration key '{configKey}' not found in configuration or environment variables");
    }

    /// <summary>
    /// Gets a connection string from ConnectionStrings section or environment variable
    /// </summary>
    public static string GetConnectionStringOrEnvironment(
        this IConfiguration configuration,
        string name,
        string? environmentKey = null,
        string? defaultValue = null)
    {
        // Try ConnectionStrings section
        var connectionString = configuration.GetConnectionString(name);
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            return connectionString;
        }

        // Try environment variable
        if (!string.IsNullOrWhiteSpace(environmentKey))
        {
            connectionString = Environment.GetEnvironmentVariable(environmentKey);
            if (!string.IsNullOrWhiteSpace(connectionString))
            {
                return connectionString;
            }
        }

        // Return default or throw
        if (defaultValue != null)
        {
            return defaultValue;
        }

        throw new InvalidOperationException(
            $"Connection string '{name}' not found in configuration or environment variables");
    }
}
