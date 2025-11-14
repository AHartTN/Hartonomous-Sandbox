using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Hartonomous.Core.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace Hartonomous.Infrastructure.Services.Tenant;

/// <summary>
/// Database-backed tenant GUID-to-INT mapping service.
/// Provides safe, consistent mapping with ACID guarantees.
/// Replaces unsafe GetHashCode() approach.
/// </summary>
public class TenantMappingService : ITenantMappingService
{
    private readonly string _connectionString;
    private readonly ILogger<TenantMappingService> _logger;

    public TenantMappingService(
        string connectionString,
        ILogger<TenantMappingService> logger)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<int?> ResolveTenantGuidAsync(
        Guid tenantGuid,
        string tenantName = null,
        bool autoRegister = true,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            await using var command = new SqlCommand("dbo.sp_ResolveTenantGuid", connection)
            {
                CommandType = CommandType.StoredProcedure,
                CommandTimeout = 30
            };

            command.Parameters.AddWithValue("@TenantGuid", tenantGuid);
            command.Parameters.AddWithValue("@TenantName", tenantName ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@AutoRegister", autoRegister);

            var tenantIdParam = new SqlParameter("@TenantId", SqlDbType.Int)
            {
                Direction = ParameterDirection.Output
            };
            command.Parameters.Add(tenantIdParam);

            await command.ExecuteNonQueryAsync(cancellationToken);

            if (tenantIdParam.Value == DBNull.Value)
            {
                _logger.LogWarning(
                    "Tenant GUID {TenantGuid} not found and auto-registration disabled",
                    tenantGuid);
                return null;
            }

            var tenantId = (int)tenantIdParam.Value;

            _logger.LogDebug(
                "Resolved tenant GUID {TenantGuid} to tenant ID {TenantId}",
                tenantGuid, tenantId);

            return tenantId;
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex,
                "Failed to resolve tenant GUID {TenantGuid}: {ErrorMessage}",
                tenantGuid, ex.Message);
            throw;
        }
    }

    public async Task<Guid?> GetTenantGuidAsync(
        int tenantId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            await using var command = new SqlCommand(@"
                SELECT TenantGuid
                FROM dbo.TenantGuidMapping WITH (NOLOCK)
                WHERE TenantId = @TenantId
                  AND IsActive = 1",
                connection);

            command.Parameters.AddWithValue("@TenantId", tenantId);

            var result = await command.ExecuteScalarAsync(cancellationToken);

            if (result == null || result == DBNull.Value)
            {
                _logger.LogWarning("Tenant ID {TenantId} not found", tenantId);
                return null;
            }

            return (Guid)result;
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex,
                "Failed to get tenant GUID for tenant ID {TenantId}: {ErrorMessage}",
                tenantId, ex.Message);
            throw;
        }
    }
}
