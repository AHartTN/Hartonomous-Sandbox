using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Hartonomous.Core.Interfaces.Ingestion;

namespace Hartonomous.Infrastructure.Atomizers;

/// <summary>
/// Atomizes archive files (ZIP, TAR, GZ) by extracting contained files for recursive atomization.
/// Creates composition relationships between archive and extracted files.
/// </summary>
public class ArchiveAtomizer : IAtomizer<byte[]>
{
    private const int MaxAtomSize = 64;
    private const int MaxRecursionDepth = 10;
    public int Priority => 30;

    public bool CanHandle(string contentType, string? fileExtension)
    {
        if (contentType == "application/zip" ||
            contentType == "application/x-zip-compressed" ||
            contentType == "application/gzip" ||
            contentType == "application/x-gzip" ||
            contentType == "application/x-tar")
            return true;

        var archiveExtensions = new[] { "zip", "gz", "tar" };
        return fileExtension != null && archiveExtensions.Contains(fileExtension.ToLowerInvariant());
    }

    public async Task<AtomizationResult> AtomizeAsync(byte[] input, SourceMetadata source, CancellationToken cancellationToken)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var atoms = new List<AtomData>();
        var compositions = new List<AtomComposition>();
        var childSources = new List<ChildSource>();
        var warnings = new List<string>();

        try
        {
            // Create parent atom for the archive file itself (metadata only)
            var archiveHash = SHA256.HashData(input);
            var archiveMetadataBytes = Encoding.UTF8.GetBytes($"archive:{source.FileName}:{input.Length}");
            var archiveAtom = new AtomData
            {
                AtomicValue = archiveMetadataBytes.Length <= MaxAtomSize ? archiveMetadataBytes : archiveMetadataBytes.Take(MaxAtomSize).ToArray(),
                ContentHash = archiveHash,
                Modality = "archive",
                Subtype = $"{GetArchiveType(source.ContentType, source.FileName)}-metadata",
                ContentType = source.ContentType,
                CanonicalText = $"{source.FileName ?? "archive"} ({input.Length:N0} bytes)",
                Metadata = $"{{\"size\":{input.Length},\"compressed\":true,\"fileName\":\"{source.FileName}\"}}"
            };
            atoms.Add(archiveAtom);

            // Extract archive contents
            if (source.ContentType?.Contains("zip") == true || 
                source.FileName?.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) == true)
            {
                await ExtractZipAsync(input, archiveHash, childSources, compositions, warnings, cancellationToken);
            }
            else if (source.ContentType?.Contains("gzip") == true ||
                     source.FileName?.EndsWith(".gz", StringComparison.OrdinalIgnoreCase) == true)
            {
                await ExtractGzipAsync(input, archiveHash, source.FileName, childSources, compositions, warnings, cancellationToken);
            }
            else
            {
                warnings.Add($"Archive type not yet implemented: {source.ContentType}");
            }

            sw.Stop();

