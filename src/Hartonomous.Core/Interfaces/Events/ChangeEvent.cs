namespace Hartonomous.Core.Interfaces.Events;

public class ChangeEvent
{
    public string Lsn { get; set; } = string.Empty;
    public int Operation { get; set; }
    public string TableName { get; set; } = string.Empty;
    public Dictionary<string, object> Data { get; set; } = new();
}

/// <summary>
/// CloudEvent representation following CNCF CloudEvents specification
/// </summary>
