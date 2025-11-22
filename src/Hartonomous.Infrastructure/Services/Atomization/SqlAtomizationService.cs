using Hartonomous.Core.Configuration;
using Hartonomous.Core.Interfaces.Atomization;
using Hartonomous.Infrastructure.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Hartonomous.Infrastructure.Services.Atomization;

/// <summary>
/// SQL Server implementation of atomization service.
/// Provides bulk atom ingestion, model ingestion, and modality-specific atomization.
/// All operations use stored procedures for optimal performance and governance.
/// </summary>
public sealed class SqlAtomizationService : IAtomizationService
{
    private readonly ISqlConnectionFactory _connectionFactory;
    private readonly ILogger<SqlAtomizationService> _logger;

    public SqlAtomizationService(
        ILogger<SqlAtomizationService> logger,
        ISqlConnectionFactory connectionFactory)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    #region Code Atomization

    public async Task AtomizeCodeAsync(
        long atomId,
        int tenantId = 0,
        string language = "csharp",
        bool debug = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(atomId);

        _logger.LogInformation(
            "AtomizeCode: AtomId {AtomId}, Language {Language}, TenantId {TenantId}",
            atomId, language, tenantId);

        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        await using var command = new SqlCommand("dbo.sp_AtomizeCode", connection)
        {
            CommandType = CommandType.StoredProcedure,
            CommandTimeout = 120
        };

        command.Parameters.AddWithValue("@AtomId", atomId);
        command.Parameters.AddWithValue("@TenantId", tenantId);
        command.Parameters.AddWithValue("@Language", language);
        command.Parameters.AddWithValue("@Debug", debug);

        await command.ExecuteNonQueryAsync(cancellationToken);

        _logger.LogInformation("Code atomization completed: AtomId {AtomId}", atomId);
    }

    #endregion

    #region Text Atomization

    public async Task AtomizeTextAsync(
        long atomId,
        int tenantId = 0,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(atomId);

        _logger.LogInformation(
            "AtomizeText: AtomId {AtomId}, TenantId {TenantId}",
            atomId, tenantId);

        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        await using var command = new SqlCommand("dbo.sp_AtomizeText_Governed", connection)
        {
            CommandType = CommandType.StoredProcedure,
            CommandTimeout = 300 // Text can be large
        };

        command.Parameters.AddWithValue("@ParentAtomId", atomId);
        command.Parameters.AddWithValue("@TenantId", tenantId);

        await command.ExecuteNonQueryAsync(cancellationToken);

        _logger.LogInformation("Text atomization completed: AtomId {AtomId}", atomId);
    }

    #endregion

    #region Image Atomization

    public async Task AtomizeImageAsync(
        long atomId,
        int tenantId = 0,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(atomId);

        _logger.LogInformation(
            "AtomizeImage: AtomId {AtomId}, TenantId {TenantId}",
            atomId, tenantId);

        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        await using var command = new SqlCommand("dbo.sp_AtomizeImage_Governed", connection)
        {
            CommandType = CommandType.StoredProcedure,
            CommandTimeout = 600 // Images can be very large
        };

        command.Parameters.AddWithValue("@ParentAtomId", atomId);
        command.Parameters.AddWithValue("@TenantId", tenantId);

        await command.ExecuteNonQueryAsync(cancellationToken);

        _logger.LogInformation("Image atomization completed: AtomId {AtomId}", atomId);
    }

    #endregion

    #region Model Atomization

    public async Task AtomizeModelAsync(
        long atomId,
        int tenantId = 0,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(atomId);

        _logger.LogInformation(
            "AtomizeModel: AtomId {AtomId}, TenantId {TenantId}",
            atomId, tenantId);

        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        await using var command = new SqlCommand("dbo.sp_AtomizeModel_Governed", connection)
        {
            CommandType = CommandType.StoredProcedure,
            CommandTimeout = 1800 // Models can be massive (30 minutes)
        };

        command.Parameters.AddWithValue("@ParentAtomId", atomId);
        command.Parameters.AddWithValue("@TenantId", tenantId);

        await command.ExecuteNonQueryAsync(cancellationToken);

        _logger.LogInformation("Model atomization completed: AtomId {AtomId}", atomId);
    }

    #endregion

    #region Tokenization

    public async Task<int[]> TokenizeTextAsync(
        string text,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);

        _logger.LogInformation("TokenizeText: Length {Length}", text.Length);

        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        await using var command = new SqlCommand("dbo.sp_TokenizeText", connection)
        {
            CommandType = CommandType.StoredProcedure,
            CommandTimeout = 60
        };

        command.Parameters.AddWithValue("@Text", text);

        var result = await command.ExecuteScalarAsync(cancellationToken);

        if (result is string tokenIdsJson)
        {
            var tokenIds = JsonConvert.DeserializeObject<int[]>(tokenIdsJson)
                ?? Array.Empty<int>();

            _logger.LogInformation("Tokenization completed: {TokenCount} tokens", tokenIds.Length);
            return tokenIds;
        }

        _logger.LogWarning("Tokenization returned no results");
        return Array.Empty<int>();
    }

    #endregion

    #region Embedding

    public async Task<(byte[] Embedding, int Dimension)> TextToEmbeddingAsync(
        string text,
        string? modelName = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);

        _logger.LogInformation(
            "TextToEmbedding: Length {Length}, Model {Model}",
            text.Length, modelName ?? "default");

        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        await using var command = new SqlCommand("dbo.sp_TextToEmbedding", connection)
        {
            CommandType = CommandType.StoredProcedure,
            CommandTimeout = 120
        };

        command.Parameters.AddWithValue("@Text", text);
        command.Parameters.AddWithValue("@ModelName", modelName ?? (object)DBNull.Value);

        var embeddingParam = new SqlParameter("@Embedding", SqlDbType.VarBinary, -1)
        {
            Direction = ParameterDirection.Output
        };
        command.Parameters.Add(embeddingParam);

        var dimensionParam = new SqlParameter("@Dimension", SqlDbType.Int)
        {
            Direction = ParameterDirection.Output
        };
        command.Parameters.Add(dimensionParam);

        await command.ExecuteNonQueryAsync(cancellationToken);

        var embedding = (byte[])(embeddingParam.Value ?? Array.Empty<byte>());
        var dimension = (int)(dimensionParam.Value ?? 0);

        _logger.LogInformation(
            "Embedding generated: Dimension {Dimension}, Size {Size} bytes",
            dimension, embedding.Length);

        return (embedding, dimension);
    }

    #endregion
}
