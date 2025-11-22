using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Hartonomous.Core.Interfaces.Ingestion;
using Hartonomous.Core.Utilities;
using Microsoft.Extensions.Logging;

namespace Hartonomous.Infrastructure.Atomizers;

/// <summary>
/// Atomizes source code files by extracting syntactic elements: functions, classes, methods, imports, comments.
/// Supports multiple programming languages with language-specific parsing heuristics.
/// </summary>
public class CodeFileAtomizer : BaseAtomizer<byte[]>
{
    public CodeFileAtomizer(ILogger<CodeFileAtomizer> logger) : base(logger) { }

    public override int Priority => 20;

    private static readonly Dictionary<string, string[]> LanguageExtensions = new()
    {
        ["csharp"] = new[] { "cs", "csx" },
        ["python"] = new[] { "py", "pyw", "pyx" },
        ["javascript"] = new[] { "js", "mjs", "cjs" },
        ["typescript"] = new[] { "ts", "tsx" },
        ["java"] = new[] { "java" },
        ["cpp"] = new[] { "cpp", "cc", "cxx", "c", "h", "hpp", "hxx" },
        ["go"] = new[] { "go" },
        ["rust"] = new[] { "rs" },
        ["ruby"] = new[] { "rb" },
        ["php"] = new[] { "php" },
        ["sql"] = new[] { "sql" },
        ["shell"] = new[] { "sh", "bash", "zsh" },
        ["powershell"] = new[] { "ps1", "psm1", "psd1" }
    };

    public override bool CanHandle(string contentType, string? fileExtension)
    {
        if (contentType?.Contains("x-csharp") == true ||
            contentType?.Contains("x-python") == true ||
            contentType?.Contains("javascript") == true ||
            contentType?.Contains("typescript") == true)
            return true;

        if (string.IsNullOrEmpty(fileExtension))
            return false;

        var ext = fileExtension.ToLowerInvariant();
        return LanguageExtensions.Values.Any(exts => exts.Contains(ext));
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

        var language = DetectLanguage(source.ContentType, source.FileName);
        var fileHash = CreateFileMetadataAtom(input, source, atoms);

        await ExtractCodeElementsAsync(code, language, fileHash, atoms, compositions, warnings, cancellationToken);
    }

    protected override string GetDetectedFormat() => "code (heuristic)";
    protected override string GetModality() => "code";

    protected override byte[] GetFileMetadataBytes(byte[] input, SourceMetadata source)
    {
        var language = DetectLanguage(source.ContentType, source.FileName);
        return Encoding.UTF8.GetBytes($"code:{language}:{source.FileName}:{input.Length}");
    }

    protected override string GetCanonicalFileText(byte[] input, SourceMetadata source)
    {
        return $"{source.FileName ?? "code"} ({input.Length:N0} bytes)";
    }

    protected override string GetFileMetadataJson(byte[] input, SourceMetadata source)
    {
        var language = DetectLanguage(source.ContentType, source.FileName);
        string code;
        try
        {
            code = Encoding.UTF8.GetString(input);
        }
        catch
        {
            code = "";
        }

        return $"{{\"language\":\"{language}\",\"size\":{input.Length},\"fileName\":\"{source.FileName}\",\"lines\":{(string.IsNullOrEmpty(code) ? 0 : code.Split('\n').Length)}}}";
    }

    private string DetectLanguage(string? contentType, string? fileName)
    {
        if (string.IsNullOrEmpty(fileName))
            return "unknown";

        var ext = System.IO.Path.GetExtension(fileName).TrimStart('.').ToLowerInvariant();
        
        foreach (var (lang, exts) in LanguageExtensions)
        {
            if (exts.Contains(ext))
                return lang;
        }

        return "unknown";
    }

