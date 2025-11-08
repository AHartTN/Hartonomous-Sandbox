namespace Hartonomous.Core.Interfaces.Events;

public class CloudEvent
{
    public string Id { get; set; } = string.Empty;
    public Uri Source { get; set; } = null!;
    public string Type { get; set; } = string.Empty;
    public DateTimeOffset Time { get; set; }
    public string? Subject { get; set; }
    public Uri? DataSchema { get; set; }
    public object? Data { get; set; }
    public Dictionary<string, object> Extensions { get; set; } = new();
}
