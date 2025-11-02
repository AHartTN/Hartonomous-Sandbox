namespace ModelIngestion.Content;

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
