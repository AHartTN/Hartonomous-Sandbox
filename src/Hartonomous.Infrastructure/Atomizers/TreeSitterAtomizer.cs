using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Hartonomous.Core.Interfaces.Ingestion;

namespace Hartonomous.Infrastructure.Atomizers;

/// <summary>
/// Polyglot code atomizer using regex patterns (Tree-sitter style parsing).
/// Supports Python, JavaScript, TypeScript, Go, Rust, Ruby, Java, and more.
/// </summary>
public class TreeSitterAtomizer : IAtomizer<byte[]>
{
    private const int MaxAtomSize = 64;
    public int Priority => 22; // Higher than CodeFileAtomizer but lower than RoslynAtomizer for C#

    private static readonly Dictionary<string, LanguageConfig> SupportedLanguages = new()
    {
        ["python"] = new("py", "pyw", "pyx") { FunctionPattern = @"def\s+(\w+)\s*\(", ClassPattern = @"class\s+(\w+)\s*[:\(]" },
        ["javascript"] = new("js", "mjs", "cjs") { FunctionPattern = @"function\s+(\w+)\s*\(|(\w+)\s*[=:]\s*(?:async\s*)?\(", ClassPattern = @"class\s+(\w+)" },
        ["typescript"] = new("ts", "tsx") { FunctionPattern = @"function\s+(\w+)\s*\(|(\w+)\s*[=:]\s*(?:async\s*)?\(", ClassPattern = @"class\s+(\w+)|interface\s+(\w+)" },
        ["go"] = new("go") { FunctionPattern = @"func\s+(?:\([^)]+\)\s*)?(\w+)\s*\(", ClassPattern = @"type\s+(\w+)\s+struct" },
        ["rust"] = new("rs") { FunctionPattern = @"fn\s+(\w+)\s*[<\(]", ClassPattern = @"struct\s+(\w+)|enum\s+(\w+)|impl\s+(\w+)" },
        ["ruby"] = new("rb") { FunctionPattern = @"def\s+(\w+)", ClassPattern = @"class\s+(\w+)|module\s+(\w+)" },
        ["java"] = new("java") { FunctionPattern = @"(?:public|private|protected)?\s*(?:static)?\s*\w+\s+(\w+)\s*\(", ClassPattern = @"class\s+(\w+)|interface\s+(\w+)" },
        ["php"] = new("php") { FunctionPattern = @"function\s+(\w+)\s*\(", ClassPattern = @"class\s+(\w+)|interface\s+(\w+)" },
        ["swift"] = new("swift") { FunctionPattern = @"func\s+(\w+)\s*[<\(]", ClassPattern = @"class\s+(\w+)|struct\s+(\w+)|protocol\s+(\w+)" },
        ["kotlin"] = new("kt", "kts") { FunctionPattern = @"fun\s+(\w+)\s*[<\(]", ClassPattern = @"class\s+(\w+)|interface\s+(\w+)|object\s+(\w+)" },
        ["scala"] = new("scala") { FunctionPattern = @"def\s+(\w+)\s*[<\(:\[]", ClassPattern = @"class\s+(\w+)|trait\s+(\w+)|object\s+(\w+)" }
    };

    public bool CanHandle(string contentType, string? fileExtension)
    {
        if (string.IsNullOrEmpty(fileExtension))
            return false;

        var ext = fileExtension.ToLowerInvariant();
        return SupportedLanguages.Values.Any(lang => lang.Extensions.Contains(ext));
    }

    public async Task<AtomizationResult> AtomizeAsync(byte[] input, SourceMetadata source, CancellationToken cancellationToken)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var atoms = new List<AtomData>();
        var compositions = new List<AtomComposition>();
        var warnings = new List<string>();
        var sequenceIndex = 0;

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

            var language = DetectLanguage(source.FileName);
            LanguageConfig? config = null;
            if (language == null || !SupportedLanguages.TryGetValue(language, out config))
            {
                warnings.Add($"Unsupported language for file: {source.FileName}");
                language = "unknown";
            }

            var fileHash = SHA256.HashData(input);

            // Create file-level atom
            var fileAtom = CreateFileAtom(source, input, fileHash, code, language);
            atoms.Add(fileAtom);

