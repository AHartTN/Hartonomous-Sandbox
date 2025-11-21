using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace Hartonomous.Infrastructure.Services.SignalR;

/// <summary>
/// Real-time ingestion progress hub
/// PHASE 4: Real SignalR implementation
/// </summary>
public class IngestionHub : Hub
{
    /// <summary>
    /// Client subscribes to ingestion progress for a specific job
    /// </summary>
    public async Task SubscribeToJob(string jobId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"job_{jobId}");
    }

    /// <summary>
    /// Client unsubscribes from job progress
    /// </summary>
    public async Task UnsubscribeFromJob(string jobId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"job_{jobId}");
    }

    /// <summary>
    /// Broadcast progress update to all clients monitoring this job
    /// </summary>
    public static async Task BroadcastProgress(
        IHubContext<IngestionHub> hubContext,
        string jobId,
        int atomsProcessed,
        int totalAtoms,
        string status)
    {
        await hubContext.Clients.Group($"job_{jobId}").SendAsync("ProgressUpdate", new
        {
            JobId = jobId,
            AtomsProcessed = atomsProcessed,
            TotalAtoms = totalAtoms,
            Progress = totalAtoms > 0 ? (double)atomsProcessed / totalAtoms : 0,
            Status = status,
            Timestamp = System.DateTime.UtcNow
        });
    }

    /// <summary>
    /// Broadcast atom creation event (for real-time knowledge graph visualization)
    /// </summary>
    public static async Task BroadcastAtom(
        IHubContext<IngestionHub> hubContext,
        long atomId,
        string modality,
        string canonicalText)
    {
        await hubContext.Clients.All.SendAsync("AtomCreated", new
        {
            AtomId = atomId,
            Modality = modality,
            CanonicalText = canonicalText,
            Timestamp = System.DateTime.UtcNow
        });
    }
}
