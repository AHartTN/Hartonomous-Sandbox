using Hartonomous.Core.Abstracts;
using Hartonomous.Core.Exceptions;
using Hartonomous.Core.Interfaces.BackgroundJob;
using Hartonomous.Core.Interfaces.Ingestion;
using Hartonomous.Core.Services;
using Hartonomous.Core.Validation;
using Hartonomous.Data.Entities;
using Hartonomous.Data.Entities.Entities;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Text.Json;
using Microsoft.Data.SqlClient;

namespace Hartonomous.Infrastructure.Services;

/// <summary>
/// Implementation of IIngestionService using direct DbContext access.
/// Follows Microsoft pattern: Services use DbContext directly (no repository layer).
/// Inherits from ServiceBase for standardized logging, telemetry, and validation.
/// </summary>
public class IngestionService : ServiceBase<IngestionService>, IIngestionService
{
    private readonly HartonomousDbContext _context;
    private readonly IFileTypeDetector _fileTypeDetector;
    private readonly IEnumerable<IAtomizer<byte[]>> _atomizers;
    private readonly IBackgroundJobService _backgroundJobService;
    private readonly TelemetryClient? _telemetry;

    public IngestionService(
        HartonomousDbContext context,
        IFileTypeDetector fileTypeDetector,
        IEnumerable<IAtomizer<byte[]>> atomizers,
        IBackgroundJobService backgroundJobService,
        ILogger<IngestionService> logger,
        TelemetryClient? telemetry = null)
        : base(logger)
    {
        _context = Guard.NotNull(context, nameof(context));
        _fileTypeDetector = Guard.NotNull(fileTypeDetector, nameof(fileTypeDetector));
        _atomizers = Guard.NotNullOrEmpty(atomizers, nameof(atomizers));
        _backgroundJobService = Guard.NotNull(backgroundJobService, nameof(backgroundJobService));
        _telemetry = telemetry;
    }

    public async Task<IngestionResult> IngestFileAsync(byte[] fileData, string fileName, int tenantId)
    {
        return await ExecuteWithTelemetryAsync(
            $"IngestFile ({fileName})",
            async () => await IngestFileInternalAsync(fileData, fileName, tenantId),
            CancellationToken.None);
    }

