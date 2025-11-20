using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Hartonomous.Core.Interfaces.Ingestion;

namespace Hartonomous.Infrastructure.Atomizers;

/// <summary>
/// Atomizes source code files by extracting syntactic elements: functions, classes, methods, imports, comments.
/// Supports multiple programming languages with language-specific parsing heuristics.
/// </summary>
public class CodeFileAtomizer : IAtomizer<byte[]>
{
    private const int MaxAtomSize = 64;
    public int Priority => 20;

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

    public bool CanHandle(string contentType, string? fileExtension)
    {
        // Check content type
        if (contentType?.Contains("x-csharp") == true ||
            contentType?.Contains("x-python") == true ||
            contentType?.Contains("javascript") == true ||
            contentType?.Contains("typescript") == true)
            return true;

        // Check file extension
        if (string.IsNullOrEmpty(fileExtension))
            return false;

        var ext = fileExtension.ToLowerInvariant();
        return LanguageExtensions.Values.Any(exts => exts.Contains(ext));
    }

    public async Task<AtomizationResult> AtomizeAsync(byte[] input, SourceMetadata source, CancellationToken cancellationToken)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var atoms = new List<AtomData>();
        var compositions = new List<AtomComposition>();
        var warnings = new List<string>();

        try
        {
            // Decode source code
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

            // Detect language
            var language = DetectLanguage(source.ContentType, source.FileName);
            
            // Create parent atom for the code file
            var fileHash = SHA256.HashData(input);
            var fileMetadataBytes = Encoding.UTF8.GetBytes($"code:{language}:{source.FileName}:{input.Length}");
            
            var fileAtom = new AtomData
            {
                AtomicValue = fileMetadataBytes.Length <= MaxAtomSize ? fileMetadataBytes : fileMetadataBytes.Take(MaxAtomSize).ToArray(),
                ContentHash = fileHash,
                Modality = "code",
                Subtype = $"{language}-file",
                ContentType = source.ContentType ?? $"text/x-{language}",
                CanonicalText = $"{source.FileName ?? "code"} ({input.Length:N0} bytes)",
                Metadata = $"{{\"language\":\"{language}\",\"size\":{input.Length},\"fileName\":\"{source.FileName}\",\"lines\":{code.Split('\n').Length}}}"
            };
            atoms.Add(fileAtom);

            // Extract code elements
            await ExtractCodeElementsAsync(code, language, fileHash, atoms, compositions, warnings, cancellationToken);

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
                    AtomizerType = nameof(CodeFileAtomizer),
                    DetectedFormat = language,
                    Warnings = warnings.Count > 0 ? warnings : null
                }
            };
        }
        catch (Exception ex)
        {
            warnings.Add($"Code atomization failed: {ex.Message}");
            throw;
        }
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
        var lines = code.Split('\n');
        
        // Extract imports/using statements
        ExtractImports(code, language, fileHash, atoms, compositions);
        
        // Extract functions/methods (language-specific patterns)
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
        
        // Extract comments
        ExtractComments(code, language, fileHash, atoms, compositions);
        
        warnings.Add($"Code parsing is heuristic-based; production use should employ language-specific AST parsers (e.g., Roslyn for C#)");
        
        await Task.CompletedTask;
    }

    private void ExtractImports(
        string code,
        string language,
        byte[] fileHash,
        List<AtomData> atoms,
        List<AtomComposition> compositions)
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

        if (importPattern == null)
            return;

        var regex = new Regex(importPattern, RegexOptions.Multiline);
        var matches = regex.Matches(code);
        
        int importIndex = 0;
        foreach (Match match in matches)
        {
            var importStatement = match.Groups[1].Value.Trim();
            var importBytes = Encoding.UTF8.GetBytes(importStatement);
            var importHash = SHA256.HashData(importBytes);
            
            var importAtom = new AtomData
            {
                AtomicValue = importBytes.Length <= MaxAtomSize ? importBytes : importBytes.Take(MaxAtomSize).ToArray(),
                ContentHash = importHash,
                Modality = "code",
                Subtype = "import",
                ContentType = $"text/x-{language}",
                CanonicalText = importStatement,
                Metadata = $"{{\"language\":\"{language}\",\"type\":\"import\"}}"
            };
            
            if (!atoms.Any(a => a.ContentHash.SequenceEqual(importHash)))
            {
                atoms.Add(importAtom);
            }
            
            compositions.Add(new AtomComposition
            {
                ParentAtomHash = fileHash,
                ComponentAtomHash = importHash,
                SequenceIndex = importIndex++,
                Position = new SpatialPosition { X = 0, Y = 0, Z = 0 }
            });
        }
    }

    private void ExtractCSharpElements(
        string code,
        byte[] fileHash,
        List<AtomData> atoms,
        List<AtomComposition> compositions)
    {
        // Extract classes
        var classPattern = @"(?:public|private|internal|protected)?\s*(?:static|abstract|sealed)?\s*(?:partial)?\s*class\s+(\w+)";
        ExtractPatternElements(code, classPattern, "class", "csharp", fileHash, atoms, compositions);
        
        // Extract interfaces
        var interfacePattern = @"(?:public|private|internal)?\s*interface\s+(\w+)";
        ExtractPatternElements(code, interfacePattern, "interface", "csharp", fileHash, atoms, compositions);
        
        // Extract methods
        var methodPattern = @"(?:public|private|protected|internal)?\s*(?:static|virtual|override|async)?\s*\w+\s+(\w+)\s*\([^)]*\)";
        ExtractPatternElements(code, methodPattern, "method", "csharp", fileHash, atoms, compositions);
        
        // Extract properties
        var propertyPattern = @"(?:public|private|protected|internal)?\s*(?:static)?\s*\w+\s+(\w+)\s*{\s*get;";
        ExtractPatternElements(code, propertyPattern, "property", "csharp", fileHash, atoms, compositions);
    }

    private void ExtractPythonElements(
        string code,
        byte[] fileHash,
        List<AtomData> atoms,
        List<AtomComposition> compositions)
    {
        // Extract classes
        var classPattern = @"^class\s+(\w+)";
        ExtractPatternElements(code, classPattern, "class", "python", fileHash, atoms, compositions);
        
        // Extract functions
        var functionPattern = @"^(?:async\s+)?def\s+(\w+)\s*\(";
        ExtractPatternElements(code, functionPattern, "function", "python", fileHash, atoms, compositions);
    }

    private void ExtractJavaScriptElements(
        string code,
        byte[] fileHash,
        List<AtomData> atoms,
        List<AtomComposition> compositions)
    {
        // Extract classes
        var classPattern = @"class\s+(\w+)";
        ExtractPatternElements(code, classPattern, "class", "javascript", fileHash, atoms, compositions);
        
        // Extract functions
        var functionPattern = @"(?:function\s+(\w+)|const\s+(\w+)\s*=\s*(?:async\s*)?\([^)]*\)\s*=>|(\w+)\s*:\s*(?:async\s*)?\([^)]*\)\s*=>)";
        ExtractPatternElements(code, functionPattern, "function", "javascript", fileHash, atoms, compositions);
    }

    private void ExtractJavaElements(
        string code,
        byte[] fileHash,
        List<AtomData> atoms,
        List<AtomComposition> compositions)
    {
        // Extract classes
        var classPattern = @"(?:public|private|protected)?\s*(?:static|final|abstract)?\s*class\s+(\w+)";
        ExtractPatternElements(code, classPattern, "class", "java", fileHash, atoms, compositions);
        
        // Extract interfaces
        var interfacePattern = @"(?:public)?\s*interface\s+(\w+)";
        ExtractPatternElements(code, interfacePattern, "interface", "java", fileHash, atoms, compositions);
        
        // Extract methods
        var methodPattern = @"(?:public|private|protected)?\s*(?:static|final|synchronized)?\s*\w+\s+(\w+)\s*\([^)]*\)";
        ExtractPatternElements(code, methodPattern, "method", "java", fileHash, atoms, compositions);
    }

    private void ExtractCppElements(
        string code,
        byte[] fileHash,
        List<AtomData> atoms,
        List<AtomComposition> compositions)
    {
        // Extract classes/structs
        var classPattern = @"(?:class|struct)\s+(\w+)";
        ExtractPatternElements(code, classPattern, "class", "cpp", fileHash, atoms, compositions);
        
        // Extract functions
        var functionPattern = @"\w+\s+(\w+)\s*\([^)]*\)\s*(?:{|;)";
        ExtractPatternElements(code, functionPattern, "function", "cpp", fileHash, atoms, compositions);
    }

    private void ExtractGenericElements(
        string code,
        byte[] fileHash,
        List<AtomData> atoms,
        List<AtomComposition> compositions)
    {
        // Generic function/method detection
        var functionPattern = @"function\s+(\w+)|def\s+(\w+)|(\w+)\s*\([^)]*\)\s*{";
        ExtractPatternElements(code, functionPattern, "function", "generic", fileHash, atoms, compositions);
    }

    private void ExtractPatternElements(
        string code,
        string pattern,
        string elementType,
        string language,
        byte[] fileHash,
        List<AtomData> atoms,
        List<AtomComposition> compositions)
    {
        var regex = new Regex(pattern, RegexOptions.Multiline);
        var matches = regex.Matches(code);
        
        int elementIndex = 0;
        foreach (Match match in matches)
        {
            // Find the first non-empty group
            string elementName = "";
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
            
            var elementBytes = Encoding.UTF8.GetBytes($"{elementType}:{elementName}");
            var elementHash = SHA256.HashData(elementBytes);
            
            var elementAtom = new AtomData
            {
                AtomicValue = elementBytes,
                ContentHash = elementHash,
                Modality = "code",
                Subtype = elementType,
                ContentType = $"text/x-{language}",
                CanonicalText = elementName,
                Metadata = $"{{\"language\":\"{language}\",\"type\":\"{elementType}\",\"name\":\"{elementName}\"}}"
            };
            
            if (!atoms.Any(a => a.ContentHash.SequenceEqual(elementHash)))
            {
                atoms.Add(elementAtom);
            }
            
            compositions.Add(new AtomComposition
            {
                ParentAtomHash = fileHash,
                ComponentAtomHash = elementHash,
                SequenceIndex = elementIndex++,
                Position = new SpatialPosition { X = 0, Y = 0, Z = 0 }
            });
        }
    }

    private void ExtractComments(
        string code,
        string language,
        byte[] fileHash,
        List<AtomData> atoms,
        List<AtomComposition> compositions)
    {
        // Single-line comments
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
            if (string.IsNullOrWhiteSpace(commentText) || commentText.Length < 5)
                continue;
            
            var commentBytes = Encoding.UTF8.GetBytes(commentText);
            var commentHash = SHA256.HashData(commentBytes);
            
            var commentAtom = new AtomData
            {
                AtomicValue = commentBytes.Length <= MaxAtomSize ? commentBytes : commentBytes.Take(MaxAtomSize).ToArray(),
                ContentHash = commentHash,
                Modality = "text",
                Subtype = "code-comment",
                ContentType = "text/plain",
                CanonicalText = commentText.Length <= 100 ? commentText : commentText[..100] + "...",
                Metadata = $"{{\"language\":\"{language}\",\"type\":\"comment\"}}"
            };
            
            if (!atoms.Any(a => a.ContentHash.SequenceEqual(commentHash)))
            {
                atoms.Add(commentAtom);
            }
            
            compositions.Add(new AtomComposition
            {
                ParentAtomHash = fileHash,
                ComponentAtomHash = commentHash,
                SequenceIndex = commentIndex++,
                Position = new SpatialPosition { X = 0, Y = 0, Z = 0 }
            });
        }
    }
}
