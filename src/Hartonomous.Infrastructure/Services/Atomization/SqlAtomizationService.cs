using Azure.Core;
using Azure.Identity;
using Hartonomous.Core.Configuration;
using Hartonomous.Core.Interfaces.Atomization;
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
    private readonly string _connectionString;
    private readonly TokenCredential _credential;
    private readonly ILogger<SqlAtomizationService> _logger;

    public SqlAtomizationService(
        ILogger<SqlAtomizationService> logger,
        IOptions<DatabaseOptions> options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        var databaseOptions = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _connectionString = databaseOptions.HartonomousDb;
        _credential = new DefaultAzureCredential();
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

        await using var connection = new SqlConnection(_connectionString);
        await SetupConnectionAsync(connection, cancellationToken);

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

        await using var connection = new SqlConnection(_connectionString);
        await SetupConnectionAsync(connection, cancellationToken);

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

        await using var connection = new SqlConnection(_connectionString);
        await SetupConnectionAsync(connection, cancellationToken);

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

        await using var connection = new SqlConnection(_connectionString);
        await SetupConnectionAsync(connection, cancellationToken);

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

        await using var connection = new SqlConnection(_connectionString);
        await SetupConnectionAsync(connection, cancellationToken);

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

        await using var connection = new SqlConnection(_connectionString);
        await SetupConnectionAsync(connection, cancellationToken);

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

    #region Helper Methods

    private async Task SetupConnectionAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        // Use managed identity if no password in connection string
        if (!_connectionString.Contains("Password=", StringComparison.OrdinalIgnoreCase) &&
            !_connectionString.Contains("Integrated Security=true", StringComparison.OrdinalIgnoreCase))
        {
            var tokenRequestContext = new TokenRequestContext(["https://database.windows.net/.default"]);
            var token = await _credential.GetTokenAsync(tokenRequestContext, cancellationToken);
            connection.AccessToken = token.Token;
        }

        await connection.OpenAsync(cancellationToken);
    }

    #endregion
}
