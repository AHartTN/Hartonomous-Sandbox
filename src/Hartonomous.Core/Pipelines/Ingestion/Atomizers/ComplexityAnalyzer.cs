using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Hartonomous.Core.Pipelines.Ingestion.Atomizers
{
    /// <summary>
    /// Computes code complexity metrics (cyclomatic complexity, Halstead, maintainability index).
    /// </summary>
    public class ComplexityAnalyzer
    {
        /// <summary>
        /// Computes cyclomatic complexity for a C# syntax node.
        /// McCabe Cyclomatic Complexity: M = E - N + 2P
        /// Simplified: Count decision points + 1
        /// </summary>
        public int ComputeCyclomaticComplexity(SyntaxNode node)
        {
            int complexity = 1; // Base complexity

            // Count decision points (if, while, for, foreach, case, catch, &&, ||, ?:)
            complexity += node.DescendantNodes().OfType<IfStatementSyntax>().Count();
            complexity += node.DescendantNodes().OfType<WhileStatementSyntax>().Count();
            complexity += node.DescendantNodes().OfType<ForStatementSyntax>().Count();
            complexity += node.DescendantNodes().OfType<ForEachStatementSyntax>().Count();
            complexity += node.DescendantNodes().OfType<DoStatementSyntax>().Count();
            complexity += node.DescendantNodes().OfType<SwitchSectionSyntax>().Count();
            complexity += node.DescendantNodes().OfType<CatchClauseSyntax>().Count();
            complexity += node.DescendantNodes().OfType<ConditionalExpressionSyntax>().Count(); // ?:

            // Count logical operators (&&, ||)
            complexity += node.DescendantNodes()
                .OfType<BinaryExpressionSyntax>()
                .Count(b => b.IsKind(SyntaxKind.LogicalAndExpression) || b.IsKind(SyntaxKind.LogicalOrExpression));

            // Count null-coalescing operators (??)
            complexity += node.DescendantNodes()
                .OfType<BinaryExpressionSyntax>()
                .Count(b => b.IsKind(SyntaxKind.CoalesceExpression));

            return complexity;
        }

        /// <summary>
        /// Computes Halstead metrics for code.
        /// Based on operators and operands.
        /// </summary>
        public HalsteadMetrics ComputeHalsteadMetrics(SyntaxNode node)
        {
            var operators = new HashSet<string>();
            var operands = new HashSet<string>();
            var totalOperators = 0;
            var totalOperands = 0;

            // Count operators (simplified - counts tokens)
            foreach (var token in node.DescendantTokens())
            {
                if (token.IsKind(SyntaxKind.PlusToken) ||
                    token.IsKind(SyntaxKind.MinusToken) ||
                    token.IsKind(SyntaxKind.AsteriskToken) ||
                    token.IsKind(SyntaxKind.SlashToken) ||
                    token.IsKind(SyntaxKind.EqualsEqualsToken) ||
                    token.IsKind(SyntaxKind.ExclamationEqualsToken) ||
                    token.IsKind(SyntaxKind.LessThanToken) ||
                    token.IsKind(SyntaxKind.GreaterThanToken) ||
                    token.IsKind(SyntaxKind.AmpersandAmpersandToken) ||
                    token.IsKind(SyntaxKind.BarBarToken))
                {
                    operators.Add(token.Text);
                    totalOperators++;
                }
            }

            // Count operands (variables, literals)
            foreach (var identifier in node.DescendantTokens().Where(t => t.IsKind(SyntaxKind.IdentifierToken)))
            {
                operands.Add(identifier.Text);
                totalOperands++;
            }

            var n1 = operators.Count; // Distinct operators
            var n2 = operands.Count;  // Distinct operands
            var N1 = totalOperators;  // Total operators
            var N2 = totalOperands;   // Total operands

            var vocabulary = n1 + n2;
            var length = N1 + N2;
            var volume = length * (vocabulary > 0 ? Math.Log2(vocabulary) : 0);
            var difficulty = (n1 > 0 && N2 > 0) ? (n1 / 2.0) * (N2 / (double)n2) : 0;
            var effort = volume * difficulty;

            return new HalsteadMetrics
            {
                Vocabulary = vocabulary,
                Length = length,
                Volume = volume,
                Difficulty = difficulty,
                Effort = effort,
                TimeToProgram = effort / 18.0, // Seconds (Halstead's formula)
                BugsDelivered = volume / 3000.0 // Estimated bugs
            };
        }

        /// <summary>
        /// Computes maintainability index (0-100 scale).
        /// MI = max(0, (171 - 5.2 * ln(V) - 0.23 * G - 16.2 * ln(LOC)) * 100 / 171)
        /// Where: V = Halstead Volume, G = Cyclomatic Complexity, LOC = Lines of Code
        /// </summary>
        public double ComputeMaintainabilityIndex(SyntaxNode node, int linesOfCode)
        {
            var halstead = ComputeHalsteadMetrics(node);
            var complexity = ComputeCyclomaticComplexity(node);

            if (halstead.Volume == 0 || linesOfCode == 0)
                return 100; // Perfect score for trivial code

            var mi = 171 - 5.2 * Math.Log(halstead.Volume) - 0.23 * complexity - 16.2 * Math.Log(linesOfCode);
            mi = Math.Max(0, mi) * 100.0 / 171.0;

            return mi;
        }

        /// <summary>
        /// Computes lines of code metrics (total, source, comment).
        /// </summary>
        public LinesOfCodeMetrics ComputeLinesOfCode(string code)
        {
            var lines = code.Split('\n');
            var totalLines = lines.Length;
            var blankLines = lines.Count(l => string.IsNullOrWhiteSpace(l));
            var commentLines = lines.Count(l => l.TrimStart().StartsWith("//") || l.TrimStart().StartsWith("///"));
            
            // Multi-line comments (simplified detection)
            var multiLineCommentLines = 0;
            var inMultiLineComment = false;
            foreach (var line in lines)
            {
                if (line.TrimStart().StartsWith("/*"))
                    inMultiLineComment = true;
                
                if (inMultiLineComment)
                    multiLineCommentLines++;
                
                if (line.TrimEnd().EndsWith("*/"))
                    inMultiLineComment = false;
            }

            var sourceLines = totalLines - blankLines - commentLines - multiLineCommentLines;

            return new LinesOfCodeMetrics
            {
                Total = totalLines,
                Source = Math.Max(0, sourceLines),
                Comment = commentLines + multiLineCommentLines,
                Blank = blankLines
            };
        }
    }

    /// <summary>
    /// Halstead complexity metrics.
    /// </summary>
    public class HalsteadMetrics
    {
        public int Vocabulary { get; set; }
        public int Length { get; set; }
        public double Volume { get; set; }
        public double Difficulty { get; set; }
        public double Effort { get; set; }
        public double TimeToProgram { get; set; } // In seconds
        public double BugsDelivered { get; set; }
    }

    /// <summary>
    /// Lines of code metrics.
    /// </summary>
    public class LinesOfCodeMetrics
    {
        public int Total { get; set; }
        public int Source { get; set; }
        public int Comment { get; set; }
        public int Blank { get; set; }
    }
}
