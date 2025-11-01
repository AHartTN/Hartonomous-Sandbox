using Hartonomous.Admin.Operations;

namespace Hartonomous.Admin.Models;

public sealed record AdminDashboardSnapshot(
    int TotalModels,
    long TotalParameters,
    long TotalLayers,
    IReadOnlyDictionary<string, int> ArchitectureBreakdown,
    IReadOnlyList<AdminOperationStatus> RecentOperations,
    DateTimeOffset CapturedAt)
{
    public static AdminDashboardSnapshot Empty { get; } = new(
        0,
        0,
        0,
        new Dictionary<string, int>(),
        Array.Empty<AdminOperationStatus>(),
        DateTimeOffset.MinValue);
}
