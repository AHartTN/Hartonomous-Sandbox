using System.Security.Cryptography;
using System.Text;
using Hartonomous.Core.Interfaces.Ingestion;
using Hartonomous.Infrastructure.Atomizers.Visitors;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Hartonomous.Infrastructure.Atomizers;

/// <summary>
/// C# code atomizer using Roslyn for semantic AST parsing.
/// Extracts classes, methods, properties, and relationships with full semantic understanding.
/// </summary>
public class RoslynAtomizer : IAtomizer<byte[]>
{
    private const int MaxAtomSize = 64;
    public int Priority => 25; // Higher priority than regex-based CodeFileAtomizer

    public bool CanHandle(string contentType, string? fileExtension)
    {
        if (contentType?.Contains("x-csharp") == true || contentType?.Contains("csharp") == true)
            return true;

        if (string.IsNullOrEmpty(fileExtension))
            return false;

        var ext = fileExtension.ToLowerInvariant();
        return ext is "cs" or "csx";
    }

    public async Task<AtomizationResult> AtomizeAsync(byte[] input, SourceMetadata source, CancellationToken cancellationToken)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var atoms = new List<AtomData>();
        var compositions = new List<AtomComposition>();
        var warnings = new List<string>();

        try
        {
            string code;
            try
            {
                code = Encoding.UTF8.GetString(input);
            }
            catch
            {
                warnings.Add("UTF-8 decode failed, using Latin1 fallback");
                code = Encoding.GetEncoding("ISO-8859-1").GetString(input);
            }

            var fileHash = SHA256.HashData(input);
            var tree = CSharpSyntaxTree.ParseText(code, cancellationToken: cancellationToken);
            var root = await tree.GetRootAsync(cancellationToken);

            // Create file-level atom
            var fileAtom = CreateFileAtom(source, input, fileHash, code);
            atoms.Add(fileAtom);

            // Extract semantic elements
            var visitor = new CSharpSemanticVisitor(atoms, compositions, fileHash, warnings);
            visitor.Visit(root);

            sw.Stop();
            var uniqueHashes = atoms.Select(a => Convert.ToBase64String(a.ContentHash)).Distinct().Count();

            return new AtomizationResult
            {
                Atoms = atoms,
                Compositions = compositions,
                ProcessingInfo = new ProcessingMetadata
                {
                    TotalAtoms = atoms.Count,
                    UniqueAtoms = uniqueHashes,
                    DurationMs = sw.ElapsedMilliseconds,
                    AtomizerType = nameof(RoslynAtomizer),
                    DetectedFormat = "csharp",
                    Warnings = warnings.Count > 0 ? warnings : null
                }
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            warnings.Add($"Roslyn parsing failed: {ex.Message}");
            var uniqueHashes = atoms.Select(a => Convert.ToBase64String(a.ContentHash)).Distinct().Count();

            return new AtomizationResult
            {
                Atoms = atoms,
                Compositions = compositions,
                ProcessingInfo = new ProcessingMetadata
                {
                    TotalAtoms = atoms.Count,
                    UniqueAtoms = uniqueHashes,
                    DurationMs = sw.ElapsedMilliseconds,
                    AtomizerType = nameof(RoslynAtomizer),
                    DetectedFormat = "csharp",
                    Warnings = warnings
                }
            };
        }
    }

    private static AtomData CreateFileAtom(SourceMetadata source, byte[] input, byte[] fileHash, string code)
    {
        var fileMetadataBytes = Encoding.UTF8.GetBytes($"csharp:{source.FileName}:{input.Length}");
        return new AtomData
        {
            AtomicValue = fileMetadataBytes.Length <= MaxAtomSize ? fileMetadataBytes : fileMetadataBytes.Take(MaxAtomSize).ToArray(),
            ContentHash = fileHash,
            Modality = "code",
            Subtype = "csharp-file",
            ContentType = source.ContentType ?? "text/x-csharp",
            CanonicalText = $"{source.FileName ?? "code.cs"} ({input.Length:N0} bytes)",
            Metadata = System.Text.Json.JsonSerializer.Serialize(new
            {
                language = "csharp",
                size = input.Length,
                fileName = source.FileName,
                lines = code.Split('\n').Length,
                parsingEngine = "Roslyn"
            })
        };
    }
}
