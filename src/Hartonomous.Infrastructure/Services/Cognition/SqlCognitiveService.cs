using Azure.Core;
using Azure.Identity;
using Hartonomous.Core.Configuration;
using Hartonomous.Core.Interfaces.Cognition;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Hartonomous.Infrastructure.Services.Cognition;

/// <summary>
/// SQL Server implementation of cognitive activation service.
/// Provides neural activation spreading and spatial projection capabilities.
/// </summary>
public sealed class SqlCognitiveService : ICognitiveService
{
    private readonly string _connectionString;
    private readonly TokenCredential _credential;
    private readonly ILogger<SqlCognitiveService> _logger;

    public SqlCognitiveService(
        ILogger<SqlCognitiveService> logger,
        IOptions<DatabaseOptions> options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        var databaseOptions = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _connectionString = databaseOptions.HartonomousDb;
        _credential = new DefaultAzureCredential();
    }

    public async Task<CognitiveActivationResult> ActivateAsync(
        string atomIds,
        float activationThreshold = 0.3f,
        int spreadDepth = 3,
        int tenantId = 0,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(atomIds, nameof(atomIds));

        if (activationThreshold < 0.0f || activationThreshold > 1.0f)
            throw new ArgumentOutOfRangeException(nameof(activationThreshold), "Threshold must be between 0.0 and 1.0");

        if (spreadDepth < 1 || spreadDepth > 10)
            throw new ArgumentOutOfRangeException(nameof(spreadDepth), "SpreadDepth must be between 1 and 10");

        _logger.LogInformation(
            "CognitiveActivation: AtomIds {AtomIds}, Threshold {Threshold}, Depth {Depth}, TenantId {TenantId}",
            atomIds, activationThreshold, spreadDepth, tenantId);

        var sw = System.Diagnostics.Stopwatch.StartNew();

        await using var connection = new SqlConnection(_connectionString);
        await SetupConnectionAsync(connection, cancellationToken);

        await using var command = new SqlCommand("dbo.sp_CognitiveActivation", connection)
        {
            CommandType = CommandType.StoredProcedure,
            CommandTimeout = 90
        };

        command.Parameters.AddWithValue("@AtomIds", atomIds);
        command.Parameters.AddWithValue("@ActivationThreshold", activationThreshold);
        command.Parameters.AddWithValue("@SpreadDepth", spreadDepth);
        command.Parameters.AddWithValue("@TenantId", tenantId);

        // Output parameters
        var activatedCountParam = new SqlParameter("@ActivatedCount", SqlDbType.Int)
        {
            Direction = ParameterDirection.Output
        };
        var totalActivationParam = new SqlParameter("@TotalActivation", SqlDbType.Float)
        {
            Direction = ParameterDirection.Output
        };

        command.Parameters.Add(activatedCountParam);
        command.Parameters.Add(totalActivationParam);

        await command.ExecuteNonQueryAsync(cancellationToken);
        sw.Stop();

        var activatedCount = activatedCountParam.Value is int count ? count : 0;
        var totalActivation = totalActivationParam.Value is double total ? (float)total : 0.0f;

        _logger.LogInformation(
            "CognitiveActivation completed: Activated {Count} atoms, Total {Total:F2}, Duration {Duration}ms",
            activatedCount, totalActivation, sw.ElapsedMilliseconds);

        return new CognitiveActivationResult(
            activatedCount,
            totalActivation,
            (int)sw.ElapsedMilliseconds);
    }

    public async Task<SpatialProjectionResult> ProjectAsync(
        long atomId,
        int dimensions = 3,
        string method = "PCA",
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(atomId, nameof(atomId));

        if (dimensions < 2 || dimensions > 3)
            throw new ArgumentOutOfRangeException(nameof(dimensions), "Dimensions must be 2 or 3");

        var validMethods = new[] { "PCA", "TSNE", "UMAP" };
        if (!validMethods.Contains(method, StringComparer.OrdinalIgnoreCase))
            throw new ArgumentException($"Method must be one of: {string.Join(", ", validMethods)}", nameof(method));

        _logger.LogInformation(
            "SpatialProjection: AtomId {AtomId}, Dimensions {Dimensions}, Method {Method}",
            atomId, dimensions, method);

        await using var connection = new SqlConnection(_connectionString);
        await SetupConnectionAsync(connection, cancellationToken);

        await using var command = new SqlCommand("dbo.sp_ComputeSpatialProjection", connection)
        {
            CommandType = CommandType.StoredProcedure,
            CommandTimeout = 60
        };

        command.Parameters.AddWithValue("@AtomId", atomId);
        command.Parameters.AddWithValue("@TargetDimensions", dimensions);
        command.Parameters.AddWithValue("@ProjectionMethod", method);

        // Output parameters for coordinates
        var xParam = new SqlParameter("@X", SqlDbType.Float) { Direction = ParameterDirection.Output };
        var yParam = new SqlParameter("@Y", SqlDbType.Float) { Direction = ParameterDirection.Output };
        var zParam = new SqlParameter("@Z", SqlDbType.Float) { Direction = ParameterDirection.Output };

        command.Parameters.Add(xParam);
        command.Parameters.Add(yParam);
        command.Parameters.Add(zParam);

        await command.ExecuteNonQueryAsync(cancellationToken);

        var x = xParam.Value is double xVal ? (float)xVal : 0.0f;
        var y = yParam.Value is double yVal ? (float)yVal : 0.0f;
        var z = dimensions == 3 && zParam.Value is double zVal ? (float)zVal : 0.0f;

        var coordinates = dimensions == 3 ? new[] { x, y, z } : new[] { x, y };

        _logger.LogInformation(
            "SpatialProjection completed: AtomId {AtomId}, Coordinates [{Coords}]",
            atomId, string.Join(", ", coordinates.Select(c => c.ToString("F4"))));

        return new SpatialProjectionResult(atomId, coordinates, method);
    }

    private async Task SetupConnectionAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        if (!_connectionString.Contains("Password=", StringComparison.OrdinalIgnoreCase) &&
            !_connectionString.Contains("Integrated Security=true", StringComparison.OrdinalIgnoreCase))
        {
            var tokenRequestContext = new TokenRequestContext(["https://database.windows.net/.default"]);
            var token = await _credential.GetTokenAsync(tokenRequestContext, cancellationToken);
            connection.AccessToken = token.Token;
        }

        await connection.OpenAsync(cancellationToken);
    }
}