    private async Task<IngestionResult> IngestFileInternalAsync(byte[] fileData, string fileName, int tenantId)
    {
        // Validation using Guard clauses
        Guard.NotNullOrEmpty(fileData, nameof(fileData));
        Guard.NotNullOrWhiteSpace(fileName, nameof(fileName));
        Guard.Positive(tenantId, nameof(tenantId));

        // Detect file type
        var fileType = _fileTypeDetector.Detect(fileData, fileName);
        if (fileType.Category == FileCategory.Unknown)
        {
            throw new InvalidFileFormatException($"Unsupported file format: {fileName}");
        }

        Logger.LogInformation(
            "Ingesting file: {FileName}, Type: {ContentType}, Size: {Size} bytes, Tenant: {TenantId}",
            fileName, fileType.ContentType, fileData.Length, tenantId);

        // Find appropriate atomizers for this file type
        var supportedAtomizers = _atomizers
            .Where(a => a.CanHandle(fileType.ContentType, fileType.Extension))
            .OrderByDescending(a => a.Priority)
            .ToList();
            
        if (!supportedAtomizers.Any())
        {
            throw new InvalidFileFormatException($"No atomizer found for file type: {fileType.ContentType}");
        }

        var allAtoms = new List<Atom>();

        // Create source metadata
        var sourceMetadata = new SourceMetadata
        {
            FileName = fileName,
            ContentType = fileType.ContentType,
            SizeBytes = fileData.Length,
            TenantId = tenantId,
            SourceType = fileType.Category.ToString()
        };

        // Use the first matching atomizer (highest priority)
        var atomizer = supportedAtomizers.First();
        
        try
        {
            var result = await atomizer.AtomizeAsync(fileData, sourceMetadata);
            
            // Convert AtomData to Atom entities
            foreach (var atomData in result.Atoms)
            {
                var atom = new Atom
                {
                    ContentHash = atomData.ContentHash,
                    AtomicValue = atomData.AtomicValue,
                    Modality = atomData.Modality,
                    Subtype = atomData.Subtype,
                    ContentType = atomData.ContentType,
                    CanonicalText = atomData.CanonicalText,
                    Metadata = atomData.Metadata,
                    TenantId = tenantId
                };
                
                allAtoms.Add(atom);
            }
            
            Logger.LogInformation(
                "Atomizer {AtomizerType} produced {AtomCount} atoms",
                result.ProcessingInfo.AtomizerType, result.Atoms.Count);
        }
        catch (Exception ex)
        {
            throw new AtomizationFailedException(
                $"Failed to atomize {fileName} using {atomizer.GetType().Name}", ex);
        }

        // CRITICAL: Call sp_IngestAtoms to preserve deduplication and Service Broker triggers
        // Do NOT use _context.Atoms.AddRangeAsync - it bypasses kernel logic
        var atomsJson = SerializeAtomsToJson(allAtoms);
        var batchId = await CallSpIngestAtomsAsync(atomsJson, tenantId);

        // ===== FIX 1: TRIGGER EMBEDDING GENERATION =====
        // Queue embedding jobs for all atoms that need embeddings
        var embeddingJobCount = 0;
        foreach (var atom in allAtoms.Where(a => NeedsEmbedding(a.Modality)))
        {
            await _backgroundJobService.CreateJobAsync(
                jobType: "GenerateEmbedding",
                parametersJson: JsonSerializer.Serialize(new 
                { 
                    AtomId = atom.AtomId,
                    TenantId = tenantId,
                    Modality = atom.Modality
                }),
                tenantId: tenantId,
                cancellationToken: CancellationToken.None);
            
            embeddingJobCount++;
        }

        if (embeddingJobCount > 0)
        {
            Logger.LogInformation(
                "Queued {JobCount} embedding generation jobs for batch {BatchId}",
                embeddingJobCount, batchId);
        }
        // ===== END FIX 1 =====

        // Track custom metrics
        _telemetry?.TrackMetric("Atoms.Ingested", allAtoms.Count);
        _telemetry?.TrackMetric("EmbeddingJobs.Queued", embeddingJobCount);
        _telemetry?.TrackEvent("FileIngestionCompleted", new Dictionary<string, string>
        {
            ["FileName"] = fileName,
            ["FileType"] = fileType.ContentType,
            ["AtomCount"] = allAtoms.Count.ToString(),
            ["EmbeddingJobsQueued"] = embeddingJobCount.ToString(),
            ["TenantId"] = tenantId.ToString(),
            ["BatchId"] = batchId.ToString()
        });

        Logger.LogInformation(
            "Ingestion complete: {FileName} → {AtomCount} atoms created, {JobCount} embedding jobs queued, Tenant: {TenantId}, BatchId: {BatchId}",
            fileName, allAtoms.Count, embeddingJobCount, tenantId, batchId);

        return new IngestionResult
        {
            Success = true,
            ItemsProcessed = allAtoms.Count,
            Message = $"Successfully ingested {allAtoms.Count} atoms from {fileName}, queued {embeddingJobCount} embedding jobs"
        };
    }

    /// <summary>
    /// Serialize atoms to JSON format expected by sp_IngestAtoms
    /// </summary>
    private string SerializeAtomsToJson(List<Atom> atoms)
    {
        var atomDtos = atoms.Select(a => new
        {
            AtomicValue = a.AtomicValue,
            CanonicalText = a.CanonicalText,
            Modality = a.Modality,
            Subtype = a.Subtype,
            Metadata = a.Metadata
        });

        return JsonSerializer.Serialize(atomDtos);
    }

    /// <summary>
    /// Call sp_IngestAtoms stored procedure to leverage kernel-level deduplication and Service Broker
    /// </summary>
    private async Task<Guid> CallSpIngestAtomsAsync(string atomsJson, int tenantId)
    {
        var batchIdParam = new SqlParameter("@batchId", SqlDbType.UniqueIdentifier)
        {
            Direction = ParameterDirection.Output
        };

        await _context.Database.ExecuteSqlRawAsync(
            "EXEC dbo.sp_IngestAtoms @atomsJson = {0}, @tenantId = {1}, @batchId = {2} OUTPUT",
            atomsJson,
            tenantId,
            batchIdParam);

        return (Guid)(batchIdParam.Value ?? Guid.Empty);
    }

