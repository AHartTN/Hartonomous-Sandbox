using Hartonomous.Admin.Models;

namespace Hartonomous.Admin.Services;

public sealed class AdminTelemetryCache
{
    private readonly object _gate = new();
    private AdminDashboardSnapshot _snapshot = AdminDashboardSnapshot.Empty;

    public event EventHandler<AdminDashboardSnapshot>? SnapshotUpdated;

    public AdminDashboardSnapshot Snapshot
    {
        get
        {
            lock (_gate)
            {
                return _snapshot;
            }
        }
    }

    public void SetSnapshot(AdminDashboardSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        lock (_gate)
        {
            _snapshot = snapshot;
        }

        SnapshotUpdated?.Invoke(this, snapshot);
    }
}
