namespace Hartonomous.Api.DTOs.Operations;

/// <summary>
/// Tenant-specific metrics
/// </summary>
public class TenantMetricsResponse
{
    public int TenantId { get; set; }
    public long QuotaUsed { get; set; }
    public long QuotaLimit { get; set; }
    public decimal CostAccrued { get; set; }
    public int InferenceCount { get; set; }
    public DateTime? LastInferenceUtc { get; set; }
    public double AvgInferenceMs { get; set; }
    public DateTime MeasuredAt { get; set; }
}