    private async Task ExtractCodeElementsAsync(
        string code,
        string language,
        byte[] fileHash,
        List<AtomData> atoms,
        List<AtomComposition> compositions,
        List<string> warnings,
        CancellationToken cancellationToken)
    {
        ExtractImports(code, language, fileHash, atoms, compositions);
        
        switch (language)
        {
            case "csharp":
                ExtractCSharpElements(code, fileHash, atoms, compositions);
                break;
            case "python":
                ExtractPythonElements(code, fileHash, atoms, compositions);
                break;
            case "javascript":
            case "typescript":
                ExtractJavaScriptElements(code, fileHash, atoms, compositions);
                break;
            case "java":
                ExtractJavaElements(code, fileHash, atoms, compositions);
                break;
            case "cpp":
            case "c":
                ExtractCppElements(code, fileHash, atoms, compositions);
                break;
            default:
                ExtractGenericElements(code, fileHash, atoms, compositions);
                break;
        }
        
        ExtractComments(code, language, fileHash, atoms, compositions);
        warnings.Add($"Code parsing is heuristic-based; production use should employ language-specific AST parsers (e.g., Roslyn for C#)");
        
        await Task.CompletedTask;
    }

    private void ExtractImports(string code, string language, byte[] fileHash, List<AtomData> atoms, List<AtomComposition> compositions)
    {
        var importPattern = language switch
        {
            "csharp" => @"^\s*using\s+([^;]+);",
            "python" => @"^\s*(?:import|from)\s+([^\n]+)",
            "javascript" or "typescript" => @"^\s*import\s+([^\n]+)",
            "java" => @"^\s*import\s+([^;]+);",
            "cpp" or "c" => @"^\s*#include\s+([^\n]+)",
            "go" => @"^\s*import\s+([^\n]+)",
            _ => null
        };

        if (importPattern == null) return;

        var regex = new Regex(importPattern, RegexOptions.Multiline);
        var matches = regex.Matches(code);
        
        int importIndex = 0;
        foreach (Match match in matches)
        {
            var importStatement = match.Groups[1].Value.Trim();
            var importBytes = Encoding.UTF8.GetBytes(importStatement);
            var importHash = CreateContentAtom(importBytes, "code", "import", importStatement, 
                $"{{\"language\":\"{language}\",\"type\":\"import\"}}", atoms);
            
            CreateAtomComposition(fileHash, importHash, importIndex++, compositions);
        }
    }

    private void ExtractCSharpElements(string code, byte[] fileHash, List<AtomData> atoms, List<AtomComposition> compositions)
    {
        ExtractPatternElements(code, @"(?:public|private|internal|protected)?\s*(?:static|abstract|sealed)?\s*(?:partial)?\s*class\s+(\w+)", "class", "csharp", fileHash, atoms, compositions);
        ExtractPatternElements(code, @"(?:public|private|internal)?\s*interface\s+(\w+)", "interface", "csharp", fileHash, atoms, compositions);
        ExtractPatternElements(code, @"(?:public|private|protected|internal)?\s*(?:static|virtual|override|async)?\s*\w+\s+(\w+)\s*\([^)]*\)", "method", "csharp", fileHash, atoms, compositions);
        ExtractPatternElements(code, @"(?:public|private|protected|internal)?\s*(?:static)?\s*\w+\s+(\w+)\s*{\s*get;", "property", "csharp", fileHash, atoms, compositions);
    }

    private void ExtractPythonElements(string code, byte[] fileHash, List<AtomData> atoms, List<AtomComposition> compositions)
    {
        ExtractPatternElements(code, @"^class\s+(\w+)", "class", "python", fileHash, atoms, compositions);
        ExtractPatternElements(code, @"^(?:async\s+)?def\s+(\w+)\s*\(", "function", "python", fileHash, atoms, compositions);
    }

    private void ExtractJavaScriptElements(string code, byte[] fileHash, List<AtomData> atoms, List<AtomComposition> compositions)
    {
        ExtractPatternElements(code, @"class\s+(\w+)", "class", "javascript", fileHash, atoms, compositions);
        ExtractPatternElements(code, @"(?:function\s+(\w+)|const\s+(\w+)\s*=\s*(?:async\s*)?\([^)]*\)\s*=>|(\w+)\s*:\s*(?:async\s*)?\([^)]*\)\s*=>)", "function", "javascript", fileHash, atoms, compositions);
    }

