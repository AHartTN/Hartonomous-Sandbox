using System.Diagnostics;
using Hartonomous.Core.Interfaces.Ingestion;
using Microsoft.Extensions.Logging;

namespace Hartonomous.Infrastructure.Atomizers;

/// <summary>
/// Base class for atomizers providing common functionality and eliminating DRY violations.
/// Handles logging, timing, error handling, and common atomization patterns.
/// </summary>
public abstract class BaseAtomizer<TInput> : IAtomizer<TInput>
{
    protected readonly ILogger Logger;
    protected const int MaxAtomSize = 64;

    protected BaseAtomizer(ILogger logger)
    {
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets the priority of this atomizer for content type resolution.
    /// Higher values indicate higher priority.
    /// </summary>
    public abstract int Priority { get; }

    /// <summary>
    /// Determines if this atomizer can handle the specified content type and file extension.
    /// </summary>
    /// <param name="contentType">The MIME content type.</param>
    /// <param name="fileExtension">The file extension (without dot).</param>
    /// <returns>True if this atomizer can handle the content, false otherwise.</returns>
    public abstract bool CanHandle(string contentType, string? fileExtension);

    /// <summary>
    /// Atomizes the input data into atoms with common error handling and logging.
    /// </summary>
    /// <param name="input">The input data to atomize.</param>
    /// <param name="source">Metadata about the source of the input.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The atomization result.</returns>
    public async Task<AtomizationResult> AtomizeAsync(TInput input, SourceMetadata source, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var atoms = new List<AtomData>();
        var compositions = new List<AtomComposition>();
        var warnings = new List<string>();

        try
        {
            Logger.LogInformation("Starting atomization of {FileName} ({ContentType})", source.FileName, source.ContentType);

            await AtomizeCoreAsync(input, source, atoms, compositions, warnings, cancellationToken);

            stopwatch.Stop();

            var uniqueHashes = atoms.Select(a => Convert.ToBase64String(a.ContentHash)).Distinct().Count();

            var processingInfo = new ProcessingMetadata
            {
                TotalAtoms = atoms.Count,
                UniqueAtoms = uniqueHashes,
                DurationMs = stopwatch.ElapsedMilliseconds,
                AtomizerType = GetType().Name,
                DetectedFormat = GetDetectedFormat(),
                Warnings = warnings.Count > 0 ? warnings : null
            };

            Logger.LogInformation(
                "Completed atomization of {FileName}: {TotalAtoms} atoms, {UniqueAtoms} unique, {DurationMs}ms",
                source.FileName,
                processingInfo.TotalAtoms,
                processingInfo.UniqueAtoms,
                processingInfo.DurationMs);

            return new AtomizationResult
            {
                Atoms = atoms,
                Compositions = compositions,
                ProcessingInfo = processingInfo
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            Logger.LogError(ex, "Atomization failed for {FileName} after {DurationMs}ms", source.FileName, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    /// <summary>
    /// Core atomization logic to be implemented by derived classes.
    /// </summary>
    /// <param name="input">The input data to atomize.</param>
    /// <param name="source">Metadata about the source of the input.</param>
    /// <param name="atoms">List to populate with created atoms.</param>
    /// <param name="compositions">List to populate with atom compositions.</param>
    /// <param name="warnings">List to populate with warnings.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    protected abstract Task AtomizeCoreAsync(
        TInput input,
        SourceMetadata source,
        List<AtomData> atoms,
        List<AtomComposition> compositions,
        List<string> warnings,
        CancellationToken cancellationToken);

    /// <summary>
    /// Gets the detected format description for logging.
    /// </summary>
    /// <returns>A string describing the detected format.</returns>
    protected abstract string GetDetectedFormat();

    /// <summary>
    /// Creates a file-level metadata atom with common properties.
    /// </summary>
    /// <param name="input">The input data.</param>
    /// <param name="source">Source metadata.</param>
    /// <param name="atoms">List to add the atom to.</param>
    /// <returns>The content hash of the created atom.</returns>
    protected byte[] CreateFileMetadataAtom(TInput input, SourceMetadata source, List<AtomData> atoms)
    {
        var fileHash = CreateContentHash(GetFileMetadataBytes(input, source));
        var fileMetadataBytes = GetFileMetadataBytes(input, source);

        var fileAtom = new AtomData
        {
            AtomicValue = fileMetadataBytes.Length <= MaxAtomSize ? fileMetadataBytes : fileMetadataBytes.Take(MaxAtomSize).ToArray(),
            ContentHash = fileHash,
            Modality = GetModality(),
            Subtype = "file-metadata",
            ContentType = source.ContentType,
            CanonicalText = GetCanonicalFileText(input, source),
            Metadata = GetFileMetadataJson(input, source)
        };

        atoms.Add(fileAtom);
        return fileHash;
    }

    /// <summary>
    /// Creates a content hash for the given data.
    /// </summary>
    /// <param name="data">The data to hash.</param>
    /// <returns>The SHA256 hash.</returns>
    protected static byte[] CreateContentHash(byte[] data)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        return sha256.ComputeHash(data);
    }

    /// <summary>
    /// Gets the modality string for this atomizer type.
    /// </summary>
    /// <returns>The modality string.</returns>
    protected abstract string GetModality();

    /// <summary>
    /// Gets the file metadata as bytes for hashing.
    /// </summary>
    /// <param name="input">The input data.</param>
    /// <param name="source">Source metadata.</param>
    /// <returns>Byte representation of file metadata.</returns>
    protected abstract byte[] GetFileMetadataBytes(TInput input, SourceMetadata source);

    /// <summary>
    /// Gets the canonical text representation for file metadata.
    /// </summary>
    /// <param name="input">The input data.</param>
    /// <param name="source">Source metadata.</param>
    /// <returns>Canonical text string.</returns>
    protected abstract string GetCanonicalFileText(TInput input, SourceMetadata source);

    /// <summary>
    /// Gets the metadata JSON string for the file atom.
    /// </summary>
    /// <param name="input">The input data.</param>
    /// <param name="source">Source metadata.</param>
    /// <returns>JSON metadata string.</returns>
    protected abstract string GetFileMetadataJson(TInput input, SourceMetadata source);
}