using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Hartonomous.Core.Pipelines.Ingestion.Atomizers
{
    /// <summary>
    /// Comprehensive code ingestion atomizer with language detection, AST parsing,
    /// semantic chunking, symbol extraction, and quality analysis.
    /// PRIMARY FOCUS: T-SQL, C#, CLR functions, with multi-language support.
    /// </summary>
    public class CodeAtomizer : IAtomizer<string>
    {
        private readonly LanguageDetector _languageDetector;
        private readonly SemanticChunker _semanticChunker;
        private readonly SymbolExtractor _symbolExtractor;
        private readonly ComplexityAnalyzer _complexityAnalyzer;

        public string Modality => "Code";

        public CodeAtomizer()
        {
            _languageDetector = new LanguageDetector();
            _semanticChunker = new SemanticChunker();
            _symbolExtractor = new SymbolExtractor();
            _complexityAnalyzer = new ComplexityAnalyzer();
        }

        /// <summary>
        /// Atomizes code into semantic chunks with full metadata.
        /// </summary>
        public async IAsyncEnumerable<AtomCandidate> AtomizeAsync(
            string source,
            AtomizationContext context,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            // 1. Detect language if not provided
            var language = context.Hints?.TryGetValue("language", out var langObj) == true
                ? langObj?.ToString()
                : null;
            
            var detectedLanguage = language ?? _languageDetector.Detect(source, context.SourceUri);

            // 2. Parse AST (currently supports C# via Roslyn)
            List<CodeAtomResult> results;
            if (detectedLanguage == "CSharp")
            {
                results = AtomizeCSharp(source, context.SourceUri);
            }
            else if (detectedLanguage == "TSql")
            {
                results = AtomizeTSql(source, context.SourceUri);
            }
            else
            {
                // Fallback: generic line-based chunking for unsupported languages
                results = AtomizeGeneric(source, context.SourceUri, detectedLanguage);
            }

            // 3. Convert to AtomCandidate format
            foreach (var result in results)
            {
                if (cancellationToken.IsCancellationRequested)
                    yield break;

                var metadata = new Dictionary<string, object>
                {
                    ["language"] = result.Language,
                    ["codeType"] = result.CodeType,
                    ["symbolName"] = result.SymbolName ?? "",
                    ["namespace"] = result.Namespace ?? "",
                    ["linesOfCode"] = result.LinesOfCode,
                    ["cyclomaticComplexity"] = result.CyclomaticComplexity,
                    ["codeHash"] = HashToString(result.CodeHash)
                };

                if (result.Imports?.Any() == true)
                    metadata["imports"] = string.Join(", ", result.Imports);
                if (result.Modifiers?.Any() == true)
                    metadata["modifiers"] = string.Join(" ", result.Modifiers);
                if (result.BaseTypes?.Any() == true)
                    metadata["baseTypes"] = string.Join(", ", result.BaseTypes);
                if (!string.IsNullOrEmpty(result.ReturnType))
                    metadata["returnType"] = result.ReturnType;
                if (result.Parameters?.Any() == true)
                    metadata["parameters"] = string.Join(", ", result.Parameters);
                if (!string.IsNullOrEmpty(result.ParseError))
                    metadata["parseError"] = result.ParseError;

                var semantics = new Dictionary<string, object>();
                if (result.Symbols?.Any() == true)
                {
                    semantics["symbols"] = result.Symbols.Select(s => new Dictionary<string, object>
                    {
                        ["name"] = s.Name,
                        ["type"] = s.Type,
                        ["dataType"] = s.DataType ?? "",
                        ["isExternal"] = s.IsExternal
                    }).ToList();
                }

                yield return new AtomCandidate
                {
                    Modality = "Code",
                    Subtype = result.CodeType,
                    CanonicalText = result.Code,
                    SourceUri = context.SourceUri,
                    SourceType = context.SourceType,
                    Boundary = new AtomBoundary
                    {
                        StartLineNumber = result.StartLine,
                        EndLineNumber = result.EndLine,
                        StructuralPath = string.IsNullOrEmpty(result.Namespace)
                            ? result.SymbolName
                            : $"{result.Namespace}/{result.SymbolName}"
                    },
                    Metadata = metadata,
                    Semantics = semantics.Count > 0 ? semantics : null,
                    QualityScore = ComputeQualityScore(result),
                    HashInput = HashToString(result.CodeHash)
                };

                await Task.Yield(); // Allow cooperative cancellation
            }
        }

        /// <summary>
        /// Atomizes C# code using Roslyn AST parsing.
        /// Chunks by class/method boundaries with full context.
        /// </summary>
        private List<CodeAtomResult> AtomizeCSharp(string code, string? filePath)
        {
            var results = new List<CodeAtomResult>();

            try
            {
                var tree = CSharpSyntaxTree.ParseText(code);
                var root = tree.GetRoot();
                var compilation = CSharpCompilation.Create("Analysis")
                    .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
                    .AddSyntaxTrees(tree);
                var semanticModel = compilation.GetSemanticModel(tree);

                // Extract namespace context
                var namespaceDeclaration = root.DescendantNodes().OfType<BaseNamespaceDeclarationSyntax>().FirstOrDefault();
                var namespaceName = namespaceDeclaration?.Name.ToString();

                // Extract using statements
                var usings = root.DescendantNodes().OfType<UsingDirectiveSyntax>()
                    .Select(u => u.Name?.ToString())
                    .Where(n => n != null)
                    .ToList();

                // Atomize by class
                foreach (var classDeclaration in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
                {
                    var className = classDeclaration.Identifier.Text;
                    var classCode = classDeclaration.ToFullString();
                    var classSymbol = semanticModel.GetDeclaredSymbol(classDeclaration);

                    // Extract class-level info
                    var classAtom = new CodeAtomResult
                    {
                        Code = classCode,
                        Language = "CSharp",
                        CodeType = "Class",
                        SymbolName = className,
                        Namespace = namespaceName,
                        FilePath = filePath,
                        StartLine = classDeclaration.GetLocation().GetLineSpan().StartLinePosition.Line,
                        EndLine = classDeclaration.GetLocation().GetLineSpan().EndLinePosition.Line,
                        Imports = usings!,
                        Modifiers = classDeclaration.Modifiers.Select(m => m.Text).ToList(),
                        BaseTypes = classDeclaration.BaseList?.Types.Select(t => t.ToString()).ToList() ?? new List<string>(),
                        Summary = ExtractDocComment(classDeclaration)
                    };

                    // Compute complexity
                    classAtom.CyclomaticComplexity = _complexityAnalyzer.ComputeCyclomaticComplexity(classDeclaration);
                    classAtom.LinesOfCode = classAtom.EndLine - classAtom.StartLine + 1;

                    // Extract symbols
                    classAtom.Symbols = _symbolExtractor.ExtractSymbols(classDeclaration, semanticModel);

                    results.Add(classAtom);

                    // Atomize by method within class
                    foreach (var methodDeclaration in classDeclaration.DescendantNodes().OfType<MethodDeclarationSyntax>())
                    {
                        var methodName = methodDeclaration.Identifier.Text;
                        var methodCode = methodDeclaration.ToFullString();
                        var methodSymbol = semanticModel.GetDeclaredSymbol(methodDeclaration);

                        var methodAtom = new CodeAtomResult
                        {
                            Code = methodCode,
                            Language = "CSharp",
                            CodeType = "Method",
                            SymbolName = $"{className}.{methodName}",
                            Namespace = namespaceName,
                            FilePath = filePath,
                            StartLine = methodDeclaration.GetLocation().GetLineSpan().StartLinePosition.Line,
                            EndLine = methodDeclaration.GetLocation().GetLineSpan().EndLinePosition.Line,
                            Imports = usings!,
                            Modifiers = methodDeclaration.Modifiers.Select(m => m.Text).ToList(),
                            ReturnType = methodDeclaration.ReturnType.ToString(),
                            Parameters = methodDeclaration.ParameterList.Parameters.Select(p => $"{p.Type} {p.Identifier}").ToList(),
                            Summary = ExtractDocComment(methodDeclaration)
                        };

                        // Compute method-level complexity
                        methodAtom.CyclomaticComplexity = _complexityAnalyzer.ComputeCyclomaticComplexity(methodDeclaration);
                        methodAtom.LinesOfCode = methodAtom.EndLine - methodAtom.StartLine + 1;

                        // Extract method symbols
                        methodAtom.Symbols = _symbolExtractor.ExtractSymbols(methodDeclaration, semanticModel);

                        results.Add(methodAtom);
                    }
                }

                // Compute content hashes
                foreach (var result in results)
                {
                    result.CodeHash = ComputeHash(result.Code);
                }
            }
            catch (Exception ex)
            {
                // Return error as atom for diagnostics
                results.Add(new CodeAtomResult
                {
                    Code = code,
                    Language = "CSharp",
                    CodeType = "Error",
                    FilePath = filePath,
                    ParseError = ex.Message
                });
            }

            return results;
        }

        /// <summary>
        /// Atomizes T-SQL code by procedure/function/view boundaries.
        /// Uses regex-based parsing (full T-SQL grammar parser would require antlr4).
        /// </summary>
        private List<CodeAtomResult> AtomizeTSql(string code, string? filePath)
        {
            var results = new List<CodeAtomResult>();

            try
            {
                // Regex patterns for T-SQL objects
                var procedurePattern = @"CREATE\s+(OR\s+ALTER\s+)?PROCEDURE\s+(\[?[\w\.]+\]?)(.*?)(?=CREATE\s+(OR\s+ALTER\s+)?PROCEDURE|CREATE\s+(OR\s+ALTER\s+)?FUNCTION|CREATE\s+VIEW|\z)";
                var functionPattern = @"CREATE\s+(OR\s+ALTER\s+)?FUNCTION\s+(\[?[\w\.]+\]?)(.*?)(?=CREATE\s+(OR\s+ALTER\s+)?PROCEDURE|CREATE\s+(OR\s+ALTER\s+)?FUNCTION|CREATE\s+VIEW|\z)";
                var viewPattern = @"CREATE\s+(OR\s+ALTER\s+)?VIEW\s+(\[?[\w\.]+\]?)(.*?)(?=CREATE\s+(OR\s+ALTER\s+)?PROCEDURE|CREATE\s+(OR\s+ALTER\s+)?FUNCTION|CREATE\s+VIEW|\z)";

                var patterns = new[]
                {
                    (Pattern: procedurePattern, Type: "Procedure"),
                    (Pattern: functionPattern, Type: "Function"),
                    (Pattern: viewPattern, Type: "View")
                };

                foreach (var (pattern, type) in patterns)
                {
                    var matches = Regex.Matches(code, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
                    
                    foreach (Match match in matches)
                    {
                        var objectName = match.Groups[2].Value.Trim('[', ']');
                        var objectCode = match.Value;

                        var atom = new CodeAtomResult
                        {
                            Code = objectCode,
                            Language = "TSql",
                            CodeType = type,
                            SymbolName = objectName,
                            FilePath = filePath,
                            LinesOfCode = objectCode.Split('\n').Length
                        };

                        // Extract schema and object name
                        var parts = objectName.Split('.');
                        if (parts.Length == 2)
                        {
                            atom.Schema = parts[0];
                            atom.SymbolName = parts[1];
                        }

                        // Compute hash
                        atom.CodeHash = ComputeHash(objectCode);

                        // Extract parameters (simplified)
                        var paramMatch = Regex.Match(objectCode, @"\((.*?)\)", RegexOptions.Singleline);
                        if (paramMatch.Success)
                        {
                            var paramText = paramMatch.Groups[1].Value;
                            atom.Parameters = paramText.Split(',')
                                .Select(p => p.Trim())
                                .Where(p => !string.IsNullOrWhiteSpace(p))
                                .ToList();
                        }

                        results.Add(atom);
                    }
                }

                // If no objects found, return entire file as single atom
                if (results.Count == 0)
                {
                    results.Add(new CodeAtomResult
                    {
                        Code = code,
                        Language = "TSql",
                        CodeType = "Script",
                        FilePath = filePath,
                        LinesOfCode = code.Split('\n').Length,
                        CodeHash = ComputeHash(code)
                    });
                }
            }
            catch (Exception ex)
            {
                results.Add(new CodeAtomResult
                {
                    Code = code,
                    Language = "TSql",
                    CodeType = "Error",
                    FilePath = filePath,
                    ParseError = ex.Message
                });
            }

            return results;
        }

        /// <summary>
        /// Generic line-based chunking for unsupported languages.
        /// Chunks by blank lines or fixed line count.
        /// </summary>
        private List<CodeAtomResult> AtomizeGeneric(string code, string? filePath, string language)
        {
            var results = new List<CodeAtomResult>();
            var lines = code.Split('\n');
            var chunkSize = 50; // lines per chunk
            
            for (int i = 0; i < lines.Length; i += chunkSize)
            {
                var chunk = string.Join("\n", lines.Skip(i).Take(chunkSize));
                
                results.Add(new CodeAtomResult
                {
                    Code = chunk,
                    Language = language,
                    CodeType = "Snippet",
                    FilePath = filePath,
                    StartLine = i,
                    EndLine = Math.Min(i + chunkSize - 1, lines.Length - 1),
                    LinesOfCode = Math.Min(chunkSize, lines.Length - i),
                    CodeHash = ComputeHash(chunk)
                });
            }

            return results;
        }

        /// <summary>
        /// Extracts XML doc comment summary.
        /// </summary>
        private string? ExtractDocComment(SyntaxNode node)
        {
            var trivia = node.GetLeadingTrivia()
                .FirstOrDefault(t => t.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia) ||
                                     t.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia));

            if (trivia == default) return null;

            var xml = trivia.GetStructure()?.ToString();
            if (xml == null) return null;

            // Extract <summary> content
            var summaryMatch = Regex.Match(xml, @"<summary>(.*?)</summary>", RegexOptions.Singleline);
            if (summaryMatch.Success)
            {
                return summaryMatch.Groups[1].Value.Trim().Replace("///", "").Trim();
            }

            return null;
        }

        /// <summary>
        /// Computes SHA256 hash for deduplication.
        /// </summary>
        private byte[] ComputeHash(string code)
        {
            using (var sha256 = SHA256.Create())
            {
                return sha256.ComputeHash(Encoding.UTF8.GetBytes(code));
            }
        }

        /// <summary>
        /// Computes quality score (0-1) based on cyclomatic complexity, LOC, and error presence.
        /// </summary>
        private double ComputeQualityScore(CodeAtomResult result)
        {
            // Penalize parse errors
            if (!string.IsNullOrEmpty(result.ParseError))
                return 0.3;

            double score = 1.0;

            // Penalize high cyclomatic complexity (>10 is concerning, >20 is problematic)
            if (result.CyclomaticComplexity > 20)
                score *= 0.5;
            else if (result.CyclomaticComplexity > 10)
                score *= 0.7;

            // Penalize very short or very long methods
            if (result.LinesOfCode < 3)
                score *= 0.6; // Too short, likely trivial
            else if (result.LinesOfCode > 300)
                score *= 0.5; // Too long, should be refactored

            // Reward documented code
            if (!string.IsNullOrEmpty(result.Summary))
                score *= 1.1;

            // Ensure score stays in 0-1 range
            return Math.Min(1.0, Math.Max(0.0, score));
        }

        /// <summary>
        /// Converts byte[] hash to hex string.
        /// </summary>
        private string HashToString(byte[]? hash)
        {
            if (hash == null) return string.Empty;
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }
    }

    /// <summary>
    /// Result of code atomization.
    /// </summary>
    public class CodeAtomResult
    {
        public string Code { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
        public string CodeType { get; set; } = string.Empty; // Class, Method, Function, Procedure, View, etc.
        public string? SymbolName { get; set; }
        public string? Namespace { get; set; }
        public string? Schema { get; set; } // For T-SQL
        public string? FilePath { get; set; }
        public int StartLine { get; set; }
        public int EndLine { get; set; }
        public int LinesOfCode { get; set; }
        public List<string> Imports { get; set; } = new List<string>();
        public List<string> Modifiers { get; set; } = new List<string>(); // public, static, etc.
        public List<string> BaseTypes { get; set; } = new List<string>(); // Inheritance/interfaces
        public string? ReturnType { get; set; }
        public List<string> Parameters { get; set; } = new List<string>();
        public string? Summary { get; set; }
        public int CyclomaticComplexity { get; set; }
        public List<SymbolInfo> Symbols { get; set; } = new List<SymbolInfo>();
        public byte[]? CodeHash { get; set; }
        public string? ParseError { get; set; }
    }

    /// <summary>
    /// Symbol information for cross-reference tracking.
    /// </summary>
    public class SymbolInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // Variable, Method, Property, etc.
        public string? DataType { get; set; }
        public bool IsExternal { get; set; } // External dependency
    }
}
