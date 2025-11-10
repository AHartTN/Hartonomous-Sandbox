using System.Collections.Generic;

namespace Hartonomous.Api.DTOs.Billing
{
    public class UsageReportResponse
    {
        public required List<UsageTypeSummary> UsageSummaries { get; set; }
    }
}
