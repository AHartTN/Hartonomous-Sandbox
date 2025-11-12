using System;

namespace Hartonomous.Core.Pipelines.Ingestion
{
    /// <summary>
    /// Implements Fast Fourier Transform (FFT) using Cooley-Tukey radix-2 algorithm.
    /// Based on Microsoft's FFT documentation and mathematical principles.
    /// Transforms time-domain audio samples to frequency-domain representation.
    /// NO external libraries - pure C# implementation for database-first AI runtime.
    /// </summary>
    public class FftProcessor
    {
        /// <summary>
        /// Complex number representation for FFT calculations.
        /// </summary>
        public struct Complex
        {
            public double Real { get; set; }
            public double Imaginary { get; set; }

            public Complex(double real, double imaginary)
            {
                Real = real;
                Imaginary = imaginary;
            }

            /// <summary>
            /// Magnitude: sqrt(real² + imaginary²)
            /// </summary>
            public double Magnitude => Math.Sqrt(Real * Real + Imaginary * Imaginary);

            /// <summary>
            /// Phase angle: atan2(imaginary, real)
            /// </summary>
            public double Phase => Math.Atan2(Imaginary, Real);

            /// <summary>
            /// Complex addition
            /// </summary>
            public static Complex operator +(Complex a, Complex b)
            {
                return new Complex(a.Real + b.Real, a.Imaginary + b.Imaginary);
            }

            /// <summary>
            /// Complex subtraction
            /// </summary>
            public static Complex operator -(Complex a, Complex b)
            {
                return new Complex(a.Real - b.Real, a.Imaginary - b.Imaginary);
            }

            /// <summary>
            /// Complex multiplication
            /// </summary>
            public static Complex operator *(Complex a, Complex b)
            {
                return new Complex(
                    a.Real * b.Real - a.Imaginary * b.Imaginary,
                    a.Real * b.Imaginary + a.Imaginary * b.Real
                );
            }

            /// <summary>
            /// Scalar multiplication
            /// </summary>
            public static Complex operator *(Complex a, double scalar)
            {
                return new Complex(a.Real * scalar, a.Imaginary * scalar);
            }

            /// <summary>
            /// Complex exponential: e^(i*angle)
            /// </summary>
            public static Complex FromPolar(double magnitude, double phase)
            {
                return new Complex(magnitude * Math.Cos(phase), magnitude * Math.Sin(phase));
            }
        }

        /// <summary>
        /// Performs in-place Cooley-Tukey FFT on complex input.
        /// Time complexity: O(N log N)
        /// N must be a power of 2.
        /// </summary>
        /// <param name="data">Complex array (will be modified in-place)</param>
        /// <param name="inverse">If true, performs inverse FFT</param>
        public static void Fft(Complex[] data, bool inverse = false)
        {
            int n = data.Length;

            // Validate power of 2
            if (n == 0 || (n & (n - 1)) != 0)
            {
                throw new ArgumentException("FFT size must be a power of 2");
            }

            // Bit-reversal permutation
            int bits = (int)Math.Log2(n);
            for (int i = 0; i < n; i++)
            {
                int reversed = ReverseBits(i, bits);
                if (reversed > i)
                {
                    // Swap
                    (data[i], data[reversed]) = (data[reversed], data[i]);
                }
            }

            // Cooley-Tukey iterative FFT (butterfly operations)
            for (int size = 2; size <= n; size *= 2)
            {
                int halfSize = size / 2;
                double angle = (inverse ? 2.0 : -2.0) * Math.PI / size;

                // Precompute twiddle factor (w = e^(i*angle))
                for (int i = 0; i < n; i += size)
                {
                    for (int k = 0; k < halfSize; k++)
                    {
                        // Twiddle factor: e^(-2πik/size) for forward FFT
                        Complex w = Complex.FromPolar(1.0, angle * k);
                        Complex even = data[i + k];
                        Complex odd = data[i + k + halfSize] * w;

                        // Butterfly operation
                        data[i + k] = even + odd;
                        data[i + k + halfSize] = even - odd;
                    }
                }
            }

            // Normalize for inverse FFT
            if (inverse)
            {
                for (int i = 0; i < n; i++)
                {
                    data[i] = data[i] * (1.0 / n);
                }
            }
        }

