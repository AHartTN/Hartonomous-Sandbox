using System.ComponentModel.DataAnnotations;

namespace Hartonomous.Core.Configuration;

/// <summary>
/// Azure general configuration options.
/// </summary>
public class AzureOptions
{
    public const string SectionName = "Azure";

    /// <summary>
    /// Whether to use Azure Managed Identity for authentication.
    /// </summary>
    public bool UseManagedIdentity { get; set; } = true;
}

/// <summary>
/// Azure App Configuration options.
/// </summary>
public class AzureAppConfigurationOptions
{
    public const string SectionName = "AzureAppConfiguration";

    /// <summary>
    /// Whether App Configuration is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// App Configuration endpoint URL.
    /// </summary>
    [Required]
    public string? Endpoint { get; set; }

    /// <summary>
    /// Refresh interval for dynamic configuration in minutes.
    /// </summary>
    public int RefreshIntervalMinutes { get; set; } = 5;
}

/// <summary>
/// Azure Key Vault configuration options.
/// </summary>
public class KeyVaultOptions
{
    public const string SectionName = "KeyVault";

    /// <summary>
    /// Whether Key Vault is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Key Vault URI.
    /// </summary>
    [Required]
    public string? VaultUri { get; set; }
}

/// <summary>
/// Azure Storage configuration options.
/// </summary>
public class StorageOptions
{
    public const string SectionName = "Storage";

    /// <summary>
    /// Storage account name.
    /// </summary>
    [Required]
    public string? AccountName { get; set; }

    /// <summary>
    /// Blob storage endpoint URL.
    /// </summary>
    [Required]
    public string? BlobEndpoint { get; set; }
}

/// <summary>
/// Application Insights configuration options.
/// </summary>
public class ApplicationInsightsOptions
{
    public const string SectionName = "ApplicationInsights";

    /// <summary>
    /// Application Insights connection string.
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Whether Application Insights is enabled.
    /// </summary>
    public bool Enabled => !string.IsNullOrWhiteSpace(ConnectionString);
}
