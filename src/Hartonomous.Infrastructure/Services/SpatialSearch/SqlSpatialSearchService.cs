using Azure.Core;
using Azure.Identity;
using Hartonomous.Core.Interfaces.SpatialSearch;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using Newtonsoft.Json;
using System.Data;

namespace Hartonomous.Infrastructure.Services.SpatialSearch;

/// <summary>
/// SQL Server implementation of spatial search using NetTopologySuite for geography operations.
/// Leverages CLR functions for landmark projections and nearest neighbor queries.
/// </summary>
public sealed class SqlSpatialSearchService : ISpatialSearchService
{
    private readonly ILogger<SqlSpatialSearchService> _logger;
    private readonly IConfiguration _configuration;
    private readonly TokenCredential _credential;
    private readonly SqlServerBytesReader _spatialReader;

    public SqlSpatialSearchService(
        ILogger<SqlSpatialSearchService> logger,
        IConfiguration configuration)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _credential = new DefaultAzureCredential();
        _spatialReader = new SqlServerBytesReader { IsGeography = true };
    }

    public async Task<IEnumerable<SpatialAtom>> FindNearestAtomsAsync(
        Point location,
        double radiusMeters,
        int maxResults = 100,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(location);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxResults);

        _logger.LogInformation(
            "Executing spatial search for nearest atoms at ({Latitude}, {Longitude}), max results: {MaxResults}, radius: {Radius}m",
            location.Y, location.X, maxResults, radiusMeters);

        var connectionString = _configuration.GetConnectionString("HartonomousDb")
            ?? throw new InvalidOperationException("Connection string 'HartonomousDb' not found.");

        await using var connection = new SqlConnection(connectionString);

        var tokenRequestContext = new TokenRequestContext(["https://database.windows.net/.default"]);
        var token = await _credential.GetTokenAsync(tokenRequestContext, cancellationToken);
        connection.AccessToken = token.Token;

        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand("dbo.sp_FindNearestAtoms", connection)
        {
            CommandType = CommandType.StoredProcedure,
            CommandTimeout = 60
        };

        // Convert Point to SQL Server geography
        var writer = new SqlServerBytesWriter { IsGeography = true };
        var geographyBytes = writer.Write(location);

        command.Parameters.Add(new SqlParameter("@location", SqlDbType.Udt)
        {
            UdtTypeName = "geography",
            Value = geographyBytes
        });
        command.Parameters.Add(new SqlParameter("@maxResults", SqlDbType.Int) { Value = maxResults });
        command.Parameters.Add(new SqlParameter("@radiusMeters", SqlDbType.Float) { Value = radiusMeters });

        var results = new List<SpatialAtom>();

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var atomId = reader.GetInt64(reader.GetOrdinal("AtomId"));
            var distanceMeters = reader.GetDouble(reader.GetOrdinal("DistanceMeters"));
            var atomLocationBytes = reader.GetSqlBytes(reader.GetOrdinal("Location")).Value;
            var atomLocation = _spatialReader.Read(atomLocationBytes) as Point;
            var atomData = reader.IsDBNull(reader.GetOrdinal("AtomData")) ? null : reader.GetString(reader.GetOrdinal("AtomData"));
            var createdAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"));

            results.Add(new SpatialAtom
            {
                AtomId = atomId,
                Location = atomLocation ?? throw new InvalidOperationException("Failed to read atom location."),
                DistanceMeters = distanceMeters,
                AtomData = atomData,
                CreatedAt = createdAt
            });
        }

        _logger.LogInformation("Spatial search completed. Found {ResultCount} atoms.", results.Count);

        return results;
    }

    public async Task<IEnumerable<SpatialAtom>> FindKNearestAtomsAsync(
        Point location,
        int k,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(location);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(k);

        _logger.LogInformation(
            "Executing k-NN search for {K} nearest atoms at ({Latitude}, {Longitude})",
            k, location.Y, location.X);

        var connectionString = _configuration.GetConnectionString("HartonomousDb")
            ?? throw new InvalidOperationException("Connection string 'HartonomousDb' not found.");

        await using var connection = new SqlConnection(connectionString);

        var tokenRequestContext = new TokenRequestContext(["https://database.windows.net/.default"]);
        var token = await _credential.GetTokenAsync(tokenRequestContext, cancellationToken);
        connection.AccessToken = token.Token;

        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand("dbo.sp_FindKNearestAtoms", connection)
        {
            CommandType = CommandType.StoredProcedure,
            CommandTimeout = 60
        };

        var writer = new SqlServerBytesWriter { IsGeography = true };
        var geographyBytes = writer.Write(location);

        command.Parameters.Add(new SqlParameter("@location", SqlDbType.Udt)
        {
            UdtTypeName = "geography",
            Value = geographyBytes
        });
        command.Parameters.Add(new SqlParameter("@k", SqlDbType.Int) { Value = k });

        var results = new List<SpatialAtom>();

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var atomId = reader.GetInt64(reader.GetOrdinal("AtomId"));
            var distanceMeters = reader.GetDouble(reader.GetOrdinal("DistanceMeters"));
            var atomLocationBytes = reader.GetSqlBytes(reader.GetOrdinal("Location")).Value;
            var atomLocation = _spatialReader.Read(atomLocationBytes) as Point;
            var atomData = reader.IsDBNull(reader.GetOrdinal("AtomData")) ? null : reader.GetString(reader.GetOrdinal("AtomData"));
            var createdAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"));

            results.Add(new SpatialAtom
            {
                AtomId = atomId,
                Location = atomLocation ?? throw new InvalidOperationException("Failed to read atom location."),
                DistanceMeters = distanceMeters,
                AtomData = atomData,
                CreatedAt = createdAt
            });
        }

        _logger.LogInformation("k-NN search completed. Found {ResultCount} atoms.", results.Count);

        return results;
    }

    public async Task<IEnumerable<LandmarkProjection>> ProjectOntoLandmarksAsync(
        IEnumerable<long> atomIds,
        IEnumerable<long> landmarkIds,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(atomIds);
        ArgumentNullException.ThrowIfNull(landmarkIds);

        var atomIdList = atomIds.ToList();
        var landmarkIdList = landmarkIds.ToList();

        if (atomIdList.Count == 0) throw new ArgumentException("At least one atom ID required.", nameof(atomIds));
        if (landmarkIdList.Count == 0) throw new ArgumentException("At least one landmark ID required.", nameof(landmarkIds));

        _logger.LogInformation(
            "Projecting {AtomCount} atoms onto {LandmarkCount} landmarks",
            atomIdList.Count, landmarkIdList.Count);

        var connectionString = _configuration.GetConnectionString("HartonomousDb")
            ?? throw new InvalidOperationException("Connection string 'HartonomousDb' not found.");

        await using var connection = new SqlConnection(connectionString);

        var tokenRequestContext = new TokenRequestContext(["https://database.windows.net/.default"]);
        var token = await _credential.GetTokenAsync(tokenRequestContext, cancellationToken);
        connection.AccessToken = token.Token;

        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand("dbo.sp_ProjectOntoLandmarks", connection)
        {
            CommandType = CommandType.StoredProcedure,
            CommandTimeout = 120
        };

        command.Parameters.Add(new SqlParameter("@atomIds", SqlDbType.NVarChar, -1) { Value = string.Join(",", atomIdList) });
        command.Parameters.Add(new SqlParameter("@landmarkIds", SqlDbType.NVarChar, -1) { Value = string.Join(",", landmarkIdList) });

        var results = new List<LandmarkProjection>();

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var atomId = reader.GetInt64(reader.GetOrdinal("AtomId"));
            var coordinatesJson = reader.GetString(reader.GetOrdinal("Coordinates"));
            var coordinates = JsonConvert.DeserializeObject<double[]>(coordinatesJson) ?? Array.Empty<double>();

            results.Add(new LandmarkProjection
            {
                AtomId = atomId,
                Coordinates = coordinates,
                LandmarkIds = landmarkIdList.ToArray()
            });
        }

        _logger.LogInformation("Landmark projection completed. Projected {ResultCount} atoms.", results.Count);

        return results;
    }
}
