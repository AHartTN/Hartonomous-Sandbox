namespace Hartonomous.Api.DTOs.Operations;

public class ConfigurationRequest
{
    public required string Key { get; set; }
    public string? Value { get; set; }
}
