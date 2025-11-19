using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hartonomous.Clr.Contracts;
using Hartonomous.Clr.Enums;
using Hartonomous.Clr.Models;

namespace Hartonomous.Clr.ModelParsers
{
    /// <summary>
    /// Parses PyTorch format models (.pt, .pth, .ckpt).
    /// Handles both ZIP-based (newer) and pickle-based (legacy) formats.
    /// Security: Avoids arbitrary code execution from pickle, recommends SafeTensors conversion.
    /// Note: ZIP archive parsing is limited in SQL CLR - recommend conversion to SafeTensors for full support.
    /// </summary>
    public class PyTorchParser : IModelFormatReader
    {
        public ModelFormat Format => ModelFormat.PyTorch;

        public bool ValidateFormat(Stream stream)
        {
            if (stream == null || !stream.CanRead || !stream.CanSeek)
                return false;

            long originalPosition = stream.Position;
            try
            {
                stream.Position = 0;
                
                // Check for ZIP magic (newer PyTorch format)
                byte[] zipMagic = new byte[4];
                if (stream.Read(zipMagic, 0, 4) == 4)
                {
                    if (zipMagic[0] == 0x50 && zipMagic[1] == 0x4B && 
                        zipMagic[2] == 0x03 && zipMagic[3] == 0x04) // "PK\x03\x04"
                        return true;
                }

                // Check for pickle magic (legacy format)
                stream.Position = 0;
                int firstByte = stream.ReadByte();
                if (firstByte == 0x80) // Pickle protocol marker
                    return true;

                return false;
            }
            catch
            {
                return false;
            }
            finally
            {
                stream.Position = originalPosition;
            }
        }

        public ModelMetadata ReadMetadata(Stream stream)
        {
            if (!ValidateFormat(stream))
                throw new ArgumentException("Invalid PyTorch format", nameof(stream));

            stream.Position = 0;

            var metadata = new ModelMetadata
            {
                Format = ModelFormat.PyTorch,
                Name = "PyTorch Model",
                Architecture = "Unknown",
                LayerCount = 0,
                EmbeddingDimension = 0,
                ParameterCount = 0
            };

            // PyTorch format (both pickle and ZIP) requires unsafe code execution for full parsing
            // SQL Server CLR has limited ZIP support and pickle is a security risk
            // Recommend conversion to SafeTensors
            metadata.Name = "PyTorch Model (recommend SafeTensors conversion for full support)";
            metadata.Architecture = IsZipFormat(stream) ? "ZIP-based" : "Pickle-based";

            return metadata;
        }

        public Dictionary<string, TensorInfo> ReadWeights(Stream stream)
        {
            if (!ValidateFormat(stream))
                throw new ArgumentException("Invalid PyTorch format", nameof(stream));

            // PyTorch parsing is limited in SQL CLR environment
            throw new NotSupportedException(
                "PyTorch format parsing is limited in SQL Server CLR due to:\n" +
                "1. Pickle format requires arbitrary code execution (security risk)\n" +
                "2. ZIP parsing requires System.IO.Compression.ZipArchive (not available in SQL CLR)\n\n" +
                "Please convert to SafeTensors format using:\n" +
                GetConversionCommand());
        }

        #region Helper Methods

        private bool IsZipFormat(Stream stream)
        {
            long originalPosition = stream.Position;
            stream.Position = 0;
            
            byte[] magic = new byte[4];
            bool isZip = stream.Read(magic, 0, 4) == 4 &&
                         magic[0] == 0x50 && magic[1] == 0x4B &&
                         magic[2] == 0x03 && magic[3] == 0x04;
            
            stream.Position = originalPosition;
            return isZip;
        }

        /// <summary>
        /// Recommends conversion command for safer SafeTensors format.
        /// </summary>
        public static string GetConversionCommand()
        {
            return @"# Convert PyTorch to SafeTensors (Python)
from safetensors.torch import save_file
import torch

model = torch.load('model.pth', map_location='cpu')
state_dict = model.state_dict() if hasattr(model, 'state_dict') else model
save_file(state_dict, 'model.safetensors')";
        }

        #endregion
    }
}
