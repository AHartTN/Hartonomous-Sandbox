using System;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;

namespace Hartonomous.Clr
{
    /// <summary>
    /// Audio processing operations using NetTopologySuite for spatial representations.
    /// </summary>
    public static class AudioProcessing
    {
        private static readonly GeometryFactory _geometryFactory = new GeometryFactory(new PrecisionModel(), 0);
        private static readonly SqlServerBytesWriter _geometryWriter = new SqlServerBytesWriter();
        private static readonly SqlServerBytesReader _geometryReader = new SqlServerBytesReader();

        /// <summary>
        /// Convert 16-bit PCM audio samples into a line-string geometry representing the waveform.
        /// X-axis is time (seconds), Y-axis is normalized amplitude [-1, 1].
        /// </summary>
        [SqlFunction(IsDeterministic = false, IsPrecise = false)]
        public static SqlBytes AudioToWaveform(SqlBytes audioData, SqlInt32 channelCount, SqlInt32 sampleRate, SqlInt32 maxPoints)
        {
            if (audioData.IsNull)
            {
                return SqlBytes.Null;
            }

            try
            {
                var buffer = SqlBytesInterop.GetBuffer(audioData, out var byteLength);
                int channels = channelCount.IsNull || channelCount.Value <= 0 ? 1 : channelCount.Value;
                int rate = sampleRate.IsNull || sampleRate.Value <= 0 ? 44100 : sampleRate.Value;
                int desiredPoints = maxPoints.IsNull || maxPoints.Value <= 0 ? 4096 : Math.Max(2, maxPoints.Value);

                int bytesPerSample = 2 * channels;
                if (byteLength < bytesPerSample)
                {
                    return SqlBytes.Null;
                }

                int totalSamples = byteLength / bytesPerSample;
                if (totalSamples < 2)
                {
                    return SqlBytes.Null;
                }

                int stride = Math.Max(1, totalSamples / desiredPoints);
                int actualPoints = Math.Min(desiredPoints, (totalSamples + stride - 1) / stride);
                
                var coordinates = new Coordinate[actualPoints];
                int coordIndex = 0;
                double samplePeriod = 1.0 / rate;

                for (int i = 0; i < totalSamples && coordIndex < actualPoints; i += stride)
                {
                    double amplitude = ReadSampleNormalized(buffer, byteLength, i, channels);
                    coordinates[coordIndex++] = new Coordinate(i * samplePeriod, amplitude);
                }

                // Ensure last sample included if not already
                if ((totalSamples - 1) % stride != 0 && coordIndex < actualPoints)
                {
                    double amplitude = ReadSampleNormalized(buffer, byteLength, totalSamples - 1, channels);
                    coordinates[coordIndex++] = new Coordinate((totalSamples - 1) * samplePeriod, amplitude);
                }

                if (coordIndex < coordinates.Length)
                {
                    Array.Resize(ref coordinates, coordIndex);
                }

                var lineString = _geometryFactory.CreateLineString(coordinates);
                var geometryBytes = _geometryWriter.Write(lineString);

                return new SqlBytes(geometryBytes);
            }
            catch
            {
                return SqlBytes.Null;
            }
        }

        /// <summary>
        /// Compute the overall RMS amplitude of 16-bit PCM audio.
        /// </summary>
        [SqlFunction(IsDeterministic = true, IsPrecise = false)]
        public static SqlDouble AudioComputeRms(SqlBytes audioData, SqlInt32 channelCount)
        {
            if (audioData.IsNull)
            {
                return SqlDouble.Null;
            }

            var buffer = SqlBytesInterop.GetBuffer(audioData, out var byteLength);
            int channels = channelCount.IsNull || channelCount.Value <= 0 ? 1 : channelCount.Value;
            int bytesPerSample = 2 * channels;
            if (byteLength < bytesPerSample)
            {
                return new SqlDouble(0);
            }

            int totalSamples = byteLength / bytesPerSample;
            double sum = 0;

            for (int i = 0; i < totalSamples; i++)
            {
                double sample = ReadSampleNormalized(buffer, byteLength, i, channels);
                sum += sample * sample;
            }

            double rms = Math.Sqrt(sum / totalSamples);
            return new SqlDouble(rms);
        }

