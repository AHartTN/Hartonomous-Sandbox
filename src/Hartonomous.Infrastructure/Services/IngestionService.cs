using Hartonomous.Core.Abstracts;
using Hartonomous.Core.Exceptions;
using Hartonomous.Core.Interfaces.Ingestion;
using Hartonomous.Core.Services;
using Hartonomous.Core.Validation;
using Hartonomous.Data.Entities;
using Hartonomous.Data.Entities.Entities;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Extensions.Logging;

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
    private readonly TelemetryClient? _telemetry;

    public IngestionService(
        HartonomousDbContext context,
        IFileTypeDetector fileTypeDetector,
        IEnumerable<IAtomizer<byte[]>> atomizers,
        ILogger<IngestionService> logger,
        TelemetryClient? telemetry = null)
        : base(logger)
    {
        _context = Guard.NotNull(context, nameof(context));
        _fileTypeDetector = Guard.NotNull(fileTypeDetector, nameof(fileTypeDetector));
        _atomizers = Guard.NotNullOrEmpty(atomizers, nameof(atomizers));
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

        // Save to database - DbContext = Repository + Unit of Work
        await _context.Atoms.AddRangeAsync(allAtoms);
        await _context.SaveChangesAsync();

        // Track custom metrics
        _telemetry?.TrackMetric("Atoms.Ingested", allAtoms.Count);
        _telemetry?.TrackEvent("FileIngestionCompleted", new Dictionary<string, string>
        {
            ["FileName"] = fileName,
            ["FileType"] = fileType.ContentType,
            ["AtomCount"] = allAtoms.Count.ToString(),
            ["TenantId"] = tenantId.ToString()
        });

        Logger.LogInformation(
            "Ingestion complete: {FileName} → {AtomCount} atoms created, Tenant: {TenantId}",
            fileName, allAtoms.Count, tenantId);

        return new IngestionResult
        {
            Success = true,
            ItemsProcessed = allAtoms.Count,
            Message = $"Successfully ingested {allAtoms.Count} atoms from {fileName}"
        };
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
}
