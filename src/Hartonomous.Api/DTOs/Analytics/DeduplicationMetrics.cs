namespace Hartonomous.Api.DTOs.Analytics
{
    public class DeduplicationMetrics
    {
        public long TotalAtomReferences { get; set; }
        public long UniqueAtoms { get; set; }
        public double SpaceSavingsPercent { get; set; }
        public long EstimatedBytesSaved { get; set; }
    }
}
