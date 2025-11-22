using System.Text;
using System.Text.RegularExpressions;
using Hartonomous.Core.Interfaces.Ingestion;
using Hartonomous.Core.Utilities;
using Hartonomous.Infrastructure.Atomizers.Configuration;
using Microsoft.Extensions.Logging;

namespace Hartonomous.Infrastructure.Atomizers;

/// <summary>
/// Polyglot code atomizer using regex patterns (Tree-sitter style parsing).
/// Supports Python, JavaScript, TypeScript, Go, Rust, Ruby, Java, and more.
/// </summary>
public class TreeSitterAtomizer : BaseAtomizer<byte[]>
{
    public TreeSitterAtomizer(ILogger<TreeSitterAtomizer> logger) : base(logger) { }

    public override int Priority => 22;

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

    public override bool CanHandle(string contentType, string? fileExtension)
    {
        if (string.IsNullOrEmpty(fileExtension))
            return false;

        var ext = fileExtension.ToLowerInvariant();
        return SupportedLanguages.Values.Any(lang => lang.Extensions.Contains(ext));
    }

    protected override async Task AtomizeCoreAsync(
        byte[] input,
        SourceMetadata source,
        List<AtomData> atoms,
        List<AtomComposition> compositions,
        List<string> warnings,
        CancellationToken cancellationToken)
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

        var fileHash = CreateFileMetadataAtom(input, source, atoms);

        if (config != null)
        {
            var sequenceIndex = 0;
            
            if (!string.IsNullOrEmpty(config.ClassPattern))
            {
                ExtractElements(code, config.ClassPattern, "class", language, fileHash, atoms, compositions, ref sequenceIndex);
            }

            if (!string.IsNullOrEmpty(config.FunctionPattern))
            {
                ExtractElements(code, config.FunctionPattern, "function", language, fileHash, atoms, compositions, ref sequenceIndex);
            }
        }

        await Task.CompletedTask;
    }

    protected override string GetDetectedFormat()
    {
        return "polyglot code (Tree-sitter regex)";
    }

    protected override string GetModality() => "code";

    protected override byte[] GetFileMetadataBytes(byte[] input, SourceMetadata source)
    {
        var language = DetectLanguage(source.FileName) ?? "unknown";
        return Encoding.UTF8.GetBytes($"{language}:{source.FileName}:{input.Length}");
    }

    protected override string GetCanonicalFileText(byte[] input, SourceMetadata source)
    {
        return $"{source.FileName ?? "code"} ({input.Length:N0} bytes)";
    }

    protected override string GetFileMetadataJson(byte[] input, SourceMetadata source)
    {
        var language = DetectLanguage(source.FileName) ?? "unknown";
        string code;
        try
        {
            code = Encoding.UTF8.GetString(input);
        }
        catch
        {
            code = "";
        }
        
        return System.Text.Json.JsonSerializer.Serialize(new
        {
            language,
            size = input.Length,
            fileName = source.FileName,
            lines = string.IsNullOrEmpty(code) ? 0 : code.Split('\n').Length,
            parsingEngine = "TreeSitter-Regex"
        });
    }

    private static string? DetectLanguage(string? fileName)
    {
        if (string.IsNullOrEmpty(fileName))
            return null;

        var ext = Path.GetExtension(fileName).TrimStart('.').ToLowerInvariant();
        return SupportedLanguages.FirstOrDefault(kv => kv.Value.Extensions.Contains(ext)).Key;
    }

    private void ExtractElements(
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
            var elementHash = CreateContentAtom(
                contentBytes,
                "code",
                $"{language}-{elementType}",
                elementName,
                System.Text.Json.JsonSerializer.Serialize(new
                {
                    language,
                    type = elementType,
                    name = elementName,
                    parsingEngine = "TreeSitter-Regex"
                }),
                atoms);

            CreateAtomComposition(fileHash, elementHash, sequenceIndex++, compositions);
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
