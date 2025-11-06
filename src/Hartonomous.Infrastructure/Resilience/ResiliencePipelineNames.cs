namespace Hartonomous.Infrastructure.Resilience;

/// <summary>
/// Named resilience pipelines for different service types
/// </summary>
public static class ResiliencePipelineNames
{
    public const string Default = "default";
    public const string Inference = "inference";
    public const string Generation = "generation";
    public const string ExternalApi = "external-api";
    public const string Database = "database";
    public const string Cache = "cache";
    public const string EventBus = "event-bus";
}
