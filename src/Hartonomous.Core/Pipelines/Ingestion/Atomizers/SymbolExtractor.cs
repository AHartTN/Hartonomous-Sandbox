using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Hartonomous.Core.Pipelines.Ingestion.Atomizers
{
    /// <summary>
    /// Extracts symbols (classes, methods, variables, dependencies) from code.
    /// </summary>
    public class SymbolExtractor
    {
        /// <summary>
        /// Extracts all symbols from a C# syntax node.
        /// </summary>
        public List<SymbolInfo> ExtractSymbols(SyntaxNode node, SemanticModel semanticModel)
        {
            var symbols = new List<SymbolInfo>();

            // Extract method calls (external dependencies)
            foreach (var invocation in node.DescendantNodes().OfType<InvocationExpressionSyntax>())
            {
                var symbolInfo = semanticModel.GetSymbolInfo(invocation);
                if (symbolInfo.Symbol is IMethodSymbol methodSymbol)
                {
                    var isExternal = !methodSymbol.ContainingAssembly.Name.Contains("CSharpScript") &&
                                    !methodSymbol.ContainingAssembly.Name.Contains("mscorlib");

                    symbols.Add(new SymbolInfo
                    {
                        Name = methodSymbol.Name,
                        Type = "Method",
                        DataType = methodSymbol.ReturnType.ToString(),
                        IsExternal = isExternal
                    });
                }
            }

            // Extract variable declarations
            foreach (var variable in node.DescendantNodes().OfType<VariableDeclaratorSyntax>())
            {
                var symbol = semanticModel.GetDeclaredSymbol(variable);
                if (symbol is ILocalSymbol localSymbol)
                {
                    symbols.Add(new SymbolInfo
                    {
                        Name = localSymbol.Name,
                        Type = "Variable",
                        DataType = localSymbol.Type.ToString(),
                        IsExternal = false
                    });
                }
            }

            // Extract property accesses
            foreach (var propertyAccess in node.DescendantNodes().OfType<MemberAccessExpressionSyntax>())
            {
                var symbolInfo = semanticModel.GetSymbolInfo(propertyAccess);
                if (symbolInfo.Symbol is IPropertySymbol propertySymbol)
                {
                    var isExternal = !propertySymbol.ContainingAssembly.Name.Contains("CSharpScript");

                    symbols.Add(new SymbolInfo
                    {
                        Name = propertySymbol.Name,
                        Type = "Property",
                        DataType = propertySymbol.Type.ToString(),
                        IsExternal = isExternal
                    });
                }
            }

            return symbols.DistinctBy(s => s.Name).ToList();
        }

        /// <summary>
        /// Builds a call graph for method invocations.
        /// </summary>
        public Dictionary<string, List<string>> BuildCallGraph(SyntaxNode root, SemanticModel semanticModel)
        {
            var callGraph = new Dictionary<string, List<string>>();

            foreach (var methodDeclaration in root.DescendantNodes().OfType<MethodDeclarationSyntax>())
            {
                var methodName = methodDeclaration.Identifier.Text;
                var calls = new List<string>();

                foreach (var invocation in methodDeclaration.DescendantNodes().OfType<InvocationExpressionSyntax>())
                {
                    var symbolInfo = semanticModel.GetSymbolInfo(invocation);
                    if (symbolInfo.Symbol is IMethodSymbol calledMethod)
                    {
                        calls.Add(calledMethod.Name);
                    }
                }

                callGraph[methodName] = calls;
            }

            return callGraph;
        }

        /// <summary>
        /// Extracts import/export relationships.
        /// </summary>
        public List<string> ExtractDependencies(SyntaxNode root)
        {
            var dependencies = new List<string>();

            // Extract using directives
            foreach (var usingDirective in root.DescendantNodes().OfType<UsingDirectiveSyntax>())
            {
                var namespaceName = usingDirective.Name?.ToString();
                if (namespaceName != null)
                {
                    dependencies.Add(namespaceName);
                }
            }

            return dependencies.Distinct().ToList();
        }
    }

    /// <summary>
    /// Semantic chunking by code boundaries (class, method, etc.).
    /// </summary>
    public class SemanticChunker
    {
        /// <summary>
        /// Chunks code by semantic boundaries preserving context.
        /// For C#: class and method boundaries.
        /// For other languages: function/procedure boundaries or line-based.
        /// </summary>
        public List<CodeChunk> ChunkBySemanticBoundaries(string code, string language)
        {
            if (language == "CSharp")
            {
                return ChunkCSharp(code);
            }
            else if (language == "TSql")
            {
                return ChunkTSql(code);
            }
            else
            {
                return ChunkGeneric(code);
            }
        }

        private List<CodeChunk> ChunkCSharp(string code)
        {
            var chunks = new List<CodeChunk>();
            var tree = CSharpSyntaxTree.ParseText(code);
            var root = tree.GetRoot();

            foreach (var classDeclaration in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
            {
                chunks.Add(new CodeChunk
                {
                    Code = classDeclaration.ToFullString(),
                    Type = "Class",
                    Name = classDeclaration.Identifier.Text,
                    StartLine = classDeclaration.GetLocation().GetLineSpan().StartLinePosition.Line,
                    EndLine = classDeclaration.GetLocation().GetLineSpan().EndLinePosition.Line
                });

                foreach (var methodDeclaration in classDeclaration.DescendantNodes().OfType<MethodDeclarationSyntax>())
                {
                    chunks.Add(new CodeChunk
                    {
                        Code = methodDeclaration.ToFullString(),
                        Type = "Method",
                        Name = $"{classDeclaration.Identifier.Text}.{methodDeclaration.Identifier.Text}",
                        StartLine = methodDeclaration.GetLocation().GetLineSpan().StartLinePosition.Line,
                        EndLine = methodDeclaration.GetLocation().GetLineSpan().EndLinePosition.Line
                    });
                }
            }

            return chunks;
        }

        private List<CodeChunk> ChunkTSql(string code)
        {
            // T-SQL chunking by object boundaries
            // This is simplified - production would use full T-SQL parser
            var chunks = new List<CodeChunk>();
            var lines = code.Split('\n');
            var currentChunk = new List<string>();
            var currentName = "";
            var currentType = "";

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                
                if (line.Trim().StartsWith("CREATE", StringComparison.OrdinalIgnoreCase) ||
                    line.Trim().StartsWith("ALTER", StringComparison.OrdinalIgnoreCase))
                {
                    // Save previous chunk
                    if (currentChunk.Count > 0)
                    {
                        chunks.Add(new CodeChunk
                        {
                            Code = string.Join("\n", currentChunk),
                            Type = currentType,
                            Name = currentName,
                            StartLine = i - currentChunk.Count,
                            EndLine = i - 1
                        });
                        currentChunk.Clear();
                    }

                    // Start new chunk
                    if (line.Contains("PROCEDURE", StringComparison.OrdinalIgnoreCase))
                        currentType = "Procedure";
                    else if (line.Contains("FUNCTION", StringComparison.OrdinalIgnoreCase))
                        currentType = "Function";
                    else if (line.Contains("VIEW", StringComparison.OrdinalIgnoreCase))
                        currentType = "View";

                    currentName = ExtractObjectName(line);
                }

                currentChunk.Add(line);
            }

            // Add final chunk
            if (currentChunk.Count > 0)
            {
                chunks.Add(new CodeChunk
                {
                    Code = string.Join("\n", currentChunk),
                    Type = currentType,
                    Name = currentName,
                    StartLine = lines.Length - currentChunk.Count,
                    EndLine = lines.Length - 1
                });
            }

            return chunks;
        }

        private List<CodeChunk> ChunkGeneric(string code)
        {
            // Generic chunking by blank lines
            var chunks = new List<CodeChunk>();
            var lines = code.Split('\n');
            var currentChunk = new List<string>();
            int startLine = 0;

            for (int i = 0; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i]) && currentChunk.Count > 0)
                {
                    chunks.Add(new CodeChunk
                    {
                        Code = string.Join("\n", currentChunk),
                        Type = "Block",
                        StartLine = startLine,
                        EndLine = i - 1
                    });
                    currentChunk.Clear();
                    startLine = i + 1;
                }
                else
                {
                    currentChunk.Add(lines[i]);
                }
            }

            if (currentChunk.Count > 0)
            {
                chunks.Add(new CodeChunk
                {
                    Code = string.Join("\n", currentChunk),
                    Type = "Block",
                    StartLine = startLine,
                    EndLine = lines.Length - 1
                });
            }

            return chunks;
        }

        private string ExtractObjectName(string line)
        {
            var parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < parts.Length - 1; i++)
            {
                if (parts[i].Equals("PROCEDURE", StringComparison.OrdinalIgnoreCase) ||
                    parts[i].Equals("FUNCTION", StringComparison.OrdinalIgnoreCase) ||
                    parts[i].Equals("VIEW", StringComparison.OrdinalIgnoreCase))
                {
                    return parts[i + 1].Trim('[', ']');
                }
            }
            return "Unknown";
        }
    }

    /// <summary>
    /// Code chunk with metadata.
    /// </summary>
    public class CodeChunk
    {
        public string Code { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string? Name { get; set; }
        public int StartLine { get; set; }
        public int EndLine { get; set; }
    }
}
