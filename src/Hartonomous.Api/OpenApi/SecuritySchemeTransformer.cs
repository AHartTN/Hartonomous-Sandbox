using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Hartonomous.Api.OpenApi;

/// <summary>
/// Transforms the OpenAPI document to add OAuth2 security scheme for Azure Entra ID.
/// </summary>
internal sealed class SecuritySchemeTransformer : IOpenApiDocumentTransformer
{
    private readonly IConfiguration _configuration;

    public SecuritySchemeTransformer(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        var tenantId = _configuration["AzureAd:TenantId"];
        var clientId = _configuration["AzureAd:ClientId"];
        var instance = _configuration["AzureAd:Instance"] ?? "https://login.microsoftonline.com";

        if (string.IsNullOrEmpty(tenantId) || string.IsNullOrEmpty(clientId))
        {
            // Skip security scheme if Entra ID is not configured
            return Task.CompletedTask;
        }

        if (document.Components == null)
        {
            document.Components = new OpenApiComponents();
        }

        if (document.Components.SecuritySchemes == null)
        {
            document.Components.SecuritySchemes = new Dictionary<string, IOpenApiSecurityScheme>();
        }
        
        document.Components.SecuritySchemes["oauth2"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.OAuth2,
            Description = "Azure Entra ID OAuth2 authentication. Requires JWT Bearer token.",
            Flows = new OpenApiOAuthFlows
            {
                AuthorizationCode = new OpenApiOAuthFlow
                {
                    AuthorizationUrl = new Uri($"{instance}/{tenantId}/oauth2/v2.0/authorize"),
                    TokenUrl = new Uri($"{instance}/{tenantId}/oauth2/v2.0/token"),
                    Scopes = new Dictionary<string, string>
                    {
                        { $"api://{clientId}/user_impersonation", "Access Hartonomous API as authenticated user" },
                        { $"api://{clientId}/DataIngestion.Read", "Read ingestion data" },
                        { $"api://{clientId}/DataIngestion.Write", "Write ingestion data" },
                        { "openid", "Sign in and read user profile" },
                        { "profile", "Read user profile" },
                        { "email", "Read user email address" }
                    }
                }
            }
        };

        // Apply security requirement globally to all operations
        var securitySchemeRef = new OpenApiSecuritySchemeReference("oauth2", document, null);
        var securityRequirement = new OpenApiSecurityRequirement
        {
            { securitySchemeRef, new List<string> { $"api://{clientId}/user_impersonation" } }
        };

        // Add to document level (applies to all operations by default)
        document.Security = new List<OpenApiSecurityRequirement> { securityRequirement };

        return Task.CompletedTask;
    }
}