        /// <summary>
        /// Compute the peak absolute amplitude of 16-bit PCM audio.
        /// </summary>
        [SqlFunction(IsDeterministic = true, IsPrecise = false)]
        public static SqlDouble AudioComputePeak(SqlBytes audioData, SqlInt32 channelCount)
        {
            if (audioData.IsNull)
            {
                return SqlDouble.Null;
            }

            var buffer = SqlBytesInterop.GetBuffer(audioData, out var byteLength);
            int channels = channelCount.IsNull || channelCount.Value <= 0 ? 1 : channelCount.Value;
            int bytesPerSample = 2 * channels;
            if (byteLength < bytesPerSample)
            {
                return new SqlDouble(0);
            }

            int totalSamples = byteLength / bytesPerSample;
            double peak = 0;

            for (int i = 0; i < totalSamples; i++)
            {
                double sample = Math.Abs(ReadSampleNormalized(buffer, byteLength, i, channels));
                if (sample > peak)
                {
                    peak = sample;
                }
            }

            return new SqlDouble(peak);
        }

        /// <summary>
        /// Down-sample audio by an integer factor using mean averaging per channel.
        /// </summary>
        [SqlFunction(IsDeterministic = true, IsPrecise = false)]
        public static SqlBytes AudioDownsample(SqlBytes audioData, SqlInt32 channelCount, SqlInt32 factor)
        {
            if (audioData.IsNull)
            {
                return SqlBytes.Null;
            }

            int downFactor = factor.IsNull || factor.Value <= 1 ? 1 : factor.Value;
            if (downFactor == 1)
            {
                return audioData;
            }

            var buffer = SqlBytesInterop.GetBuffer(audioData, out var byteLength);
            int channels = channelCount.IsNull || channelCount.Value <= 0 ? 1 : channelCount.Value;
            int bytesPerSample = 2 * channels;
            if (byteLength < bytesPerSample)
            {
                return SqlBytes.Null;
            }

            int totalSamples = byteLength / bytesPerSample;
            int reducedSamples = totalSamples / downFactor;
            if (reducedSamples == 0)
            {
                return SqlBytes.Null;
            }

            byte[] result = new byte[reducedSamples * bytesPerSample];

            for (int i = 0; i < reducedSamples; i++)
            {
                int baseSample = i * downFactor;
                for (int channel = 0; channel < channels; channel++)
                {
                    long accumulator = 0;
                    int samplesIncluded = 0;

                    for (int step = 0; step < downFactor; step++)
                    {
                        int sampleIndex = baseSample + step;
                        if (sampleIndex >= totalSamples)
                        {
                            break;
                        }

                        int offset = (sampleIndex * channels + channel) * 2;
                        if (offset + 2 > byteLength)
                        {
                            break;
                        }

                        short sampleValue = ReadInt16Le(buffer, offset);
                        accumulator += sampleValue;
                        samplesIncluded++;
                    }

                    short averaged = samplesIncluded == 0
                        ? (short)0
                        : (short)Math.Max(short.MinValue, Math.Min(short.MaxValue, accumulator / samplesIncluded));

                    int targetOffset = (i * channels + channel) * 2;
                    byte[] packed = BitConverter.GetBytes(averaged);
                    result[targetOffset] = packed[0];
                    result[targetOffset + 1] = packed[1];
                }
            }

            return new SqlBytes(result);
        }

