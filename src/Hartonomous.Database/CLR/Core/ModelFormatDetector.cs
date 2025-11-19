using System;
using System.IO;
using Hartonomous.Clr.Enums;

namespace Hartonomous.Clr.Core
{
    /// <summary>
    /// Detects model file formats using magic numbers and file headers.
    /// Enables automatic format detection for multi-format model parsers.
    /// </summary>
    public static class ModelFormatDetector
    {
        // Magic numbers for different formats
        private static readonly byte[] GGUF_MAGIC = { 0x47, 0x47, 0x55, 0x46 }; // "GGUF"
        private static readonly byte[] ONNX_MAGIC = { 0x08 }; // Protobuf tag for IR version
        private static readonly byte[] ZIP_MAGIC = { 0x50, 0x4B, 0x03, 0x04 }; // "PK\x03\x04" (ZIP format, used by PyTorch)
        private static readonly byte[] PICKLE_MAGIC = { 0x80 }; // Pickle protocol marker

        /// <summary>
        /// Detects model format from stream by reading file header.
        /// Stream position is reset after detection.
        /// </summary>
        /// <param name="stream">Model file stream</param>
        /// <returns>Detected model format</returns>
        public static ModelFormat DetectFormat(Stream stream)
        {
            if (stream == null || !stream.CanRead || !stream.CanSeek)
                return ModelFormat.Unknown;

            long originalPosition = stream.Position;
            
            try
            {
                // Read first 8 bytes for magic number detection
                byte[] header = new byte[8];
                int bytesRead = stream.Read(header, 0, 8);
                
                if (bytesRead < 4)
                    return ModelFormat.Unknown;

                // Check GGUF magic (0x46554747 = "GGUF" little-endian)
                if (MatchesMagic(header, GGUF_MAGIC))
                    return ModelFormat.GGUF;

                // Check SafeTensors (8-byte header length followed by JSON)
                if (bytesRead >= 8)
                {
                    ulong headerLength = BitConverter.ToUInt64(header, 0);
                    if (headerLength > 0 && headerLength < 100_000_000) // Reasonable JSON size
                    {
                        // Try to read JSON to verify it's SafeTensors
                        if (IsSafeTensorsFormat(stream, (int)headerLength))
                            return ModelFormat.SafeTensors;
                    }
                }

                // Check ONNX (protobuf format)
                if (header[0] == ONNX_MAGIC[0])
                {
                    // Additional validation: check for common ONNX field tags
                    if (IsOnnxFormat(stream))
                        return ModelFormat.ONNX;
                }

                // Check ZIP format (PyTorch, Stable Diffusion)
                if (MatchesMagic(header, ZIP_MAGIC))
                {
                    return DetectZipBasedFormat(stream);
                }

                // Check Pickle format (older PyTorch)
                if (header[0] == PICKLE_MAGIC[0])
                {
                    return ModelFormat.PyTorch;
                }

                // Check TensorFlow SavedModel (directory structure, not single file)
                // This would require filesystem access, not stream-based

                return ModelFormat.Unknown;
            }
            catch
            {
                return ModelFormat.Unknown;
            }
            finally
            {
                // Reset stream position
                stream.Position = originalPosition;
            }
        }

        /// <summary>
        /// Detects format from file path extension as fallback.
        /// </summary>
        public static ModelFormat DetectFormatFromPath(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return ModelFormat.Unknown;

            string extension = Path.GetExtension(filePath).ToLowerInvariant();
            
            return extension switch
            {
                ".gguf" => ModelFormat.GGUF,
                ".safetensors" => ModelFormat.SafeTensors,
                ".onnx" => ModelFormat.ONNX,
                ".pth" or ".pt" or ".ckpt" => ModelFormat.PyTorch,
                ".pb" => ModelFormat.TensorFlow,
                _ => ModelFormat.Unknown
            };
        }

        /// <summary>
        /// Combines stream-based and path-based detection.
        /// </summary>
        public static ModelFormat DetectFormat(Stream stream, string filePath)
        {
            // Try stream-based detection first (most reliable)
            var format = DetectFormat(stream);
            if (format != ModelFormat.Unknown)
                return format;

            // Fall back to path extension
            return DetectFormatFromPath(filePath);
        }

        #region Helper Methods

        private static bool MatchesMagic(byte[] data, byte[] magic)
        {
            if (data.Length < magic.Length)
                return false;

            for (int i = 0; i < magic.Length; i++)
            {
                if (data[i] != magic[i])
                    return false;
            }

            return true;
        }

        private static bool IsSafeTensorsFormat(Stream stream, int headerLength)
        {
            try
            {
                long originalPos = stream.Position;
                stream.Position = 8; // Skip the 8-byte header length

                byte[] jsonBytes = new byte[Math.Min(headerLength, 1024)]; // Read up to 1KB
                int read = stream.Read(jsonBytes, 0, jsonBytes.Length);
                
                stream.Position = originalPos;

                if (read == 0)
                    return false;

                // Check for JSON structure with common SafeTensors keys
                string jsonStart = System.Text.Encoding.UTF8.GetString(jsonBytes, 0, read);
                return jsonStart.Contains("\"") && 
                       (jsonStart.Contains("dtype") || jsonStart.Contains("shape") || jsonStart.Contains("data_offsets"));
            }
            catch
            {
                return false;
            }
        }

        private static bool IsOnnxFormat(Stream stream)
        {
            try
            {
                long originalPos = stream.Position;
                stream.Position = 0;

                // Read first 100 bytes to check for ONNX protobuf structure
                byte[] buffer = new byte[100];
                int read = stream.Read(buffer, 0, buffer.Length);
                
                stream.Position = originalPos;

                if (read < 10)
                    return false;

                // Look for common ONNX field tags (field 1 = ir_version, field 7 = graph)
                for (int i = 0; i < read - 1; i++)
                {
                    // Protobuf field tag format: (field_number << 3) | wire_type
                    // Field 1 (ir_version) varint: 0x08
                    // Field 7 (graph) length-delimited: 0x3A
                    if (buffer[i] == 0x08 || buffer[i] == 0x3A)
                        return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        private static ModelFormat DetectZipBasedFormat(Stream stream)
        {
            try
            {
                long originalPos = stream.Position;
                stream.Position = 0;

                // PyTorch and Stable Diffusion models are ZIP files
                // Differentiate by looking for specific files in the archive
                
                // This is simplified - full implementation would use ZipArchive
                // to inspect the file list:
                // - PyTorch: contains "data.pkl", "version", etc.
                // - Stable Diffusion: contains "model_index.json", multiple .safetensors

                // For now, assume PyTorch (most common ZIP-based model format)
                stream.Position = originalPos;
                return ModelFormat.PyTorch;
            }
            catch
            {
                return ModelFormat.Unknown;
            }
        }

        #endregion
    }
}
