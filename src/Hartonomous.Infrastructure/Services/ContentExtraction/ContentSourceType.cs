namespace Hartonomous.Infrastructure.Services.ContentExtraction;

/// <summary>
/// Identifies the upstream source for universal content ingestion.
/// </summary>
public enum ContentSourceType
{
    File,
    Http,
    Stream,
    Telemetry
}
