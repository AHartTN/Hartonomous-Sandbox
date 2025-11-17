namespace Hartonomous.Clr.Core
{
    /// <summary>
    /// Metadata describing a tensor's properties
    /// </summary>
    public class TensorMetadata
    {
        public string TensorName { get; set; }
        public string TensorShape { get; set; }
        public string DataType { get; set; }
        public long ElementCount { get; set; }
        public long ByteSize { get; set; }
    }
}
