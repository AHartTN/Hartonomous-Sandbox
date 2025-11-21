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
                    // Map properties from atomData to Atom entity
                };

                allAtoms.Add(atom);
            }

            // Save all atoms to the database
            await _context.AddRangeAsync(allAtoms);
            await _context.SaveChangesAsync();

            return new IngestionResult
            {
                Success = true,
                ProcessedAtomCount = allAtoms.Count,
                // Other result properties
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error ingesting file: {FileName}", fileName);
            throw new IngestionException("Error ingesting file", ex);
        }
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
