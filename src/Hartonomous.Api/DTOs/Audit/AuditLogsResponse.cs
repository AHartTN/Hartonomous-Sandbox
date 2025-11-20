using System.Collections.Generic;

using Hartonomous.Api.DTOs.Common;

namespace Hartonomous.Api.DTOs.Audit;


public class AuditLogsResponse
{
    public List<AuditLog> Logs { get; set; } = new();
    public PaginationInfo Pagination { get; set; } = new();
    public bool DemoMode { get; set; }
}
