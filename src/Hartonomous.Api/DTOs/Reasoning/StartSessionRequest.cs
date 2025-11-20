using Hartonomous.Infrastructure.Services;

namespace Hartonomous.Api.DTOs.Reasoning;


public class StartSessionRequest
{
    public required StreamType StreamType { get; set; }
}
