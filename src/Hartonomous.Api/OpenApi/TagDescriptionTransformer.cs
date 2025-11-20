using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Hartonomous.Api.OpenApi;

/// <summary>
/// Transforms the OpenAPI document to add tag descriptions for API categorization.
/// </summary>
internal sealed class TagDescriptionTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        document.Tags = new HashSet<OpenApiTag>
        {
            new()
            {
                Name = "Ingestion",
                Description = "Multi-modal data ingestion endpoints for atomization. Supports text, images, video, code, and AI models.",
                ExternalDocs = new OpenApiExternalDocs
                {
                    Description = "Learn more about ingestion",
                    Url = new Uri("https://docs.hartonomous.ai/ingestion")
                }
            },
            new OpenApiTag
            {
                Name = "Provenance",
                Description = "Provenance tracking and lineage graph queries. Full spatial provenance with Neo4j integration.",
                ExternalDocs = new OpenApiExternalDocs
                {
                    Description = "Learn more about provenance",
                    Url = new Uri("https://docs.hartonomous.ai/provenance")
                }
            },
            new OpenApiTag
            {
                Name = "Reasoning",
                Description = "AI reasoning and inference operations over atomized knowledge graphs.",
                ExternalDocs = new OpenApiExternalDocs
                {
                    Description = "Learn more about reasoning",
                    Url = new Uri("https://docs.hartonomous.ai/reasoning")
                }
            },
            new OpenApiTag
            {
                Name = "ModelIngestion",
                Description = "AI model atomization supporting PyTorch, ONNX, GGUF, and SafeTensors formats. Enables tensor-level provenance tracking.",
                ExternalDocs = new OpenApiExternalDocs
                {
                    Description = "Learn more about model ingestion",
                    Url = new Uri("https://docs.hartonomous.ai/models")
                }
            },
            new OpenApiTag
            {
                Name = "Streaming",
                Description = "Real-time progress updates and event streaming via SignalR hubs.",
                ExternalDocs = new OpenApiExternalDocs
                {
                    Description = "Learn more about streaming",
                    Url = new Uri("https://docs.hartonomous.ai/streaming")
                }
            },
            new OpenApiTag
            {
                Name = "Health",
                Description = "Health check, liveness, and readiness probe endpoints for Kubernetes/container orchestration.",
                ExternalDocs = new OpenApiExternalDocs
                {
                    Description = "Learn more about health checks",
                    Url = new Uri("https://docs.hartonomous.ai/health")
                }
            }
        };

        return Task.CompletedTask;
    }
}