            if (config != null)
            {
                // Extract classes/types
                if (!string.IsNullOrEmpty(config.ClassPattern))
                {
                    ExtractElements(code, config.ClassPattern, "class", language, fileHash, atoms, compositions, ref sequenceIndex);
                }

                // Extract functions/methods
                if (!string.IsNullOrEmpty(config.FunctionPattern))
                {
                    ExtractElements(code, config.FunctionPattern, "function", language, fileHash, atoms, compositions, ref sequenceIndex);
                }
            }

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
                    AtomizerType = nameof(TreeSitterAtomizer),
                    DetectedFormat = language,
                    Warnings = warnings.Count > 0 ? warnings : null
                }
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            warnings.Add($"Tree-sitter parsing failed: {ex.Message}");
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
                    AtomizerType = nameof(TreeSitterAtomizer),
                    DetectedFormat = "unknown",
                    Warnings = warnings
                }
            };
        }
    }

    private static string? DetectLanguage(string? fileName)
    {
        if (string.IsNullOrEmpty(fileName))
            return null;

        var ext = Path.GetExtension(fileName).TrimStart('.').ToLowerInvariant();
        return SupportedLanguages.FirstOrDefault(kv => kv.Value.Extensions.Contains(ext)).Key;
    }

    private static AtomData CreateFileAtom(SourceMetadata source, byte[] input, byte[] fileHash, string code, string language)
    {
        var fileMetadataBytes = Encoding.UTF8.GetBytes($"{language}:{source.FileName}:{input.Length}");
        return new AtomData
        {
            AtomicValue = fileMetadataBytes.Length <= MaxAtomSize ? fileMetadataBytes : fileMetadataBytes.Take(MaxAtomSize).ToArray(),
            ContentHash = fileHash,
            Modality = "code",
            Subtype = $"{language}-file",
            ContentType = source.ContentType ?? $"text/x-{language}",
            CanonicalText = $"{source.FileName ?? "code"} ({input.Length:N0} bytes)",
            Metadata = System.Text.Json.JsonSerializer.Serialize(new
            {
                language,
                size = input.Length,
                fileName = source.FileName,
                lines = code.Split('\n').Length,
                parsingEngine = "TreeSitter-Regex"
            })
        };
    }

    private static void ExtractElements(
        string code,
        string pattern,
        string elementType,
        string language,
        byte[] fileHash,
        List<AtomData> atoms,
        List<AtomComposition> compositions,
        ref int sequenceIndex)
    {
        var regex = new Regex(pattern, RegexOptions.Multiline);
        var matches = regex.Matches(code);

        foreach (Match match in matches)
        {
            string? elementName = null;
            for (int i = 1; i < match.Groups.Count; i++)
            {
                if (!string.IsNullOrEmpty(match.Groups[i].Value))
                {
                    elementName = match.Groups[i].Value;
                    break;
                }
            }

            if (string.IsNullOrEmpty(elementName))
                continue;

            var contentBytes = Encoding.UTF8.GetBytes($"{language}:{elementType}:{elementName}");
            var elementHash = SHA256.HashData(contentBytes);

            if (atoms.Any(a => a.ContentHash.SequenceEqual(elementHash)))
                continue;

            var atom = new AtomData
            {
                AtomicValue = contentBytes.Length <= MaxAtomSize ? contentBytes : contentBytes.Take(MaxAtomSize).ToArray(),
                ContentHash = elementHash,
                Modality = "code",
                Subtype = $"{language}-{elementType}",
                ContentType = $"text/x-{language}",
                CanonicalText = elementName,
                Metadata = System.Text.Json.JsonSerializer.Serialize(new
                {
                    language,
                    type = elementType,
                    name = elementName,
                    parsingEngine = "TreeSitter-Regex"
                })
            };

            atoms.Add(atom);
            compositions.Add(new AtomComposition
            {
                ParentAtomHash = fileHash,
                ComponentAtomHash = elementHash,
                SequenceIndex = sequenceIndex++,
                Position = new SpatialPosition { X = 0, Y = 0, Z = 0 }
            });
        }
    }

    private class LanguageConfig
    {
        public HashSet<string> Extensions { get; }
        public string? FunctionPattern { get; init; }
        public string? ClassPattern { get; init; }

        public LanguageConfig(params string[] extensions)
        {
            Extensions = new HashSet<string>(extensions, StringComparer.OrdinalIgnoreCase);
        }
    }
}
