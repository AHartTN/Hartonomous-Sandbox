using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Hartonomous.Api.OpenApi;

/// <summary>
/// Transforms the OpenAPI document to add server configurations for different environments.
/// </summary>
internal sealed class ServerConfigurationTransformer : IOpenApiDocumentTransformer
{
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;

    public ServerConfigurationTransformer(IConfiguration configuration, IWebHostEnvironment environment)
    {
        _configuration = configuration;
        _environment = environment;
    }

    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        var servers = new List<OpenApiServer>();

        // Production server (if configured)
        var productionUrl = _configuration["ApiBaseUrl"];
        if (!string.IsNullOrEmpty(productionUrl))
        {
            servers.Add(new OpenApiServer
            {
                Url = productionUrl,
                Description = "Production API Server"
            });
        }

        // Staging server (if configured)
        var stagingUrl = _configuration["ApiStagingUrl"];
        if (!string.IsNullOrEmpty(stagingUrl))
        {
            servers.Add(new OpenApiServer
            {
                Url = stagingUrl,
                Description = "Staging API Server"
            });
        }

        // Local development server
        if (_environment.IsDevelopment())
        {
            servers.Add(new OpenApiServer
            {
                Url = "https://localhost:5001",
                Description = "Local Development Server"
            });
        }

        // Default server if none configured
        if (servers.Count == 0)
        {
            servers.Add(new OpenApiServer
            {
                Url = "/",
                Description = "Current Server"
            });
        }

        document.Servers = servers;

        return Task.CompletedTask;
    }
}
