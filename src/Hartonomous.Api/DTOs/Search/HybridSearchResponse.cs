using System.Collections.Generic;

namespace Hartonomous.Api.DTOs.Search
{
    public class HybridSearchResponse
    {
        public required List<SearchResult> Results { get; set; }
        public int SpatialCandidatesFound { get; set; }
        public int FinalResults { get; set; }
    }
}