    private void ExtractJavaElements(string code, byte[] fileHash, List<AtomData> atoms, List<AtomComposition> compositions)
    {
        ExtractPatternElements(code, @"(?:public|private|protected)?\s*(?:static|final|abstract)?\s*class\s+(\w+)", "class", "java", fileHash, atoms, compositions);
        ExtractPatternElements(code, @"(?:public)?\s*interface\s+(\w+)", "interface", "java", fileHash, atoms, compositions);
        ExtractPatternElements(code, @"(?:public|private|protected)?\s*(?:static|final|synchronized)?\s*\w+\s+(\w+)\s*\([^)]*\)", "method", "java", fileHash, atoms, compositions);
    }

    private void ExtractCppElements(string code, byte[] fileHash, List<AtomData> atoms, List<AtomComposition> compositions)
    {
        ExtractPatternElements(code, @"(?:class|struct)\s+(\w+)", "class", "cpp", fileHash, atoms, compositions);
        ExtractPatternElements(code, @"\w+\s+(\w+)\s*\([^)]*\)\s*(?:{|;)", "function", "cpp", fileHash, atoms, compositions);
    }

    private void ExtractGenericElements(string code, byte[] fileHash, List<AtomData> atoms, List<AtomComposition> compositions)
    {
        ExtractPatternElements(code, @"function\s+(\w+)|def\s+(\w+)|(\w+)\s*\([^)]*\)\s*{", "function", "generic", fileHash, atoms, compositions);
    }

    private void ExtractPatternElements(string code, string pattern, string elementType, string language, byte[] fileHash, List<AtomData> atoms, List<AtomComposition> compositions)
    {
        var regex = new Regex(pattern, RegexOptions.Multiline);
        var matches = regex.Matches(code);
        
        int elementIndex = 0;
        foreach (Match match in matches)
        {
            string elementName = "";
            for (int i = 1; i < match.Groups.Count; i++)
            {
                if (!string.IsNullOrEmpty(match.Groups[i].Value))
                {
                    elementName = match.Groups[i].Value;
                    break;
                }
            }
            
            if (string.IsNullOrEmpty(elementName)) continue;
            
            var elementBytes = Encoding.UTF8.GetBytes($"{elementType}:{elementName}");
            var elementHash = CreateContentAtom(elementBytes, "code", elementType, elementName,
                $"{{\"language\":\"{language}\",\"type\":\"{elementType}\",\"name\":\"{elementName}\"}}", atoms);
            
            CreateAtomComposition(fileHash, elementHash, elementIndex++, compositions);
        }
    }

    private void ExtractComments(string code, string language, byte[] fileHash, List<AtomData> atoms, List<AtomComposition> compositions)
    {
        var singleLinePattern = language switch
        {
            "csharp" or "java" or "javascript" or "typescript" or "cpp" or "c" or "go" or "rust" => @"//\s*(.*?)$",
            "python" or "ruby" or "shell" => @"#\s*(.*?)$",
            "sql" => @"--\s*(.*?)$",
            _ => @"//\s*(.*?)$"
        };

        var regex = new Regex(singleLinePattern, RegexOptions.Multiline);
        var matches = regex.Matches(code);
        
        int commentIndex = 0;
        foreach (Match match in matches)
        {
            var commentText = match.Groups[1].Value.Trim();
            if (string.IsNullOrWhiteSpace(commentText) || commentText.Length < 5) continue;
            
            var commentBytes = Encoding.UTF8.GetBytes(commentText);
            var commentHash = CreateContentAtom(
                commentBytes.Length <= MaxAtomSize ? commentBytes : commentBytes.Take(MaxAtomSize).ToArray(),
                "text",
                "code-comment",
                commentText.Length <= 100 ? commentText : commentText[..100] + "...",
                $"{{\"language\":\"{language}\",\"type\":\"comment\"}}",
                atoms);
            
            CreateAtomComposition(fileHash, commentHash, commentIndex++, compositions);
        }
    }
}