            return new AtomizationResult
            {
                Atoms = atoms,
                Compositions = compositions,
                ChildSources = childSources,
                ProcessingInfo = new ProcessingMetadata
                {
                    TotalAtoms = atoms.Count,
                    UniqueAtoms = atoms.Count,
                    DurationMs = sw.ElapsedMilliseconds,
                    AtomizerType = nameof(ArchiveAtomizer),
                    DetectedFormat = $"{GetArchiveType(source.ContentType, source.FileName)} ({childSources.Count} files)",
                    Warnings = warnings.Count > 0 ? warnings : null
                }
            };
        }
        catch (Exception ex)
        {
            warnings.Add($"Archive extraction failed: {ex.Message}");
            throw;
        }
    }

    private async Task ExtractZipAsync(
        byte[] zipData,
        byte[] parentHash,
        List<ChildSource> childSources,
        List<AtomComposition> compositions,
        List<string> warnings,
        CancellationToken cancellationToken)
    {
        using var zipStream = new MemoryStream(zipData);
        using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read);

        int entryIndex = 0;
        foreach (var entry in archive.Entries)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Skip directories
            if (entry.FullName.EndsWith("/") || entry.FullName.EndsWith("\\"))
                continue;

            // Skip very large files (>100MB) - add warning
            if (entry.Length > 100 * 1024 * 1024)
            {
                warnings.Add($"Skipping large file: {entry.FullName} ({entry.Length:N0} bytes)");
                continue;
            }

            try
            {
                // Extract file content
                using var entryStream = entry.Open();
                using var memoryStream = new MemoryStream();
                await entryStream.CopyToAsync(memoryStream, cancellationToken);
                var fileContent = memoryStream.ToArray();

                // Detect content type from extension
                var ext = Path.GetExtension(entry.Name).TrimStart('.');
                var contentType = GetContentType(ext);

                // Create child source for recursive atomization
                var childSource = new ChildSource
                {
                    Content = fileContent,
                    Metadata = new SourceMetadata
                    {
                        FileName = entry.Name,
                        SourceUri = $"zip://{entry.FullName}",
                        SourceType = "zip-entry",
                        ContentType = contentType,
                        SizeBytes = fileContent.Length,
                        TenantId = 0, // Will be set by caller
                        Metadata = $"{{\"path\":\"{entry.FullName}\",\"compressed\":{entry.CompressedLength},\"ratio\":{(entry.Length > 0 ? (double)entry.CompressedLength / entry.Length : 0):F2}}}"
                    },
                    ParentAtomHash = parentHash
                };
                childSources.Add(childSource);

                // Create composition linking entry to archive
                var fileHash = SHA256.HashData(fileContent);
                compositions.Add(new AtomComposition
                {
                    ParentAtomHash = parentHash,
                    ComponentAtomHash = fileHash,
                    SequenceIndex = entryIndex++,
                    Position = new SpatialPosition { X = 0, Y = entryIndex, Z = 0 }
                });
            }
            catch (Exception ex)
            {
                warnings.Add($"Failed to extract {entry.FullName}: {ex.Message}");
            }
        }
    }

    private async Task ExtractGzipAsync(
        byte[] gzipData,
        byte[] parentHash,
        string? originalFileName,
        List<ChildSource> childSources,
        List<AtomComposition> compositions,
        List<string> warnings,
        CancellationToken cancellationToken)
    {
        try
        {
            using var compressedStream = new MemoryStream(gzipData);
            using var gzipStream = new GZipStream(compressedStream, CompressionMode.Decompress);
            using var decompressedStream = new MemoryStream();
            
            await gzipStream.CopyToAsync(decompressedStream, cancellationToken);
            var decompressedData = decompressedStream.ToArray();

            // Remove .gz extension to get original filename
            var fileName = originalFileName?.EndsWith(".gz", StringComparison.OrdinalIgnoreCase) == true
                ? Path.GetFileNameWithoutExtension(originalFileName)
                : "decompressed";

            var ext = Path.GetExtension(fileName).TrimStart('.');
            var contentType = GetContentType(ext);

            var childSource = new ChildSource
            {
                Content = decompressedData,
                Metadata = new SourceMetadata
                {
                    FileName = fileName,
                    SourceUri = $"gzip://{fileName}",
                    SourceType = "gzip-decompressed",
                    ContentType = contentType,
                    SizeBytes = decompressedData.Length,
                    TenantId = 0,
                    Metadata = $"{{\"compressed\":{gzipData.Length},\"decompressed\":{decompressedData.Length},\"ratio\":{(double)gzipData.Length / decompressedData.Length:F2}}}"
                },
                ParentAtomHash = parentHash
            };
            childSources.Add(childSource);

            var fileHash = SHA256.HashData(decompressedData);
            compositions.Add(new AtomComposition
            {
                ParentAtomHash = parentHash,
                ComponentAtomHash = fileHash,
                SequenceIndex = 0,
                Position = new SpatialPosition { X = 0, Y = 0, Z = 0 }
            });
        }
        catch (Exception ex)
        {
            warnings.Add($"GZIP decompression failed: {ex.Message}");
            throw;
        }
    }

    private string GetArchiveType(string? contentType, string? fileName)
    {
        if (contentType?.Contains("zip") == true) return "zip";
        if (contentType?.Contains("gzip") == true) return "gzip";
        if (contentType?.Contains("tar") == true) return "tar";
        
        if (fileName != null)
        {
            if (fileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)) return "zip";
            if (fileName.EndsWith(".gz", StringComparison.OrdinalIgnoreCase)) return "gzip";
            if (fileName.EndsWith(".tar", StringComparison.OrdinalIgnoreCase)) return "tar";
        }

        return "unknown";
    }

    private string GetContentType(string extension)
    {
        return extension.ToLowerInvariant() switch
        {
            "txt" or "log" or "md" => "text/plain",
            "json" => "application/json",
            "xml" => "application/xml",
            "png" => "image/png",
            "jpg" or "jpeg" => "image/jpeg",
            "gif" => "image/gif",
            "pdf" => "application/pdf",
            "zip" => "application/zip",
            "gz" => "application/gzip",
            "tar" => "application/x-tar",
            _ => "application/octet-stream"
        };
    }
}
