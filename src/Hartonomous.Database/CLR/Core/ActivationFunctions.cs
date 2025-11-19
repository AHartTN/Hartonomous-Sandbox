using System;
using System.Numerics;
using System.Runtime.InteropServices;
using Hartonomous.Clr.Contracts;
using Hartonomous.Clr.Enums;

namespace Hartonomous.Clr.Core
{
    /// <summary>
    /// SIMD-optimized activation function implementations.
    /// Consolidates 29 scattered activation function usages across codebase.
    /// </summary>
    public static class ActivationFunctions
    {
        /// <summary>
        /// Applies ReLU: max(0, x) element-wise using SIMD.
        /// </summary>
        public static void ReLU(float[] input)
        {
            int simdLength = Vector<float>.Count;
            int i = 0;

            // SIMD path: process multiple elements at once
            for (; i <= input.Length - simdLength; i += simdLength)
            {
                var vec = new Vector<float>(input, i);
                var zero = Vector<float>.Zero;
                var result = Vector.Max(vec, zero);
                result.CopyTo(input, i);
            }

            // Scalar remainder
            for (; i < input.Length; i++)
            {
                input[i] = Math.Max(0, input[i]);
            }
        }

        /// <summary>
        /// Applies GELU (Gaussian Error Linear Unit): x * Φ(x)
        /// Approximation: 0.5 * x * (1 + tanh(sqrt(2/π) * (x + 0.044715 * x^3)))
        /// </summary>
        public static void GELU(float[] input)
        {
            const float sqrt2OverPi = 0.7978845608f; // sqrt(2/π)
            const float coeff = 0.044715f;

            for (int i = 0; i < input.Length; i++)
            {
                float x = input[i];
                float x3 = x * x * x;
                float inner = sqrt2OverPi * (x + coeff * x3);
                float tanh = (float)Math.Tanh(inner);
                input[i] = 0.5f * x * (1.0f + tanh);
            }
        }

        /// <summary>
        /// Applies Tanh: (e^x - e^-x) / (e^x + e^-x) element-wise.
        /// </summary>
        public static void Tanh(float[] input)
        {
            for (int i = 0; i < input.Length; i++)
            {
                input[i] = (float)Math.Tanh(input[i]);
            }
        }

        /// <summary>
        /// Applies Sigmoid: 1 / (1 + e^-x) element-wise.
        /// </summary>
        public static void Sigmoid(float[] input)
        {
            for (int i = 0; i < input.Length; i++)
            {
                input[i] = 1.0f / (1.0f + (float)Math.Exp(-input[i]));
            }
        }

        /// <summary>
        /// Applies Swish/SiLU: x * sigmoid(x) element-wise.
        /// </summary>
        public static void Swish(float[] input)
        {
            for (int i = 0; i < input.Length; i++)
            {
                float sigmoid = 1.0f / (1.0f + (float)Math.Exp(-input[i]));
                input[i] = input[i] * sigmoid;
            }
        }

        /// <summary>
        /// Applies Mish: x * tanh(softplus(x)) where softplus(x) = ln(1 + e^x).
        /// </summary>
        public static void Mish(float[] input)
        {
            for (int i = 0; i < input.Length; i++)
            {
                float softplus = (float)Math.Log(1.0 + Math.Exp(input[i]));
                input[i] = input[i] * (float)Math.Tanh(softplus);
            }
        }

        /// <summary>
        /// Applies Leaky ReLU: max(αx, x) where α = 0.01.
        /// </summary>
        public static void LeakyReLU(float[] input, float alpha = 0.01f)
        {
            for (int i = 0; i < input.Length; i++)
            {
                input[i] = input[i] > 0 ? input[i] : alpha * input[i];
            }
        }

        /// <summary>
        /// Applies ELU: x if x > 0, else α(e^x - 1).
        /// </summary>
        public static void ELU(float[] input, float alpha = 1.0f)
        {
            for (int i = 0; i < input.Length; i++)
            {
                if (input[i] <= 0)
                {
                    input[i] = alpha * ((float)Math.Exp(input[i]) - 1.0f);
                }
            }
        }

        /// <summary>
        /// Applies SELU: λx if x > 0, else λα(e^x - 1) with fixed parameters.
        /// λ ≈ 1.0507, α ≈ 1.67326
        /// </summary>
        public static void SELU(float[] input)
        {
            const float lambda = 1.0507009873554804934193349852946f;
            const float alpha = 1.6732632423543772848170429916717f;

            for (int i = 0; i < input.Length; i++)
            {
                if (input[i] > 0)
                {
                    input[i] = lambda * input[i];
                }
                else
                {
                    input[i] = lambda * alpha * ((float)Math.Exp(input[i]) - 1.0f);
                }
            }
        }

        /// <summary>
        /// Applies Softmax: exp(x_i) / sum(exp(x_j)) over entire array.
        /// Numerically stable version using max subtraction.
        /// </summary>
        public static void Softmax(float[] input)
        {
            if (input.Length == 0)
                return;

            // Find max for numerical stability
            float max = input[0];
            for (int i = 1; i < input.Length; i++)
            {
                if (input[i] > max)
                    max = input[i];
            }

            // Compute exp(x - max) and sum
            double sum = 0.0;
            for (int i = 0; i < input.Length; i++)
            {
                input[i] = (float)Math.Exp(input[i] - max);
                sum += input[i];
            }

            // Normalize
            float sumFloat = (float)sum;
            int simdLength = Vector<float>.Count;
            int j = 0;

            // SIMD path
            var sumVec = new Vector<float>(sumFloat);
            for (; j <= input.Length - simdLength; j += simdLength)
            {
                var vec = new Vector<float>(input, j);
                var result = vec / sumVec;
                result.CopyTo(input, j);
            }

            // Scalar remainder
            for (; j < input.Length; j++)
            {
                input[j] /= sumFloat;
            }
        }

        /// <summary>
        /// Gets activation function delegate by enum type.
        /// </summary>
        public static Action<float[]> GetActivation(ActivationFunction type)
        {
            return type switch
            {
                ActivationFunction.ReLU => ReLU,
                ActivationFunction.GELU => GELU,
                ActivationFunction.Tanh => Tanh,
                ActivationFunction.Sigmoid => Sigmoid,
                ActivationFunction.Swish or ActivationFunction.SiLU => Swish,
                ActivationFunction.Mish => Mish,
                ActivationFunction.LeakyReLU => (data) => LeakyReLU(data, 0.01f),
                ActivationFunction.ELU => (data) => ELU(data, 1.0f),
                ActivationFunction.SELU => SELU,
                ActivationFunction.Softmax => Softmax,
                ActivationFunction.None => _ => { }, // No-op
                _ => throw new ArgumentException($"Unknown activation function: {type}")
            };
        }

        /// <summary>
        /// Applies activation function by enum type.
        /// </summary>
        public static void Apply(float[] input, ActivationFunction type)
        {
            GetActivation(type)(input);
        }
    }
}