        /// <summary>
        /// Synthesize a harmonic tone in 16-bit PCM. Used when no matching audio asset exists.
        /// </summary>
        [SqlFunction(IsDeterministic = false, IsPrecise = false)]
        public static SqlBytes GenerateHarmonicTone(
            SqlDouble fundamentalHz,
            SqlInt32 durationMs,
            SqlInt32 sampleRate,
            SqlInt32 channelCount,
            SqlDouble amplitude,
            SqlDouble secondHarmonicLevel,
            SqlDouble thirdHarmonicLevel)
        {
            double fundamental = fundamentalHz.IsNull ? 440.0 : Clamp(fundamentalHz.Value, 20.0, 20000.0);
            int duration = durationMs.IsNull || durationMs.Value <= 0 ? 2000 : durationMs.Value;
            int rate = sampleRate.IsNull || sampleRate.Value <= 0 ? 44100 : sampleRate.Value;
            int channels = channelCount.IsNull || channelCount.Value <= 0 ? 2 : channelCount.Value;
            double primaryAmplitude = amplitude.IsNull ? 0.75 : Clamp(amplitude.Value, 0.0, 1.0);
            double secondLevel = secondHarmonicLevel.IsNull ? 0.0 : Clamp(secondHarmonicLevel.Value, 0.0, 1.0);
            double thirdLevel = thirdHarmonicLevel.IsNull ? 0.0 : Clamp(thirdHarmonicLevel.Value, 0.0, 1.0);

            int totalSamples = Math.Max(1, (int)Math.Round(rate * (duration / 1000.0)));
            int bytesPerSample = channels * 2;
            byte[] buffer = new byte[totalSamples * bytesPerSample];

            double twoPi = 2 * Math.PI;
            double inverseSampleRate = 1.0 / rate;

            for (int sampleIndex = 0; sampleIndex < totalSamples; sampleIndex++)
            {
                double time = sampleIndex * inverseSampleRate;
                double envelope = Math.Sin(Math.PI * sampleIndex / totalSamples);

                double value = primaryAmplitude * Math.Sin(twoPi * fundamental * time);
                if (secondLevel > 0)
                {
                    value += primaryAmplitude * secondLevel * Math.Sin(twoPi * fundamental * 2 * time);
                }
                if (thirdLevel > 0)
                {
                    value += primaryAmplitude * thirdLevel * Math.Sin(twoPi * fundamental * 3 * time);
                }

                value = Clamp(value * envelope, -1.0, 1.0);
                short pcm = (short)Math.Round(value * short.MaxValue);
                byte[] packed = BitConverter.GetBytes(pcm);

                for (int channel = 0; channel < channels; channel++)
                {
                    int offset = (sampleIndex * channels + channel) * 2;
                    buffer[offset] = packed[0];
                    buffer[offset + 1] = packed[1];
                }
            }

            return new SqlBytes(buffer);
        }

        private static double ReadSampleNormalized(byte[] buffer, int bufferLength, int sampleIndex, int channels)
        {
            int offset = sampleIndex * channels * 2;
            if (offset >= bufferLength)
            {
                return 0;
            }

            long sum = 0;
            int actualChannels = 0;
            for (int c = 0; c < channels; c++)
            {
                int sampleOffset = offset + c * 2;
                if (sampleOffset + 2 > bufferLength)
                {
                    break;
                }

                short value = ReadInt16Le(buffer, sampleOffset);
                sum += value;
                actualChannels++;
            }

            if (actualChannels == 0)
            {
                return 0;
            }

            double average = sum / (double)actualChannels;
            return average / short.MaxValue;
        }

        private static short ReadInt16Le(byte[] buffer, int offset)
        {
            return (short)(buffer[offset] | (buffer[offset + 1] << 8));
        }

        private static double Clamp(double value, double min, double max)
        {
            if (value < min)
            {
                return min;
            }

            if (value > max)
            {
                return max;
            }

            return value;
        }

        /// <summary>
        /// Generates a synthesized audio tone from a spatial signature GEOMETRY point.
        /// Maps geometric properties to audio synthesis parameters.
        /// </summary>
        [SqlFunction(IsDeterministic = false, IsPrecise = false)]
        public static SqlBytes GenerateAudioFromSpatialSignature(SqlBytes spatialSignature)
        {
            if (spatialSignature.IsNull || spatialSignature.Length == 0)
            {
                return SqlBytes.Null;
            }

            try
            {
                var geometry = _geometryReader.Read(spatialSignature.Value);
                
                if (geometry == null || geometry.IsEmpty || !(geometry is Point point))
                {
                    return SqlBytes.Null;
                }

                // Map geometric properties to synthesis parameters
                double x = point.X;
                double y = point.Y;
                double z = double.IsNaN(point.Z) ? 0.5 : point.Z;
                double m = point.M;  // M coordinate for measure/importance
                if (double.IsNaN(m))
                    m = 0.5;

                // Normalize and scale parameters
                var fundamentalHz = new SqlDouble(Clamp(z * 800 + 100, 50, 1200));
                var amplitude = new SqlDouble(Clamp(m, 0.1, 1.0));
                var secondHarmonic = new SqlDouble(Clamp(x, 0.0, 1.0));
                var thirdHarmonic = new SqlDouble(Clamp(y, 0.0, 1.0));

                var durationMs = new SqlInt32(1500);
                var sampleRate = new SqlInt32(44100);
                var channelCount = new SqlInt32(1);

                return GenerateHarmonicTone(
                    fundamentalHz,
                    durationMs,
                    sampleRate,
                    channelCount,
                    amplitude,
                    secondHarmonic,
                    thirdHarmonic
                );
            }
            catch
            {
                return SqlBytes.Null;
            }
        }
    }
}
