namespace Hartonomous.Clr.Models
{
    /// <summary>
    /// Tensor shape with convenience methods.
    /// </summary>
    public struct TensorShape
    {
        public long[] Dimensions { get; set; }

        public TensorShape(params long[] dimensions)
        {
            Dimensions = dimensions;
        }

        public int Rank => Dimensions?.Length ?? 0;

        public long ElementCount
        {
            get
            {
                if (Dimensions == null || Dimensions.Length == 0)
                    return 0;
                long count = 1;
                foreach (var dim in Dimensions)
                    count *= dim;
                return count;
            }
        }

        public override string ToString()
        {
            return Dimensions == null ? "[]" : $"[{string.Join(", ", Dimensions)}]";
        }
    }
}
