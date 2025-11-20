using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Hartonomous.Api.OpenApi;

/// <summary>
/// Transforms the OpenAPI document to add comprehensive API information and metadata.
/// </summary>
internal sealed class ApiInfoTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        document.Info = new OpenApiInfo
        {
            Title = "Hartonomous API",
            Version = "v1",
            Description = """
                # Hartonomous - Atomic Intelligence Platform
                
                Enterprise-grade API for atomization, provenance tracking, and AI model ingestion.
                
                ## Features
                - **Multi-modal Atomization**: Text, images, video, code, AI models
                - **Spatial Provenance**: Full lineage tracking with Neo4j graph database
                - **Tensor Atomization**: PyTorch, ONNX, GGUF model decomposition
                - **Real-time Streaming**: SignalR-based progress updates
                - **Enterprise Security**: Entra ID authentication, rate limiting, CORS
                
                ## Authentication
                All endpoints require JWT Bearer token from Azure Entra ID.
                Include `Authorization: Bearer <token>` header in requests.
                
                Premium subscribers and developers have access to this interactive documentation.
                
                ## Rate Limiting
                - **Global**: 100 requests/minute per user
                - **Ingestion**: 20 requests/minute per user
                - **Query**: 50 requests/minute per user
                
                ## Correlation IDs
                All responses include `X-Correlation-ID` and `X-Request-ID` headers for distributed tracing.
                
                ## Support
                For API access, premium subscriptions, or technical support:
                - Email: support@hartonomous.ai
                - Documentation: https://docs.hartonomous.ai
                """,
            Contact = new OpenApiContact
            {
                Name = "Hartonomous Team",
                Email = "support@hartonomous.ai",
                Url = new Uri("https://hartonomous.ai/contact")
            },
            License = new OpenApiLicense
            {
                Name = "Proprietary",
                Url = new Uri("https://hartonomous.ai/license")
            },
            TermsOfService = new Uri("https://hartonomous.ai/terms")
        };

        return Task.CompletedTask;
    }
}
