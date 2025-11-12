using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Hartonomous.Core.Pipelines.Ingestion.Atomizers
{
    /// <summary>
    /// Detects programming language from file extension, shebang, or content analysis.
    /// </summary>
    public class LanguageDetector
    {
        private static readonly Dictionary<string, string> ExtensionMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // .NET Languages
            { ".cs", "CSharp" },
            { ".csx", "CSharp" },
            { ".fs", "FSharp" },
            { ".fsx", "FSharp" },
            { ".vb", "VisualBasic" },
            
            // SQL
            { ".sql", "TSql" },
            { ".tsql", "TSql" },
            { ".pgsql", "PostgreSQL" },
            { ".plsql", "PLSQL" },
            
            // Web
            { ".js", "JavaScript" },
            { ".jsx", "JavaScript" },
            { ".ts", "TypeScript" },
            { ".tsx", "TypeScript" },
            { ".html", "HTML" },
            { ".htm", "HTML" },
            { ".css", "CSS" },
            { ".scss", "SCSS" },
            { ".sass", "SASS" },
            { ".less", "LESS" },
            
            // Python
            { ".py", "Python" },
            { ".pyw", "Python" },
            { ".pyi", "Python" },
            
            // Java/JVM
            { ".java", "Java" },
            { ".kt", "Kotlin" },
            { ".kts", "Kotlin" },
            { ".scala", "Scala" },
            { ".groovy", "Groovy" },
            
            // C/C++
            { ".c", "C" },
            { ".h", "C" },
            { ".cpp", "CPP" },
            { ".cxx", "CPP" },
            { ".cc", "CPP" },
            { ".hpp", "CPP" },
            { ".hxx", "CPP" },
            
            // Go
            { ".go", "Go" },
            
            // Rust
            { ".rs", "Rust" },
            
            // Ruby
            { ".rb", "Ruby" },
            
            // PHP
            { ".php", "PHP" },
            
            // Shell
            { ".sh", "Shell" },
            { ".bash", "Bash" },
            { ".zsh", "Zsh" },
            { ".ps1", "PowerShell" },
            
            // Data formats
            { ".json", "JSON" },
            { ".xml", "XML" },
            { ".yaml", "YAML" },
            { ".yml", "YAML" },
            { ".toml", "TOML" },
            
            // Markup
            { ".md", "Markdown" },
            { ".rst", "reStructuredText" },
            
            // Config
            { ".config", "XML" },
            { ".csproj", "XML" },
            { ".fsproj", "XML" },
            { ".vbproj", "XML" },
            { ".sln", "SolutionFile" }
        };

        /// <summary>
        /// Detects language from file path and/or content.
        /// </summary>
        public string Detect(string content, string? filePath = null)
        {
            // 1. Try file extension first
            if (filePath != null)
            {
                var extension = Path.GetExtension(filePath);
                if (ExtensionMap.TryGetValue(extension, out var language))
                {
                    return language;
                }
            }

            // 2. Check shebang (#!/usr/bin/env python, etc.)
            var shebangLanguage = DetectFromShebang(content);
            if (shebangLanguage != null)
            {
                return shebangLanguage;
            }

            // 3. Content-based heuristics
            return DetectFromContent(content);
        }

        /// <summary>
        /// Detects language from shebang line.
        /// </summary>
        private string? DetectFromShebang(string content)
        {
            if (!content.StartsWith("#!"))
                return null;

            var firstLine = content.Split('\n')[0].ToLower();

            if (firstLine.Contains("python")) return "Python";
            if (firstLine.Contains("ruby")) return "Ruby";
            if (firstLine.Contains("node") || firstLine.Contains("javascript")) return "JavaScript";
            if (firstLine.Contains("bash") || firstLine.Contains("sh")) return "Bash";
            if (firstLine.Contains("perl")) return "Perl";
            if (firstLine.Contains("php")) return "PHP";

            return null;
        }

        /// <summary>
        /// Detects language from content patterns.
        /// </summary>
        private string DetectFromContent(string content)
        {
            var patterns = new[]
            {
                // C# patterns
                (Pattern: new Regex(@"\bnamespace\s+[\w\.]+|using\s+System|public\s+class\s+\w+"), Language: "CSharp"),
                
                // T-SQL patterns
                (Pattern: new Regex(@"\bCREATE\s+(PROCEDURE|FUNCTION|VIEW|TABLE)|SELECT\s+.*\s+FROM|EXEC\s+", RegexOptions.IgnoreCase), Language: "TSql"),
                
                // JavaScript/TypeScript patterns
                (Pattern: new Regex(@"\bfunction\s+\w+\s*\(|const\s+\w+\s*=|let\s+\w+\s*=|var\s+\w+\s*=|export\s+(default|function|class)"), Language: "JavaScript"),
                (Pattern: new Regex(@"\binterface\s+\w+|type\s+\w+\s*=|enum\s+\w+"), Language: "TypeScript"),
                
                // Python patterns
                (Pattern: new Regex(@"\bdef\s+\w+\s*\(|class\s+\w+\s*\(|import\s+\w+|from\s+\w+\s+import"), Language: "Python"),
                
                // Java patterns
                (Pattern: new Regex(@"\bpublic\s+class\s+\w+|package\s+[\w\.]+|import\s+java\."), Language: "Java"),
                
                // Go patterns
                (Pattern: new Regex(@"\bpackage\s+\w+|func\s+\w+\s*\(|import\s+\("), Language: "Go"),
                
                // Rust patterns
                (Pattern: new Regex(@"\bfn\s+\w+\s*\(|use\s+\w+|pub\s+struct\s+\w+|impl\s+\w+"), Language: "Rust"),
                
                // HTML patterns
                (Pattern: new Regex(@"<html|<head>|<body>|<!DOCTYPE\s+html>", RegexOptions.IgnoreCase), Language: "HTML"),
                
                // JSON patterns
                (Pattern: new Regex(@"^\s*\{[\s\S]*\}\s*$|^\s*\[[\s\S]*\]\s*$"), Language: "JSON")
            };

            foreach (var (pattern, language) in patterns)
            {
                if (pattern.IsMatch(content))
                {
                    return language;
                }
            }

            return "Unknown";
        }

        /// <summary>
        /// Gets file extension for a language.
        /// </summary>
        public static string GetFileExtension(string language)
        {
            var entry = ExtensionMap.FirstOrDefault(kv => kv.Value == language);
            return entry.Key ?? ".txt";
        }
    }
}
