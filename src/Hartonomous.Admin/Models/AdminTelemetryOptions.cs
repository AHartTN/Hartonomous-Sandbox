namespace Hartonomous.Admin.Models;

public sealed class AdminTelemetryOptions
{
    public const string SectionName = "AdminTelemetry";

    public int PollIntervalSeconds { get; set; } = 5;
}
