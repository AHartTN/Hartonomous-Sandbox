using System.Collections.Generic;

namespace Hartonomous.Api.DTOs.Search
{
    public class CrossModalSearchResponse
    {
        public required List<SearchResult> Results { get; set; }
        public required string QueryModality { get; set; }
        public required List<string> TargetModalities { get; set; }
    }
}
