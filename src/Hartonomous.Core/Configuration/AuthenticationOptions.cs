using System.ComponentModel.DataAnnotations;

namespace Hartonomous.Core.Configuration;

/// <summary>
/// Authentication configuration options.
/// </summary>
public class AuthenticationOptions
{
    public const string SectionName = "Authentication";

    /// <summary>
    /// Whether authentication is disabled (development only).
    /// </summary>
    public bool DisableAuth { get; set; } = false;
}

/// <summary>
/// Azure AD authentication configuration.
/// </summary>
public class AzureAdOptions
{
    public const string SectionName = "AzureAd";

    /// <summary>
    /// Azure AD instance URL.
    /// </summary>
    [Required]
    public string Instance { get; set; } = "https://login.microsoftonline.com/";

    /// <summary>
    /// Azure AD domain.
    /// </summary>
    [Required]
    public string? Domain { get; set; }

    /// <summary>
    /// Azure AD tenant ID.
    /// </summary>
    [Required]
    public string? TenantId { get; set; }

    /// <summary>
    /// Azure AD client/application ID.
    /// </summary>
    [Required]
    public string? ClientId { get; set; }

    /// <summary>
    /// API audience.
    /// </summary>
    public string? Audience { get; set; }

    /// <summary>
    /// OAuth scopes.
    /// </summary>
    public string? Scopes { get; set; }
}

/// <summary>
/// External ID (CIAM) authentication configuration.
/// </summary>
public class ExternalIdOptions
{
    public const string SectionName = "ExternalId";

    /// <summary>
    /// External ID instance URL.
    /// </summary>
    [Required]
    public string Instance { get; set; } = "https://hartonomous.ciamlogin.com/";

    /// <summary>
    /// External ID domain.
    /// </summary>
    [Required]
    public string? Domain { get; set; }

    /// <summary>
    /// External ID tenant ID.
    /// </summary>
    [Required]
    public string? TenantId { get; set; }

    /// <summary>
    /// External ID client/application ID.
    /// </summary>
    [Required]
    public string? ClientId { get; set; }

    /// <summary>
    /// API audience.
    /// </summary>
    public string? Audience { get; set; }

    /// <summary>
    /// OAuth scopes.
    /// </summary>
    public string? Scopes { get; set; }
}