    public async Task<IngestionResult> IngestUrlAsync(string url, int tenantId)
    {
        return await ExecuteWithTelemetryAsync(
            $"IngestUrl ({url})",
            async () => await IngestUrlInternalAsync(url, tenantId),
            CancellationToken.None);
    }

    private async Task<IngestionResult> IngestUrlInternalAsync(string url, int tenantId)
    {
        // Validation using Guard clauses
        Guard.NotNullOrWhiteSpace(url, nameof(url));
        Guard.Positive(tenantId, nameof(tenantId));

        // Validate URL format
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            throw new ArgumentException($"Invalid URL format: {url}", nameof(url));
        }

        // Security: Only allow HTTP and HTTPS schemes
        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
        {
            throw new InvalidOperationException($"Unsupported URL scheme: {uri.Scheme}. Only HTTP and HTTPS are allowed.");
        }

        Logger.LogInformation(
            "Ingesting URL: {Url}, Tenant: {TenantId}",
            url, tenantId);

        byte[] fileData;
        string fileName;
        string? contentType = null;

        try
        {
            // Download content using HttpClient
            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromMinutes(5); // Configurable timeout
            
            // Set user agent for better server compatibility
            httpClient.DefaultRequestHeaders.Add("User-Agent", "Hartonomous/1.0 (Cognitive Database System)");

            var response = await httpClient.GetAsync(uri);
            response.EnsureSuccessStatusCode();

            // Extract content type and data
            contentType = response.Content.Headers.ContentType?.MediaType;
            fileData = await response.Content.ReadAsByteArrayAsync();

            // Extract filename from URL or Content-Disposition header
            fileName = GetFileNameFromUrl(uri, response.Content.Headers.ContentDisposition?.FileName);

            Logger.LogInformation(
                "Downloaded {Size} bytes from {Url}, ContentType: {ContentType}, FileName: {FileName}",
                fileData.Length, url, contentType ?? "unknown", fileName);
        }
        catch (HttpRequestException ex)
        {
            throw new InvalidOperationException($"Failed to download content from URL: {url}", ex);
        }
        catch (TaskCanceledException ex)
        {
            throw new TimeoutException($"URL download timed out: {url}", ex);
        }

        // Validate downloaded content
        if (fileData == null || fileData.Length == 0)
        {
            throw new InvalidOperationException($"No content retrieved from URL: {url}");
        }

        // Track telemetry
        _telemetry?.TrackEvent("UrlDownloadCompleted", new Dictionary<string, string>
        {
            ["Url"] = url,
            ["ContentType"] = contentType ?? "unknown",
            ["Size"] = fileData.Length.ToString(),
            ["TenantId"] = tenantId.ToString()
        });

        // Delegate to existing file ingestion logic with tenant isolation
        return await IngestFileInternalAsync(fileData, fileName, tenantId);
    }

    /// <summary>
    /// Extracts a meaningful filename from a URL or fallback to generated name.
    /// </summary>
    private static string GetFileNameFromUrl(Uri uri, string? contentDispositionFileName)
    {
        // First, try Content-Disposition header filename
        if (!string.IsNullOrWhiteSpace(contentDispositionFileName))
        {
            return contentDispositionFileName.Trim('"', '\'');
        }

        // Extract from URL path
        var segments = uri.Segments;
        if (segments.Length > 0)
        {
            var lastSegment = segments[^1].Trim('/');
            if (!string.IsNullOrWhiteSpace(lastSegment) && lastSegment.Contains('.'))
            {
                return lastSegment;
            }
        }

        // Fallback: Generate filename from domain and timestamp
        var domain = uri.Host.Replace("www.", "");
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        return $"{domain}_{timestamp}.dat";
    }

    public Task<IngestionResult> IngestDatabaseAsync(string connectionString, string query, int tenantId)
    {
        // TODO: Implement database ingestion
        throw new NotImplementedException("Database ingestion not yet implemented");
    }

    /// <summary>
    /// Determines if an atom needs embedding generation based on its modality.
    /// </summary>
    private static bool NeedsEmbedding(string modality)
    {
        return modality is "text" or "image" or "audio" or "video" or "code";
    }
}
