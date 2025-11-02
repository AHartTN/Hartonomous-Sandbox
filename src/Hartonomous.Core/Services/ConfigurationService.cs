using System;
using Microsoft.Extensions.Configuration;

namespace Hartonomous.Core.Services;

/// <summary>
/// Centralized configuration service with environment variable fallback.
/// Eliminates scattered Environment.GetEnvironmentVariable calls throughout codebase.
/// </summary>
public interface IConfigurationService
{
    /// <summary>
    /// Gets a configuration value with optional default.
    /// Checks IConfiguration first, then environment variables, then returns default.
    /// </summary>
    string? GetValue(string key, string? defaultValue = null);

    /// <summary>
    /// Gets a required configuration value. Throws if not found.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when value not found</exception>
    string GetRequiredValue(string key);

    /// <summary>
    /// Gets a configuration value and parses it to specified type.
    /// </summary>
    T GetValue<T>(string key, T defaultValue);

    /// <summary>
    /// Gets a connection string from configuration.
    /// </summary>
    string? GetConnectionString(string name);

    /// <summary>
    /// Gets a required connection string. Throws if not found.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when connection string not found</exception>
    string GetRequiredConnectionString(string name);
}

/// <summary>
/// Default implementation using IConfiguration with environment variable fallback.
/// </summary>
public class ConfigurationService : IConfigurationService
{
    private readonly IConfiguration _configuration;

    public ConfigurationService(IConfiguration configuration)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    public string? GetValue(string key, string? defaultValue = null)
    {
        // Try IConfiguration first
        var value = _configuration[key];
        if (!string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        // Try environment variable (replace : with __)
        var envKey = key.Replace(":", "__");
        value = Environment.GetEnvironmentVariable(envKey);
        if (!string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        return defaultValue;
    }

    public string GetRequiredValue(string key)
    {
        var value = GetValue(key);
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(
                $"Required configuration value '{key}' not found in configuration or environment variables.");
        }
        return value;
    }

    public T GetValue<T>(string key, T defaultValue)
    {
        var stringValue = GetValue(key);
        if (string.IsNullOrWhiteSpace(stringValue))
        {
            return defaultValue;
        }

        try
        {
            return (T)Convert.ChangeType(stringValue, typeof(T));
        }
        catch
        {
            return defaultValue;
        }
    }

    public string? GetConnectionString(string name)
    {
        return _configuration.GetConnectionString(name)
            ?? Environment.GetEnvironmentVariable($"ConnectionStrings__{name}");
    }

    public string GetRequiredConnectionString(string name)
    {
        var connectionString = GetConnectionString(name);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"Required connection string '{name}' not found in configuration.");
        }
        return connectionString;
    }
}
