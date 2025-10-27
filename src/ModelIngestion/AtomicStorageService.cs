using Microsoft.Data.SqlClient;
using Microsoft.Data.SqlTypes;
using Microsoft.Extensions.Logging;
using Hartonomous.Core.Interfaces;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ModelIngestion
{
    /// <summary>
    /// Service for content-addressable atomic component storage.
    /// NEVER store the same atomic component twice - use hashing for deduplication.
    /// Examples: pixels, audio samples, vector components, tokens, waveform patterns
    /// </summary>
    public class AtomicStorageService : IAtomicStorageService
    {
        private readonly string _connectionString;
        private readonly ILogger<AtomicStorageService> _logger;

        public AtomicStorageService(string connectionString, ILogger<AtomicStorageService> logger)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // =============================================
        // PIXEL STORAGE (Images)
        // =============================================

        /// <summary>
        /// Store atomic pixel with content-addressable deduplication
        /// Returns: pixel_id (existing or newly created)
        /// </summary>
        public async Task<long> StoreAtomicPixelAsync(byte r, byte g, byte b, byte a = 255, CancellationToken cancellationToken = default)
        {
            var pixelHash = ComputePixelHash(r, g, b, a);

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            // Check if pixel already exists and return ID
            var existsSql = "SELECT pixel_id FROM dbo.AtomicPixels WHERE pixel_hash = @hash";
            using (var checkCmd = new SqlCommand(existsSql, connection))
            {
                checkCmd.Parameters.AddWithValue("@hash", pixelHash);
                var existingId = await checkCmd.ExecuteScalarAsync(cancellationToken);

                if (existingId != null)
                {
                    // Increment reference count
                    await IncrementPixelReferenceAsync(connection, pixelHash, cancellationToken);
                    return Convert.ToInt64(existingId);
                }
            }

            // Insert new atomic pixel and return ID
            var insertSql = @"
                INSERT INTO dbo.AtomicPixels (pixel_hash, r, g, b, a, color_point, reference_count)
                VALUES (@hash, @r, @g, @b, @a,
                    geometry::STGeomFromText('POINT(' +
                        CAST(@r AS NVARCHAR(10)) + ' ' +
                        CAST(@g AS NVARCHAR(10)) + ' ' +
                        CAST(@b AS NVARCHAR(10)) + ')', 0),
                    1);
                SELECT SCOPE_IDENTITY();
            ";

            using (var insertCmd = new SqlCommand(insertSql, connection))
            {
                insertCmd.Parameters.AddWithValue("@hash", pixelHash);
                insertCmd.Parameters.AddWithValue("@r", r);
                insertCmd.Parameters.AddWithValue("@g", g);
                insertCmd.Parameters.AddWithValue("@b", b);
                insertCmd.Parameters.AddWithValue("@a", a);
                var result = await insertCmd.ExecuteScalarAsync(cancellationToken);
                return Convert.ToInt64(result);
            }
        }

        /// <summary>
        /// Store a batch of atomic pixels for efficient bulk operations
        /// </summary>
        public async Task<IEnumerable<long>> StoreBatchPixelsAsync(
            IEnumerable<(byte r, byte g, byte b, byte a)> pixels, 
            CancellationToken cancellationToken = default)
        {
            var ids = new List<long>();
            
            foreach (var pixel in pixels)
            {
                var id = await StoreAtomicPixelAsync(pixel.r, pixel.g, pixel.b, pixel.a, cancellationToken);
                ids.Add(id);
            }
            
            return ids;
        }

        /// <summary>
        /// Store entire image as references to atomic pixels
        /// Returns: Number of unique pixels stored
        /// </summary>
        public async Task<int> StoreImageAtomicallyAsync(long imageId, byte[] pixelData, int width, int height, int channels = 3)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();

            int uniquePixels = 0;
            try
            {
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int offset = (y * width + x) * channels;
                        byte r = pixelData[offset];
                        byte g = pixelData[offset + 1];
                        byte b = pixelData[offset + 2];
                        byte a = channels == 4 ? pixelData[offset + 3] : (byte)255;

                        var pixelHash = await StoreAtomicPixelAsync(r, g, b, a);

                        // Store reference
                        var refSql = @"
                            INSERT INTO dbo.ImagePixelReferences (image_id, pixel_x, pixel_y, pixel_hash)
                            VALUES (@image_id, @x, @y, @hash);
                        ";

                        using var refCmd = new SqlCommand(refSql, connection, transaction);
                        refCmd.Parameters.AddWithValue("@image_id", imageId);
                        refCmd.Parameters.AddWithValue("@x", x);
                        refCmd.Parameters.AddWithValue("@y", y);
                        refCmd.Parameters.AddWithValue("@hash", pixelHash);
                        await refCmd.ExecuteNonQueryAsync();

                        uniquePixels++;
                    }
                }

                await transaction.CommitAsync();
                return uniquePixels;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        private async Task IncrementPixelReferenceAsync(SqlConnection connection, byte[] pixelHash, CancellationToken cancellationToken)
        {
            var sql = @"
                UPDATE dbo.AtomicPixels
                SET reference_count = reference_count + 1,
                    last_referenced = SYSUTCDATETIME()
                WHERE pixel_hash = @hash;
            ";

            using var cmd = new SqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@hash", pixelHash);
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }

        // =============================================
        // AUDIO SAMPLE STORAGE
        // =============================================

        /// <summary>
        /// Store atomic audio sample with deduplication
        /// Interface expects normalized amplitude (-1.0 to 1.0)
        /// </summary>
        public async Task<long> StoreAtomicAudioSampleAsync(float amplitude, CancellationToken cancellationToken = default)
        {
            // Convert normalized amplitude to int16 for storage
            short amplitudeInt16 = (short)(amplitude * 32767.0f);
            var sampleHash = ComputeAudioSampleHash(amplitudeInt16);

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            // Check if sample already exists and return ID
            var existsSql = "SELECT sample_id FROM dbo.AtomicAudioSamples WHERE sample_hash = @hash";
            using (var checkCmd = new SqlCommand(existsSql, connection))
            {
                checkCmd.Parameters.AddWithValue("@hash", sampleHash);
                var existingId = await checkCmd.ExecuteScalarAsync(cancellationToken);

                if (existingId != null)
                {
                    await IncrementAudioSampleReferenceAsync(connection, sampleHash, cancellationToken);
                    return Convert.ToInt64(existingId);
                }
            }

            // Insert new atomic audio sample and return ID
            var insertSql = @"
                INSERT INTO dbo.AtomicAudioSamples (sample_hash, amplitude_normalized, amplitude_int16, reference_count)
                VALUES (@hash, @normalized, @int16, 1);
                SELECT SCOPE_IDENTITY();
            ";

            using (var insertCmd = new SqlCommand(insertSql, connection))
            {
                insertCmd.Parameters.AddWithValue("@hash", sampleHash);
                insertCmd.Parameters.AddWithValue("@normalized", amplitude);
                insertCmd.Parameters.AddWithValue("@int16", amplitudeInt16);
                var result = await insertCmd.ExecuteScalarAsync(cancellationToken);
                return Convert.ToInt64(result);
            }
        }

        /// <summary>
        /// Store entire audio as references to atomic samples
        /// </summary>
        public async Task<int> StoreAudioAtomicallyAsync(long audioId, short[] samples, int numChannels = 1)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();

            int uniqueSamples = 0;
            try
            {
                for (long sampleNum = 0; sampleNum < samples.Length / numChannels; sampleNum++)
                {
                    for (byte channel = 0; channel < numChannels; channel++)
                    {
                        short amplitude = samples[sampleNum * numChannels + channel];
                        var sampleHash = await StoreAtomicAudioSampleAsync(amplitude);

                        // Store reference
                        var refSql = @"
                            INSERT INTO dbo.AudioSampleReferences (audio_id, sample_number, channel, sample_hash)
                            VALUES (@audio_id, @sample_num, @channel, @hash);
                        ";

                        using var refCmd = new SqlCommand(refSql, connection, transaction);
                        refCmd.Parameters.AddWithValue("@audio_id", audioId);
                        refCmd.Parameters.AddWithValue("@sample_num", sampleNum);
                        refCmd.Parameters.AddWithValue("@channel", channel);
                        refCmd.Parameters.AddWithValue("@hash", sampleHash);
                        await refCmd.ExecuteNonQueryAsync();

                        uniqueSamples++;
                    }
                }

                await transaction.CommitAsync();
                return uniqueSamples;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        private async Task IncrementAudioSampleReferenceAsync(SqlConnection connection, byte[] sampleHash, CancellationToken cancellationToken)
        {
            var sql = @"
                UPDATE dbo.AtomicAudioSamples
                SET reference_count = reference_count + 1,
                    last_referenced = SYSUTCDATETIME()
                WHERE sample_hash = @hash;
            ";

            using var cmd = new SqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@hash", sampleHash);
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }

        // =============================================
        // VECTOR COMPONENT STORAGE (Legacy - not in interface)
        // =============================================

        /// <summary>
        /// Store atomic vector component (single float) with deduplication
        /// LEGACY: Not part of IAtomicStorageService interface
        /// </summary>
        [Obsolete("This method is not part of IAtomicStorageService interface. Consider using batch operations instead.")]
        public async Task<byte[]> StoreAtomicVectorComponentAsync(float value, CancellationToken cancellationToken = default)
        {
            var componentHash = ComputeFloatHash(value);

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            var existsSql = "SELECT 1 FROM dbo.AtomicVectorComponents WHERE component_hash = @hash";
            using (var checkCmd = new SqlCommand(existsSql, connection))
            {
                checkCmd.Parameters.AddWithValue("@hash", componentHash);
                var exists = await checkCmd.ExecuteScalarAsync(cancellationToken);

                if (exists != null)
                {
                    await IncrementVectorComponentReferenceAsync(connection, componentHash, cancellationToken);
                    return componentHash;
                }
            }

            var insertSql = @"
                INSERT INTO dbo.AtomicVectorComponents (component_hash, float_value, reference_count)
                VALUES (@hash, @value, 1);
            ";

            using (var insertCmd = new SqlCommand(insertSql, connection))
            {
                insertCmd.Parameters.AddWithValue("@hash", componentHash);
                insertCmd.Parameters.AddWithValue("@value", value);
                await insertCmd.ExecuteNonQueryAsync(cancellationToken);
            }

            return componentHash;
        }

        private async Task IncrementVectorComponentReferenceAsync(SqlConnection connection, byte[] componentHash, CancellationToken cancellationToken)
        {
            var sql = @"
                UPDATE dbo.AtomicVectorComponents
                SET reference_count = reference_count + 1,
                    last_referenced = SYSUTCDATETIME()
                WHERE component_hash = @hash;
            ";

            using var cmd = new SqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@hash", componentHash);
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }

        // =============================================
        // TOKEN STORAGE
        // =============================================

        /// <summary>
        /// Store atomic text token with deduplication
        /// </summary>
        public async Task<long> StoreAtomicTextTokenAsync(string tokenText, int? vocabId = null, CancellationToken cancellationToken = default)
        {
            var tokenHash = ComputeTextHash(tokenText);

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            // Check if token already exists and return ID
            var existsSql = "SELECT token_id FROM dbo.AtomicTextTokens WHERE token_hash = @hash";
            using (var checkCmd = new SqlCommand(existsSql, connection))
            {
                checkCmd.Parameters.AddWithValue("@hash", tokenHash);
                var existingId = await checkCmd.ExecuteScalarAsync(cancellationToken);

                if (existingId != null)
                {
                    await IncrementTokenReferenceAsync(connection, tokenHash, cancellationToken);
                    return Convert.ToInt64(existingId);
                }
            }

            // Insert new atomic token and return ID
            var insertSql = @"
                INSERT INTO dbo.AtomicTextTokens (token_hash, token_text, token_length, vocab_id, reference_count)
                VALUES (@hash, @text, @length, @vocab_id, 1);
                SELECT SCOPE_IDENTITY();
            ";

            using (var insertCmd = new SqlCommand(insertSql, connection))
            {
                insertCmd.Parameters.AddWithValue("@hash", tokenHash);
                insertCmd.Parameters.AddWithValue("@text", tokenText);
                insertCmd.Parameters.AddWithValue("@length", tokenText.Length);
                insertCmd.Parameters.AddWithValue("@vocab_id", (object?)vocabId ?? DBNull.Value);
                var result = await insertCmd.ExecuteScalarAsync(cancellationToken);
                return Convert.ToInt64(result);
            }
        }

        private async Task IncrementTokenReferenceAsync(SqlConnection connection, byte[] tokenHash, CancellationToken cancellationToken)
        {
            var sql = @"
                UPDATE dbo.AtomicTextTokens
                SET reference_count = reference_count + 1,
                    last_referenced = SYSUTCDATETIME()
                WHERE token_hash = @hash;
            ";

            using var cmd = new SqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@hash", tokenHash);
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }

        // =============================================
        // HASHING UTILITIES
        // =============================================

        private byte[] ComputePixelHash(byte r, byte g, byte b, byte a)
        {
            using var sha256 = SHA256.Create();
            return sha256.ComputeHash(new byte[] { r, g, b, a });
        }

        private byte[] ComputeAudioSampleHash(short amplitude)
        {
            using var sha256 = SHA256.Create();
            return sha256.ComputeHash(BitConverter.GetBytes(amplitude));
        }

        private byte[] ComputeFloatHash(float value)
        {
            using var sha256 = SHA256.Create();
            return sha256.ComputeHash(BitConverter.GetBytes(value));
        }

        private byte[] ComputeTextHash(string text)
        {
            using var sha256 = SHA256.Create();
            return sha256.ComputeHash(Encoding.UTF8.GetBytes(text));
        }
    }
}
