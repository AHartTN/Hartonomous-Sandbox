using Hartonomous.Core.Exceptions;
using Hartonomous.Core.Interfaces.Ingestion;
using Hartonomous.Core.Services;
using Hartonomous.Data.Entities;
using Hartonomous.Data.Entities.Entities;
using Microsoft.Extensions.Logging;

namespace Hartonomous.Infrastructure.Services;

/// <summary>
/// Implementation of IIngestionService using direct DbContext access.
/// Follows Microsoft pattern: Services use DbContext directly (no repository layer).
/// </summary>
public class IngestionService : IIngestionService
{
    private readonly HartonomousDbContext _context; // Direct DbContext injection
    private readonly IFileTypeDetector _fileTypeDetector;
    private readonly IEnumerable<IAtomizer<byte[]>> _atomizers;
    private readonly ILogger<IngestionService> _logger;

    public IngestionService(
        HartonomousDbContext context, // Direct DbContext (no repository)
        IFileTypeDetector fileTypeDetector,
        IEnumerable<IAtomizer<byte[]>> atomizers,
        ILogger<IngestionService> logger)
    {
        _context = context;
        _fileTypeDetector = fileTypeDetector;
        _atomizers = atomizers;
        _logger = logger;
    }

    public async Task<IngestionResult> IngestFileAsync(byte[] fileData, string fileName, int tenantId)
    {
        // Validation - throw exceptions (Microsoft pattern, not Result<T>)
        if (fileData == null || fileData.Length == 0)
            throw new ArgumentException("File cannot be empty", nameof(fileData));

        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("Filename cannot be empty", nameof(fileName));

        // Detect file type
        var fileType = _fileTypeDetector.Detect(fileData, fileName);
        if (fileType.Category == FileCategory.Unknown)
            throw new InvalidFileFormatException($"Unsupported file format: {fileName}");

        _logger.LogInformation(
            "Ingesting file: {FileName}, Type: {ContentType}, Size: {Size} bytes, Tenant: {TenantId}",
            fileName, fileType.ContentType, fileData.Length, tenantId);

        // Find appropriate atomizers for this file type
        var supportedAtomizers = _atomizers
            .Where(a => a.CanHandle(fileType.ContentType, fileType.Extension))
            .OrderByDescending(a => a.Priority)
            .ToList();
            
        if (!supportedAtomizers.Any())
            throw new InvalidFileFormatException($"No atomizer found for file type: {fileType.ContentType}");

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
            
            _logger.LogInformation(
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
        await _context.SaveChangesAsync(); // Unit of Work pattern

        _logger.LogInformation(
            "Ingestion complete: {FileName} → {AtomCount} atoms created, Tenant: {TenantId}",
            fileName, allAtoms.Count, tenantId);

        return new IngestionResult
        {
            Success = true,
            ItemsProcessed = allAtoms.Count,
            Message = $"Successfully ingested {allAtoms.Count} atoms from {fileName}"
        };
    }

    public Task<IngestionResult> IngestUrlAsync(string url, int tenantId)
    {
        // TODO: Implement URL ingestion
        throw new NotImplementedException("URL ingestion not yet implemented");
    }

    public Task<IngestionResult> IngestDatabaseAsync(string connectionString, string query, int tenantId)
    {
        // TODO: Implement database ingestion
        throw new NotImplementedException("Database ingestion not yet implemented");
    }
}
