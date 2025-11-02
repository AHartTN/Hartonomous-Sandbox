using System;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;
using Microsoft.SqlServer.Types;

namespace SqlClrFunctions
{
    /// <summary>
    /// Audio processing operations handled in-process via SQL CLR.
    /// </summary>
    public static class AudioProcessing
    {
        /// <summary>
        /// Convert 16-bit PCM audio samples into a line-string geometry representing the waveform.
        /// X-axis is time (seconds), Y-axis is normalized amplitude [-1, 1].
        /// </summary>
        [SqlFunction(IsDeterministic = false, IsPrecise = false)]
        public static SqlGeometry AudioToWaveform(SqlBytes audioData, SqlInt32 channelCount, SqlInt32 sampleRate, SqlInt32 maxPoints)
        {
            if (audioData.IsNull)
            {
                return SqlGeometry.Null;
            }

            byte[] buffer = audioData.Value;
            int channels = channelCount.IsNull || channelCount.Value <= 0 ? 1 : channelCount.Value;
            int rate = sampleRate.IsNull || sampleRate.Value <= 0 ? 44100 : sampleRate.Value;
            int desiredPoints = maxPoints.IsNull || maxPoints.Value <= 0 ? 4096 : Math.Max(2, maxPoints.Value);

            int bytesPerSample = 2 * channels;
            if (buffer.Length < bytesPerSample)
            {
                return SqlGeometry.Null;
            }

            int totalSamples = buffer.Length / bytesPerSample;
            if (totalSamples < 2)
            {
                return SqlGeometry.Null;
            }

            int stride = Math.Max(1, totalSamples / desiredPoints);

            var builder = new SqlGeometryBuilder();
            builder.SetSrid(0);
            builder.BeginGeometry(OpenGisGeometryType.LineString);

            double samplePeriod = 1.0 / rate;
            double amplitude = ReadSampleNormalized(buffer, 0, channels);
            builder.BeginFigure(0.0, amplitude);

            for (int i = stride; i < totalSamples; i += stride)
            {
                amplitude = ReadSampleNormalized(buffer, i, channels);
                builder.AddLine(i * samplePeriod, amplitude);
            }

            if ((totalSamples - 1) % stride != 0)
            {
                amplitude = ReadSampleNormalized(buffer, totalSamples - 1, channels);
                builder.AddLine((totalSamples - 1) * samplePeriod, amplitude);
            }

            builder.EndFigure();
            builder.EndGeometry();

            return builder.ConstructedGeometry;
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

            byte[] buffer = audioData.Value;
            int channels = channelCount.IsNull || channelCount.Value <= 0 ? 1 : channelCount.Value;
            int bytesPerSample = 2 * channels;
            if (buffer.Length < bytesPerSample)
            {
                return new SqlDouble(0);
            }

            int totalSamples = buffer.Length / bytesPerSample;
            double sum = 0;

            for (int i = 0; i < totalSamples; i++)
            {
                double sample = ReadSampleNormalized(buffer, i, channels);
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

            byte[] buffer = audioData.Value;
            int channels = channelCount.IsNull || channelCount.Value <= 0 ? 1 : channelCount.Value;
            int bytesPerSample = 2 * channels;
            if (buffer.Length < bytesPerSample)
            {
                return new SqlDouble(0);
            }

            int totalSamples = buffer.Length / bytesPerSample;
            double peak = 0;

            for (int i = 0; i < totalSamples; i++)
            {
                double sample = Math.Abs(ReadSampleNormalized(buffer, i, channels));
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

            byte[] buffer = audioData.Value;
            int channels = channelCount.IsNull || channelCount.Value <= 0 ? 1 : channelCount.Value;
            int bytesPerSample = 2 * channels;
            if (buffer.Length < bytesPerSample)
            {
                return SqlBytes.Null;
            }

            int totalSamples = buffer.Length / bytesPerSample;
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
                        short sampleValue = BitConverter.ToInt16(buffer, offset);
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

        private static double ReadSampleNormalized(byte[] buffer, int sampleIndex, int channels)
        {
            int offset = sampleIndex * channels * 2;
            if (offset + (channels * 2) > buffer.Length)
            {
                return 0;
            }

            long sum = 0;
            for (int c = 0; c < channels; c++)
            {
                int sampleOffset = offset + c * 2;
                short value = BitConverter.ToInt16(buffer, sampleOffset);
                sum += value;
            }

            double average = sum / (double)channels;
            return average / short.MaxValue;
        }
    }
}
