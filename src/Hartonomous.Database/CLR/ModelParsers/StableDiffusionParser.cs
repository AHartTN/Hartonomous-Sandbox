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
    /// Parses Stable Diffusion model checkpoints.
    /// Detects model variant (UNet, VAE, TextEncoder) and extracts architecture metadata.
    /// Stable Diffusion models are typically SafeTensors or PyTorch checkpoints.
    /// </summary>
    public class StableDiffusionParser : IModelFormatReader
    {
        public ModelFormat Format => ModelFormat.StableDiffusion;

        // Common Stable Diffusion component patterns
        private static readonly string[] UNET_PATTERNS = { "model.diffusion_model", "unet", "down_blocks", "up_blocks", "mid_block" };
        private static readonly string[] VAE_PATTERNS = { "first_stage_model", "vae", "encoder", "decoder", "quant_conv" };
        private static readonly string[] TEXT_ENCODER_PATTERNS = { "cond_stage_model", "text_model", "transformer.text_model", "embeddings" };

        private IModelFormatReader? underlyingParser;

        public bool ValidateFormat(Stream stream)
        {
            if (stream == null || !stream.CanRead || !stream.CanSeek)
                return false;

            // Stable Diffusion models use SafeTensors or PyTorch format
            // Detect underlying format first
            underlyingParser = DetectUnderlyingFormat(stream)!;
            
            if (underlyingParser == null)
                return false;

            // Check if tensor names match Stable Diffusion patterns
            try
            {
                var weights = underlyingParser.ReadWeights(stream);
                return IsStableDiffusionModel(weights);
            }
            catch
            {
                return false;
            }
        }

        public ModelMetadata ReadMetadata(Stream stream)
        {
            if (underlyingParser == null)
            {
                underlyingParser = DetectUnderlyingFormat(stream)!;
                if (underlyingParser == null)
                    throw new ArgumentException("Invalid Stable Diffusion format", nameof(stream));
            }

            var baseMetadata = underlyingParser.ReadMetadata(stream);
            
            var metadata = new ModelMetadata
            {
                Format = ModelFormat.StableDiffusion,
                Name = "Stable Diffusion Model",
                Architecture = DetectArchitecture(stream),
                LayerCount = baseMetadata.LayerCount,
                EmbeddingDimension = baseMetadata.EmbeddingDimension,
                ParameterCount = baseMetadata.ParameterCount
            };

            try
            {
                var weights = underlyingParser.ReadWeights(stream);
                
                // Detect model variant
                var variant = DetectModelVariant(weights);
                metadata.Architecture = $"Stable Diffusion {variant}";

                // Count specialized layer types
                int unetLayers = weights.Values.Count(t => t.LayerType == LayerType.UNetDown || 
                                                             t.LayerType == LayerType.UNetMid || 
                                                             t.LayerType == LayerType.UNetUp);
                int vaeLayers = weights.Values.Count(t => t.LayerType == LayerType.VAE);
                int attentionLayers = weights.Values.Count(t => t.LayerType == LayerType.Attention || 
                                                                  t.LayerType == LayerType.CrossAttention);

                // Update layer count with specialized counts
                metadata.LayerCount = weights.Count;

                // Infer embedding dimension from text encoder or UNet
                var embeddingTensor = weights.Values.FirstOrDefault(t => 
                    t.Name.Contains("embeddings", StringComparison.OrdinalIgnoreCase) ||
                    t.Name.Contains("text_model", StringComparison.OrdinalIgnoreCase));

                if (embeddingTensor.Shape != null && embeddingTensor.Shape.Length > 0)
                {
                    metadata.EmbeddingDimension = (int)embeddingTensor.Shape[embeddingTensor.Shape.Length - 1];
                }

                // Add variant-specific details
                metadata.Name = variant switch
                {
                    "UNet" => $"Stable Diffusion UNet ({unetLayers} layers, {attentionLayers} attention)",
                    "VAE" => $"Stable Diffusion VAE ({vaeLayers} layers)",
                    "TextEncoder" => $"Stable Diffusion Text Encoder (dim={metadata.EmbeddingDimension})",
                    "Full Pipeline" => $"Stable Diffusion Full ({unetLayers} UNet, {vaeLayers} VAE, {attentionLayers} attn)",
                    _ => "Stable Diffusion Model"
                };
            }
            catch (Exception ex)
            {
                metadata.Name = $"Stable Diffusion Model (parse error: {ex.Message})";
            }

            return metadata;
        }

        public Dictionary<string, TensorInfo> ReadWeights(Stream stream)
        {
            if (underlyingParser == null)
            {
                underlyingParser = DetectUnderlyingFormat(stream);
                if (underlyingParser == null)
                    throw new ArgumentException("Invalid Stable Diffusion format", nameof(stream));
            }

            var weights = underlyingParser.ReadWeights(stream);

            // Enhance TensorInfo with Stable Diffusion-specific layer types
            var enhancedWeights = new Dictionary<string, TensorInfo>();
            
            foreach (var kvp in weights)
            {
                var tensorInfo = kvp.Value;
                
                // Override LayerType with SD-specific detection
                tensorInfo.LayerType = InferStableDiffusionLayerType(tensorInfo.Name);
                
                enhancedWeights[kvp.Key] = tensorInfo;
            }

            return enhancedWeights;
        }

        #region Stable Diffusion Detection

        private IModelFormatReader? DetectUnderlyingFormat(Stream stream)
        {
            long originalPosition = stream.Position;
            
            try
            {
                // Try SafeTensors first (recommended for SD)
                stream.Position = 0;
                if (IsSafeTensorsFormat(stream))
                {
                    // Return PyTorchParser which can handle SafeTensors-like formats
                    // or we implement a wrapper here
                    underlyingParser = new PyTorchParser();
                    return underlyingParser;
                }

                // Try PyTorch format
                stream.Position = 0;
                var parser = new PyTorchParser();
                if (parser.ValidateFormat(stream))
                    return parser;

                return null;
            }
            finally
            {
                stream.Position = originalPosition;
            }
        }

        private bool IsSafeTensorsFormat(Stream stream)
        {
            try
            {
                if (stream.Length < 8)
                    return false;
                
                byte[] header = new byte[8];
                stream.Read(header, 0, 8);
                
                ulong headerLength = BitConverter.ToUInt64(header, 0);
                return headerLength > 0 && headerLength < 100_000_000;
            }
            catch
            {
                return false;
            }
        }

        private bool IsStableDiffusionModel(Dictionary<string, TensorInfo> weights)
        {
            if (weights == null || weights.Count == 0)
                return false;

            // Check for characteristic Stable Diffusion tensor name patterns
            var tensorNames = weights.Keys.ToList();
            
            int unetMatches = tensorNames.Count(name => UNET_PATTERNS.Any(pattern => 
                name.Contains(pattern, StringComparison.OrdinalIgnoreCase)));
            
            int vaeMatches = tensorNames.Count(name => VAE_PATTERNS.Any(pattern => 
                name.Contains(pattern, StringComparison.OrdinalIgnoreCase)));
            
            int textEncoderMatches = tensorNames.Count(name => TEXT_ENCODER_PATTERNS.Any(pattern => 
                name.Contains(pattern, StringComparison.OrdinalIgnoreCase)));

            // Model is SD if it has significant matches in any component
            return (unetMatches > 10) || (vaeMatches > 5) || (textEncoderMatches > 5);
        }

        private string DetectModelVariant(Dictionary<string, TensorInfo> weights)
        {
            var tensorNames = weights.Keys.ToList();
            
            int unetMatches = tensorNames.Count(name => UNET_PATTERNS.Any(pattern => 
                name.Contains(pattern, StringComparison.OrdinalIgnoreCase)));
            
            int vaeMatches = tensorNames.Count(name => VAE_PATTERNS.Any(pattern => 
                name.Contains(pattern, StringComparison.OrdinalIgnoreCase)));
            
            int textEncoderMatches = tensorNames.Count(name => TEXT_ENCODER_PATTERNS.Any(pattern => 
                name.Contains(pattern, StringComparison.OrdinalIgnoreCase)));

            // Determine primary component
            if (unetMatches > vaeMatches && unetMatches > textEncoderMatches)
            {
                if (vaeMatches > 0 || textEncoderMatches > 0)
                    return "Full Pipeline";
                return "UNet";
            }
            else if (vaeMatches > unetMatches && vaeMatches > textEncoderMatches)
            {
                return "VAE";
            }
            else if (textEncoderMatches > 0)
            {
                return "TextEncoder";
            }

            return "Unknown Variant";
        }

        private string DetectArchitecture(Stream stream)
        {
            // Common Stable Diffusion versions:
            // - SD 1.4/1.5: 860M parameters
            // - SD 2.0/2.1: 865M parameters
            // - SDXL: 3.5B parameters
            
            try
            {
                var metadata = underlyingParser?.ReadMetadata(stream);
                if (metadata != null && metadata.Value.ParameterCount > 0)
                {
                    long paramCount = metadata.Value.ParameterCount;
                    
                    if (paramCount > 3_000_000_000)
                        return "SDXL (3.5B)";
                    else if (paramCount > 800_000_000)
                        return "SD 2.x (865M)";
                    else if (paramCount > 500_000_000)
                        return "SD 1.x (860M)";
                }
            }
            catch { }

            return "Unknown";
        }

        private LayerType InferStableDiffusionLayerType(string tensorName)
        {
            string lowerName = tensorName.ToLowerInvariant();

            // UNet components
            if (lowerName.Contains("down_blocks") || lowerName.Contains("input_blocks"))
                return LayerType.UNetDown;
            if (lowerName.Contains("mid_block") || lowerName.Contains("middle_block"))
                return LayerType.UNetMid;
            if (lowerName.Contains("up_blocks") || lowerName.Contains("output_blocks"))
                return LayerType.UNetUp;

            // VAE
            if (lowerName.Contains("first_stage_model") || lowerName.Contains("vae") || 
                lowerName.Contains("encoder.") || lowerName.Contains("decoder."))
                return LayerType.VAE;

            // Attention mechanisms
            if (lowerName.Contains("attn2") || lowerName.Contains("cross_attn"))
                return LayerType.CrossAttention;
            if (lowerName.Contains("attn") || lowerName.Contains("self_attn"))
                return LayerType.Attention;

            // Text encoder
            if (lowerName.Contains("text_model") || lowerName.Contains("cond_stage_model"))
            {
                if (lowerName.Contains("embeddings"))
                    return LayerType.Embedding;
                if (lowerName.Contains("layer_norm") || lowerName.Contains("layernorm"))
                    return LayerType.LayerNorm;
                return LayerType.Dense; // Default for text encoder layers
            }

            // Common layer types
            if (lowerName.Contains("norm"))
                return LayerType.LayerNorm;
            if (lowerName.Contains("conv"))
                return LayerType.Convolution;
            if (lowerName.Contains("linear") || lowerName.Contains("dense"))
                return LayerType.Dense;
            if (lowerName.Contains("embed"))
                return LayerType.Embedding;

            return LayerType.Unknown;
        }

        #endregion
    }
}
