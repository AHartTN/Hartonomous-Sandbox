using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Hartonomous.Core.Interfaces.Ingestion;
using Microsoft.Extensions.Logging;

namespace Hartonomous.Infrastructure.Atomizers;

/// <summary>
/// Atomizes text content into character-level or token-level atoms with spatial positions.
/// Supports UTF-8 encoding and preserves line/column information.
/// </summary>
public class TextAtomizer : BaseAtomizer<byte[]>
{
    public TextAtomizer(ILogger<TextAtomizer> logger) : base(logger) { }

    public override int Priority => 10;

    public override bool CanHandle(string contentType, string? fileExtension)
    {
        if (contentType?.StartsWith("text/") == true)
            return true;

        if (contentType == "application/json" || 
            contentType == "application/xml" ||
            contentType == "text/markdown")
            return true;

        var textExtensions = new[] { "txt", "md", "json", "xml", "yaml", "yml", "csv", "log", "ini", "cfg", "conf" };
        return fileExtension != null && textExtensions.Contains(fileExtension.ToLowerInvariant());
    }

    protected override async Task AtomizeCoreAsync(
        byte[] input,
        SourceMetadata source,
        List<AtomData> atoms,
        List<AtomComposition> compositions,
        List<string> warnings,
        CancellationToken cancellationToken)
    {
        // Decode UTF-8
        string text;
        try
        {
            text = Encoding.UTF8.GetString(input);
        }
        catch (Exception ex)
        {
            warnings.Add($"UTF-8 decode failed: {ex.Message}");
            // Fallback: treat as Latin1
            text = Encoding.GetEncoding("ISO-8859-1").GetString(input);
        }

        // Create parent atom for the entire text file
        var fileHash = CreateFileMetadataAtom(input, source, atoms);

        // Atomize by line and character
        var lines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        int charOffset = 0;

        for (int lineNum = 0; lineNum < lines.Length; lineNum++)
        {
            var line = lines[lineNum];
            
            // Create line metadata atom (if not empty)
            if (!string.IsNullOrWhiteSpace(line))
            {
                var lineMetadataBytes = Encoding.UTF8.GetBytes($"line:{lineNum}:{line.Length}");
                var lineHash = CreateContentHash(lineMetadataBytes);
                var lineAtom = new AtomData
                {
                    AtomicValue = lineMetadataBytes.Length <= MaxAtomSize ? lineMetadataBytes : lineMetadataBytes.Take(MaxAtomSize).ToArray(),
                    ContentHash = lineHash,
                    Modality = "text",
                    Subtype = "line-metadata",
                    ContentType = source.ContentType,
                    CanonicalText = line.Length <= 100 ? line : line[..100] + "...",
                    Metadata = $"{{\"lineNum\":{lineNum + 1},\"length\":{line.Length}}}"
                };
                atoms.Add(lineAtom);

                // Link line to file
                compositions.Add(new AtomComposition
                {
                    ParentAtomHash = fileHash,
                    ComponentAtomHash = lineHash,
                    SequenceIndex = lineNum,
                    Position = new SpatialPosition { X = 0, Y = lineNum, Z = 0 }
                });

                // Atomize characters within the line
                AtomizeCharacters(line, lineNum, lineHash, atoms, compositions, ref charOffset);
            }
            else
            {
                // Empty line - still track position
                charOffset += line.Length;
            }

            // Account for line ending
            charOffset += lines.Length > lineNum + 1 ? Environment.NewLine.Length : 0;
        }
    }

    protected override string GetDetectedFormat() => "UTF-8 text";

    protected override string GetModality() => "text";

    protected override byte[] GetFileMetadataBytes(byte[] input, SourceMetadata source)
    {
        return Encoding.UTF8.GetBytes($"file:{source.FileName}:{input.Length}");
    }

    protected override string GetCanonicalFileText(byte[] input, SourceMetadata source)
    {
        return $"{source.FileName} ({input.Length} bytes)";
    }

    protected override string GetFileMetadataJson(byte[] input, SourceMetadata source)
    {
        return $"{{\"size\":{input.Length},\"encoding\":\"utf-8\",\"fileName\":\"{source.FileName}\"}}";
    }

    private void AtomizeCharacters(
        string line, 
        int lineNum, 
        byte[] parentLineHash,
        List<AtomData> atoms, 
        List<AtomComposition> compositions,
        ref int globalCharOffset)
    {
        for (int col = 0; col < line.Length; col++)
        {
            var ch = line[col];
            var charBytes = Encoding.UTF8.GetBytes(new[] { ch });

            // Only create atoms for non-whitespace characters to reduce volume
            // (or create all atoms if you want full fidelity)
            if (!char.IsWhiteSpace(ch))
            {
                var charHash = SHA256.HashData(charBytes);
                var charAtom = new AtomData
                {
                    AtomicValue = charBytes,
                    ContentHash = charHash,
                    Modality = "text",
                    Subtype = "utf8-char",
                    ContentType = "text/plain",
                    CanonicalText = ch.ToString(),
                    Metadata = $"{{\"unicode\":\"U+{((int)ch):X4}\",\"offset\":{globalCharOffset + col}}}"
                };

                // Check if already exists (deduplication at atomizer level)
                if (!atoms.Any(a => a.ContentHash.SequenceEqual(charHash)))
                {
                    atoms.Add(charAtom);
                }

                // Link character to line
                compositions.Add(new AtomComposition
                {
                    ParentAtomHash = parentLineHash,
                    ComponentAtomHash = charHash,
                    SequenceIndex = col,
                    Position = new SpatialPosition 
                    { 
                        X = col, 
                        Y = lineNum, 
                        Z = 0,
                        M = globalCharOffset + col // Absolute offset in file
                    }
                });
            }
        }

        globalCharOffset += line.Length;
    }
}
