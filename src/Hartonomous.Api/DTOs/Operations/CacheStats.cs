namespace Hartonomous.Api.DTOs.Operations;

public class CacheStats
{
    public long TotalEntries { get; set; }
    public long HitCount { get; set; }
    public long MissCount { get; set; }
    public double HitRate { get; set; }
    public long MemoryUsedMB { get; set; }
    public DateTime? OldestEntry { get; set; }
    public DateTime? NewestEntry { get; set; }
}
