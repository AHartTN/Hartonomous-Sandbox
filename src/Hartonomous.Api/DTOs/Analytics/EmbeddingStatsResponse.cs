using System.Collections.Generic;

namespace Hartonomous.Api.DTOs.Analytics
{
    public class EmbeddingStatsResponse
    {
        public required List<EmbeddingTypeStat> Stats { get; set; }
        public EmbeddingOverallStats Overall { get; set; } = new();
    }
}
