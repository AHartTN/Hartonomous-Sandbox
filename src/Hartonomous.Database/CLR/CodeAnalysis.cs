using Microsoft.SqlServer.Server;
using System;
using System.Data.SqlTypes;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace Hartonomous.Clr
{
    /// <summary>
    /// SQL CLR functions for code analysis using simplified pattern-based heuristics.
    /// NOTE: This is a stub implementation that replaces the Roslyn-based AST parser.
    /// Microsoft.CodeAnalysis (Roslyn) is NOT compatible with SQL CLR due to:
    /// - Massive assembly size (30+ MB)
    /// - Complex dependencies not supported in SQL CLR
    /// - Dynamic code generation (not allowed in SQL CLR)
    /// 
    /// FUTURE: Implement full code analysis via external service or Azure Function.
    /// </summary>
    public static class CodeAnalysis
    {
        private const int VECTOR_DIMENSION = 512;

        /// <summary>
        /// Generates a simplified code structure vector using pattern-based heuristics.
        /// STUB IMPLEMENTATION: Replaces Roslyn AST traversal with regex pattern matching.
        /// Returns a vector representing code structure characteristics:
        /// - Method counts, class counts, using statements
        /// - Keyword frequency, brace depth, comment density
        /// - Line/character counts, average line length
        /// </summary>
        [SqlFunction(IsDeterministic = true, DataAccess = DataAccessKind.None)]
        public static SqlString clr_GenerateCodeAstVector(SqlString sourceCode)
        {
            if (sourceCode.IsNull || string.IsNullOrWhiteSpace(sourceCode.Value))
            {
                return SqlString.Null;
            }

            try
            {
                string code = sourceCode.Value;
                var vector = new float[VECTOR_DIMENSION];

                // Pattern-based code metrics (simplified AST simulation)
                vector[0] = CountMatches(code, @"\bclass\s+\w+");              // Class declarations
                vector[1] = CountMatches(code, @"\binterface\s+\w+");          // Interface declarations
                vector[2] = CountMatches(code, @"\b(public|private|protected)\s+\w+\s+\w+\s*\("); // Methods
                vector[3] = CountMatches(code, @"\busing\s+\w+");              // Using statements
                vector[4] = CountMatches(code, @"\bnamespace\s+\w+");          // Namespaces
                vector[5] = CountMatches(code, @"\bif\s*\(");                  // If statements
                vector[6] = CountMatches(code, @"\bfor\s*\(");                 // For loops
                vector[7] = CountMatches(code, @"\bwhile\s*\(");               // While loops
                vector[8] = CountMatches(code, @"\bforeach\s*\(");             // Foreach loops
                vector[9] = CountMatches(code, @"\bswitch\s*\(");              // Switch statements
                vector[10] = CountMatches(code, @"\btry\s*\{");                // Try blocks
                vector[11] = CountMatches(code, @"\bcatch\s*\(");              // Catch blocks
                vector[12] = CountMatches(code, @"\bfinally\s*\{");            // Finally blocks
                vector[13] = CountMatches(code, @"\breturn\s+");               // Return statements
                vector[14] = CountMatches(code, @"\bthrow\s+");                // Throw statements
                vector[15] = CountMatches(code, @"\basync\s+");                // Async keywords
                vector[16] = CountMatches(code, @"\bawait\s+");                // Await keywords
                vector[17] = CountMatches(code, @"\bvar\s+\w+\s*=");           // Var declarations
                vector[18] = CountMatches(code, @"\bnew\s+\w+");               // Object instantiations
                vector[19] = CountMatches(code, @"=>"); // Lambda expressions
                
                // Structural metrics
                vector[20] = code.Split('\n').Length;                          // Line count
                vector[21] = code.Length;                                      // Character count
                vector[22] = CountMatches(code, @"\{");                        // Opening braces
                vector[23] = CountMatches(code, @"\}");                        // Closing braces
                vector[24] = CountMatches(code, @"//.*");                      // Single-line comments
                vector[25] = CountMatches(code, @"/\*[\s\S]*?\*/");            // Multi-line comments
                vector[26] = CountMatches(code, @"\[SqlFunction");             // SQL CLR attributes
                vector[27] = CountMatches(code, @"\[SqlProcedure");
                vector[28] = CountMatches(code, @"\[SqlUserDefinedAggregate");
                vector[29] = CountMatches(code, @"\[SqlUserDefinedType");

                // Calculate derived metrics
                var lines = code.Split('\n');
                vector[30] = lines.Length > 0 ? code.Length / lines.Length : 0; // Avg line length
                vector[31] = (vector[24] + vector[25]) / lines.Length;          // Comment density

                // Normalize vector using L2 normalization
                double magnitude = 0;
                for (int i = 0; i < 32; i++)
                {
                    magnitude += vector[i] * vector[i];
                }
                magnitude = Math.Sqrt(magnitude);

                if (magnitude > 0)
                {
                    for (int i = 0; i < 32; i++)
                    {
                        vector[i] = (float)(vector[i] / magnitude);
                    }
                }

                // Truncate to actual used dimensions
                var result = new float[32];
                Array.Copy(vector, result, 32);

                return new SqlString(JsonConvert.SerializeObject(result));
            }
            catch (Exception ex)
            {
                // Return error as JSON for debugging in SQL
                return new SqlString(JsonConvert.SerializeObject(new { error = ex.Message, stackTrace = ex.StackTrace }));
            }
        }

        /// <summary>
        /// Count regex pattern matches in code
        /// </summary>
        private static int CountMatches(string code, string pattern)
        {
            try
            {
                return Regex.Matches(code, pattern).Count;
            }
            catch
            {
                return 0;
            }
        }
    }
}
