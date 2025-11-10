namespace Hartonomous.Api.DTOs.Analytics
{
    public class EmbeddingOverallStats
    {
        public long TotalEmbeddings { get; set; }
        public long UniqueAtoms { get; set; }
        public int DistinctEmbeddingTypes { get; set; }
        public int DistinctModels { get; set; }
    }
}
