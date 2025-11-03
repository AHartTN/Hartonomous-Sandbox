namespace Hartonomous.Core.Serialization;

/// <summary>
/// Lightweight abstraction for JSON serialization operations used across transports.
/// </summary>
public interface IJsonSerializer
{
    string Serialize<T>(T value);

    T? Deserialize<T>(string json);
}