        /// <summary>
        /// Performs FFT on real-valued samples (e.g., PCM audio).
        /// Returns frequency-domain complex representation.
        /// </summary>
        /// <param name="samples">Real-valued samples (time domain)</param>
        /// <returns>Complex frequency bins</returns>
        public static Complex[] FftReal(short[] samples)
        {
            if (samples == null || samples.Length == 0)
            {
                return Array.Empty<Complex>();
            }

            // Find next power of 2
            int n = NextPowerOf2(samples.Length);

            // Convert to complex (zero-pad if necessary)
            var complexData = new Complex[n];
            for (int i = 0; i < samples.Length; i++)
            {
                complexData[i] = new Complex(samples[i] / 32768.0, 0.0); // Normalize to -1.0 to 1.0
            }

            // Perform FFT
            Fft(complexData, inverse: false);

            return complexData;
        }

        /// <summary>
        /// Calculates magnitude spectrum from FFT output.
        /// Returns array of magnitudes (sqrt(real² + imag²)).
        /// </summary>
        /// <param name="fftOutput">Complex FFT output</param>
        /// <returns>Magnitude spectrum (first N/2 bins for real input)</returns>
        public static double[] MagnitudeSpectrum(Complex[] fftOutput)
        {
            if (fftOutput == null || fftOutput.Length == 0)
            {
                return Array.Empty<double>();
            }

            // For real input, only first N/2 bins are unique (Nyquist theorem)
            int halfN = fftOutput.Length / 2 + 1;
            var magnitudes = new double[halfN];

            for (int i = 0; i < halfN; i++)
            {
                magnitudes[i] = fftOutput[i].Magnitude;
            }

            return magnitudes;
        }

        /// <summary>
        /// Converts frequency bin index to Hz.
        /// Formula from MS docs: frequency[k] = k * sampleRate / N
        /// </summary>
        /// <param name="binIndex">FFT bin index</param>
        /// <param name="sampleRate">Sample rate in Hz</param>
        /// <param name="fftSize">FFT size (N)</param>
        /// <returns>Frequency in Hz</returns>
        public static double BinToFrequency(int binIndex, int sampleRate, int fftSize)
        {
            return (double)binIndex * sampleRate / fftSize;
        }

        /// <summary>
        /// Reverses bits in an integer (for bit-reversal permutation).
        /// </summary>
        private static int ReverseBits(int value, int bits)
        {
            int result = 0;
            for (int i = 0; i < bits; i++)
            {
                result = (result << 1) | (value & 1);
                value >>= 1;
            }
            return result;
        }

        /// <summary>
        /// Finds next power of 2 >= n.
        /// </summary>
        private static int NextPowerOf2(int n)
        {
            if (n <= 0)
            {
                return 1;
            }

            int power = 1;
            while (power < n)
            {
                power *= 2;
            }
            return power;
        }

        /// <summary>
        /// Applies Hann window function to reduce spectral leakage.
        /// Formula: 0.5 - 0.5 * cos(2π*n/N)
        /// </summary>
        /// <param name="samples">Input samples</param>
        /// <returns>Windowed samples</returns>
        public static double[] ApplyHannWindow(short[] samples)
        {
            if (samples == null || samples.Length == 0)
            {
                return Array.Empty<double>();
            }

            int n = samples.Length;
            var windowed = new double[n];

            for (int i = 0; i < n; i++)
            {
                double windowValue = 0.5 - 0.5 * Math.Cos(2.0 * Math.PI * i / n);
                windowed[i] = (samples[i] / 32768.0) * windowValue;
            }

            return windowed;
        }
    }
}
