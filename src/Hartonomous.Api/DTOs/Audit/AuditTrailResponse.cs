using System.Collections.Generic;

using Hartonomous.Api.DTOs.Provenance;

namespace Hartonomous.Api.DTOs.Audit;


public class AuditTrailResponse
{
    public string ResourceId { get; set; } = string.Empty;
    public string ResourceType { get; set; } = string.Empty;
    public List<AuditEvent> Timeline { get; set; } = new();
    public List<RelatedResource> RelatedResources { get; set; } = new();
    public TrailStatistics Statistics { get; set; } = new();
    public bool DemoMode { get; set; }
}
