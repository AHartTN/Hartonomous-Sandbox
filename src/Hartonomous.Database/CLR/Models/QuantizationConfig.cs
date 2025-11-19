using Hartonomous.Clr.Enums;

namespace Hartonomous.Clr.Models
{
    /// <summary>
    /// Configuration for quantization/dequantization operations.
    /// </summary>
    public struct QuantizationConfig
    {
        /// <summary>
        /// Target quantization type
        /// </summary>
        public QuantizationType Type { get; set; }

        /// <summary>
        /// Block size for block-based quantization (e.g., 32 for Q4_0)
        /// </summary>
        public int BlockSize { get; set; }

        /// <summary>
        /// Whether to use importance-based quantization
        /// </summary>
        public bool UseImportance { get; set; }

        /// <summary>
        /// Calibration dataset size for importance estimation
        /// </summary>
        public int CalibrationSamples { get; set; }

        public QuantizationConfig(QuantizationType type, int blockSize = 32)
        {
            Type = type;
            BlockSize = blockSize;
            UseImportance = false;
            CalibrationSamples = 0;
        }
    }
}
