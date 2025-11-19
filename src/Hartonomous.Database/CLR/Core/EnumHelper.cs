using System;
using Hartonomous.Clr.Enums;

namespace Hartonomous.Clr.Core
{
    /// <summary>
    /// Helper methods for converting between enums and strings at SQL boundary.
    /// SQL Server stores int values, C# CLR code uses strongly-typed enums.
    /// Only convert to/from strings when interfacing with legacy string-based code.
    /// </summary>
    public static class EnumHelper
    {
        #region ModelFormat Conversions

        public static string ModelFormatToString(ModelFormat format)
        {
            return format switch
            {
                ModelFormat.GGUF => "gguf",
                ModelFormat.SafeTensors => "safetensors",
                ModelFormat.ONNX => "onnx",
                ModelFormat.PyTorch => "pytorch",
                ModelFormat.TensorFlow => "tensorflow",
                ModelFormat.StableDiffusion => "stablediffusion",
                _ => "unknown"
            };
        }

        public static ModelFormat StringToModelFormat(string format)
        {
            return format?.ToLowerInvariant() switch
            {
                "gguf" => ModelFormat.GGUF,
                "safetensors" => ModelFormat.SafeTensors,
                "onnx" => ModelFormat.ONNX,
                "pytorch" or "pth" or "pt" => ModelFormat.PyTorch,
                "tensorflow" or "tf" or "pb" => ModelFormat.TensorFlow,
                "stablediffusion" or "sd" or "diffusion" => ModelFormat.StableDiffusion,
                _ => ModelFormat.Unknown
            };
        }

        #endregion

        #region LayerType Conversions

        public static string LayerTypeToString(LayerType type)
        {
            return type switch
            {
                LayerType.Dense => "dense",
                LayerType.Embedding => "embedding",
                LayerType.LayerNorm => "layernorm",
                LayerType.Dropout => "dropout",
                LayerType.Attention => "attention",
                LayerType.MultiHeadAttention => "multiheadattention",
                LayerType.FeedForward => "feedforward",
                LayerType.Residual => "residual",
                LayerType.Convolution => "convolution",
                LayerType.Pooling => "pooling",
                LayerType.BatchNorm => "batchnorm",
                LayerType.UNetDown => "unetdown",
                LayerType.UNetMid => "unetmid",
                LayerType.UNetUp => "unetup",
                LayerType.VAE => "vae",
                LayerType.RNN => "rnn",
                LayerType.LSTM => "lstm",
                LayerType.GRU => "gru",
                _ => "unknown"
            };
        }

        public static LayerType StringToLayerType(string type)
        {
            return type?.ToLowerInvariant() switch
            {
                "dense" or "linear" or "fc" => LayerType.Dense,
                "embedding" or "embed" => LayerType.Embedding,
                "layernorm" or "layer_norm" or "norm" => LayerType.LayerNorm,
                "dropout" => LayerType.Dropout,
                "attention" or "attn" => LayerType.Attention,
                "multiheadattention" or "mha" => LayerType.MultiHeadAttention,
                "feedforward" or "ffn" or "mlp" => LayerType.FeedForward,
                "residual" or "skip" => LayerType.Residual,
                "convolution" or "conv" => LayerType.Convolution,
                "pooling" or "pool" => LayerType.Pooling,
                "batchnorm" or "batch_norm" or "bn" => LayerType.BatchNorm,
                "unetdown" or "down" => LayerType.UNetDown,
                "unetmid" or "mid" => LayerType.UNetMid,
                "unetup" or "up" => LayerType.UNetUp,
                "vae" => LayerType.VAE,
                "rnn" => LayerType.RNN,
                "lstm" => LayerType.LSTM,
                "gru" => LayerType.GRU,
                _ => LayerType.Unknown
            };
        }

        #endregion

        #region ActivationFunction Conversions

        public static string ActivationToString(ActivationFunction activation)
        {
            return activation switch
            {
                ActivationFunction.ReLU => "relu",
                ActivationFunction.GELU => "gelu",
                ActivationFunction.Tanh => "tanh",
                ActivationFunction.Sigmoid => "sigmoid",
                ActivationFunction.Swish => "swish",
                ActivationFunction.Mish => "mish",
                ActivationFunction.SiLU => "silu",
                ActivationFunction.Softmax => "softmax",
                ActivationFunction.LeakyReLU => "leakyrelu",
                ActivationFunction.ELU => "elu",
                ActivationFunction.SELU => "selu",
                _ => "none"
            };
        }

        public static ActivationFunction StringToActivation(string activation)
        {
            return activation?.ToLowerInvariant() switch
            {
                "relu" => ActivationFunction.ReLU,
                "gelu" => ActivationFunction.GELU,
                "tanh" => ActivationFunction.Tanh,
                "sigmoid" => ActivationFunction.Sigmoid,
                "swish" => ActivationFunction.Swish,
                "mish" => ActivationFunction.Mish,
                "silu" => ActivationFunction.SiLU,
                "softmax" => ActivationFunction.Softmax,
                "leakyrelu" or "leaky_relu" => ActivationFunction.LeakyReLU,
                "elu" => ActivationFunction.ELU,
                "selu" => ActivationFunction.SELU,
                _ => ActivationFunction.None
            };
        }

        #endregion

        #region HypothesisType Conversions

        public static string HypothesisTypeToString(HypothesisType type)
        {
            return type switch
            {
                HypothesisType.IndexOptimization => "IndexOptimization",
                HypothesisType.QueryRegression => "QueryRegression",
                HypothesisType.CacheWarming => "CacheWarming",
                HypothesisType.ConceptDiscovery => "ConceptDiscovery",
                HypothesisType.PruneModel => "PruneModel",
                HypothesisType.RefactorCode => "RefactorCode",
                HypothesisType.FixUX => "FixUX",
                _ => "Unknown"
            };
        }

        public static HypothesisType StringToHypothesisType(string type)
        {
            return type switch
            {
                "IndexOptimization" => HypothesisType.IndexOptimization,
                "QueryRegression" => HypothesisType.QueryRegression,
                "CacheWarming" => HypothesisType.CacheWarming,
                "ConceptDiscovery" => HypothesisType.ConceptDiscovery,
                "PruneModel" => HypothesisType.PruneModel,
                "RefactorCode" => HypothesisType.RefactorCode,
                "FixUX" => HypothesisType.FixUX,
                _ => HypothesisType.Unknown
            };
        }

        #endregion
    }
}
