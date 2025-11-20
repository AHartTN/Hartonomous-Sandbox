namespace Hartonomous.Core.Services;

/// <summary>
/// Service for ingesting and atomizing data from various sources.
/// Business logic layer for data ingestion operations.
/// </summary>
public interface IIngestionService
{
    /// <summary>
    /// Ingests a file and atomizes its contents.
    /// </summary>
    /// <param name="fileData">Raw file bytes</param>
    /// <param name="fileName">Original filename</param>
    /// <param name="tenantId">Tenant ID for multi-tenant isolation</param>
    /// <returns>Ingestion result with item count</returns>
    Task<IngestionResult> IngestFileAsync(byte[] fileData, string fileName, int tenantId);

    /// <summary>
    /// Ingests content from a URL.
    /// </summary>
    /// <param name="url">URL to ingest</param>
    /// <param name="tenantId">Tenant ID</param>
    /// <returns>Ingestion result</returns>
    Task<IngestionResult> IngestUrlAsync(string url, int tenantId);

    /// <summary>
    /// Ingests data from a database connection.
    /// </summary>
    /// <param name="connectionString">Database connection string</param>
    /// <param name="query">Query to execute</param>
    /// <param name="tenantId">Tenant ID</param>
    /// <returns>Ingestion result</returns>
    Task<IngestionResult> IngestDatabaseAsync(string connectionString, string query, int tenantId);
}
