using System.Diagnostics;
using System.Security.Cryptography;
using Hartonomous.Core.Interfaces.Ingestion;
using Microsoft.Extensions.Logging;

namespace Hartonomous.Infrastructure.Atomizers;

/// <summary>
/// Base class for atomizers providing common functionality and eliminating DRY violations.
/// Handles logging, timing, error handling, and common atomization patterns.
/// Ensures proper Atom/AtomRelation structure compliance with 64-byte AtomicValue constraint.
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
    /// Creates a file-level metadata atom with proper 64-byte AtomicValue handling.
    /// Overflow content is stored in CanonicalText instead of truncation.
    /// </summary>
    /// <param name="input">The input data.</param>
    /// <param name="source">Source metadata.</param>
    /// <param name="atoms">List to add the atom to.</param>
    /// <returns>The content hash of the created atom.</returns>
    protected byte[] CreateFileMetadataAtom(TInput input, SourceMetadata source, List<AtomData> atoms)
    {
        var fileMetadataBytes = GetFileMetadataBytes(input, source);
        var fileHash = CreateContentHash(fileMetadataBytes);

        // Handle 64-byte AtomicValue constraint properly
        byte[] atomicValue;
        string? canonicalText = null;
        string? metadata = GetFileMetadataJson(input, source);

        if (fileMetadataBytes.Length <= MaxAtomSize)
        {
            // Fits in AtomicValue - store directly
            atomicValue = fileMetadataBytes;
        }
        else
        {
            // Overflow: Store fingerprint in AtomicValue, full content in CanonicalText
            atomicValue = ComputeFingerprint(fileMetadataBytes);
            canonicalText = GetCanonicalFileText(input, source);
            
            // Add overflow metadata
            metadata = MergeJsonMetadata(metadata, new
            {
                overflow = true,
                originalSize = fileMetadataBytes.Length,
                fingerprintAlgorithm = "SHA256-Truncated-64"
            });
        }

        var fileAtom = new AtomData
        {
            AtomicValue = atomicValue,
            ContentHash = fileHash,
            Modality = GetModality(),
            Subtype = "file-metadata",
            ContentType = source.ContentType,
            CanonicalText = canonicalText ?? GetCanonicalFileText(input, source),
            Metadata = metadata
        };

        atoms.Add(fileAtom);
        return fileHash;
    }

    /// <summary>
    /// Creates an atom with proper 64-byte AtomicValue handling.
    /// Automatically handles overflow by storing full content in CanonicalText.
    /// </summary>
    /// <param name="content">The content bytes to atomize.</param>
    /// <param name="modality">The modality (text, code, image, etc.).</param>
    /// <param name="subtype">The subtype (chunk, token, dimension, etc.).</param>
    /// <param name="canonicalText">Canonical text representation.</param>
    /// <param name="metadata">Optional metadata JSON.</param>
    /// <param name="atoms">List to add the atom to.</param>
    /// <returns>The content hash of the created atom.</returns>
    protected byte[] CreateContentAtom(
        byte[] content,
        string modality,
        string? subtype,
        string? canonicalText,
        string? metadata,
        List<AtomData> atoms)
    {
        var contentHash = CreateContentHash(content);

        byte[] atomicValue;
        string? finalCanonicalText = canonicalText;
        string? finalMetadata = metadata;

        if (content.Length <= MaxAtomSize)
        {
            // Fits in AtomicValue
            atomicValue = content;
        }
        else
        {
            // Overflow: Store fingerprint in AtomicValue, full content in CanonicalText
            atomicValue = ComputeFingerprint(content);
            
            // If no canonical text provided, store as Base64
            if (string.IsNullOrEmpty(finalCanonicalText))
            {
                finalCanonicalText = Convert.ToBase64String(content);
            }
            
            // Add overflow metadata
            finalMetadata = MergeJsonMetadata(metadata, new
            {
                overflow = true,
                originalSize = content.Length,
                fingerprintAlgorithm = "SHA256-Truncated-64",
                encoding = string.IsNullOrEmpty(canonicalText) ? "base64" : "utf8"
            });
        }

        var atom = new AtomData
        {
            AtomicValue = atomicValue,
            ContentHash = contentHash,
            Modality = modality,
            Subtype = subtype,
            CanonicalText = finalCanonicalText,
            Metadata = finalMetadata
        };

        atoms.Add(atom);
        return contentHash;
    }

    /// <summary>
    /// Creates an AtomComposition linking parent and child atoms with spatial positioning.
    /// Uses the correct schema: ParentAtomHash, ComponentAtomHash, SequenceIndex, Position.
    /// </summary>
    /// <param name="parentHash">Parent atom content hash.</param>
    /// <param name="childHash">Child/component atom content hash.</param>
    /// <param name="sequenceIndex">Sequential index for ordered relationships.</param>
    /// <param name="compositions">List to add the composition to.</param>
    /// <param name="x">Spatial X coordinate (position in structure).</param>
    /// <param name="y">Spatial Y coordinate (value/importance).</param>
    /// <param name="z">Spatial Z coordinate (layer/depth).</param>
    /// <param name="m">Spatial M coordinate (measure/weight).</param>
    protected void CreateAtomComposition(
        byte[] parentHash,
        byte[] childHash,
        long sequenceIndex,
        List<AtomComposition> compositions,
        double x = 0.0,
        double y = 0.0,
        double z = 0.0,
        double m = 0.0)
    {
        var composition = new AtomComposition
        {
            ParentAtomHash = parentHash,
            ComponentAtomHash = childHash,
            SequenceIndex = sequenceIndex,
            Position = new SpatialPosition
            {
                X = x,
                Y = y,
                Z = z,
                M = m
            }
        };

        compositions.Add(composition);
    }

    /// <summary>
    /// Computes a 64-byte fingerprint for content larger than MaxAtomSize.
    /// Uses SHA256 hash (32 bytes) + first 32 bytes of content.
    /// </summary>
    /// <param name="content">The content to fingerprint.</param>
    /// <returns>64-byte fingerprint.</returns>
    protected static byte[] ComputeFingerprint(byte[] content)
    {
        var fingerprint = new byte[MaxAtomSize];
        
        // First 32 bytes: SHA256 hash
        using (var sha256 = SHA256.Create())
        {
            var hash = sha256.ComputeHash(content);
            Array.Copy(hash, 0, fingerprint, 0, 32);
        }
        
        // Last 32 bytes: First 32 bytes of content (or padded zeros)
        int copyLength = Math.Min(32, content.Length);
        Array.Copy(content, 0, fingerprint, 32, copyLength);
        
        return fingerprint;
    }

    /// <summary>
    /// Creates a content hash for the given data using SHA256.
    /// </summary>
    /// <param name="data">The data to hash.</param>
    /// <returns>The SHA256 hash (32 bytes).</returns>
    protected static byte[] CreateContentHash(byte[] data)
    {
        using var sha256 = SHA256.Create();
        return sha256.ComputeHash(data);
    }

    /// <summary>
    /// Merges additional properties into existing JSON metadata.
    /// </summary>
    /// <param name="existingJson">Existing JSON string (may be null).</param>
    /// <param name="additionalProperties">Properties to merge.</param>
    /// <returns>Merged JSON string.</returns>
    protected static string MergeJsonMetadata(string? existingJson, object additionalProperties)
    {
        try
        {
            var options = new System.Text.Json.JsonSerializerOptions 
            { 
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase 
            };

            var additional = System.Text.Json.JsonSerializer.Serialize(additionalProperties, options);

            if (string.IsNullOrEmpty(existingJson))
            {
                return additional;
            }

            // Simple merge: Parse both, combine, serialize
            using var existingDoc = System.Text.Json.JsonDocument.Parse(existingJson);
            using var additionalDoc = System.Text.Json.JsonDocument.Parse(additional);

            var merged = new Dictionary<string, object>();

            // Add existing properties
            foreach (var prop in existingDoc.RootElement.EnumerateObject())
            {
                merged[prop.Name] = prop.Value.Clone();
            }

            // Overwrite with additional properties
            foreach (var prop in additionalDoc.RootElement.EnumerateObject())
            {
                merged[prop.Name] = prop.Value.Clone();
            }

            return System.Text.Json.JsonSerializer.Serialize(merged, options);
        }
        catch
        {
            // Fallback: Return additional properties only
            return System.Text.Json.JsonSerializer.Serialize(additionalProperties);
        }
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