using Microsoft.SqlServer.Server;
using System;
using System.Data.SqlTypes;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;
using Newtonsoft.Json;

namespace SqlClrFunctions
{
    /// <summary>
    /// SQL CLR functions for code analysis and AST-as-GEOMETRY pipeline.
    /// </summary>
    public static class CodeAnalysis
    {
        // A simple vectorization scheme: map syntax kinds to a fixed-size vector.
        private const int VECTOR_DIMENSION = 512;
        private static readonly Dictionary<SyntaxKind, int> _syntaxKindToDimension;

        static CodeAnalysis()
        {
            // Create a stable mapping from common syntax kinds to vector indices.
            _syntaxKindToDimension = new Dictionary<SyntaxKind, int>();
            var kinds = Enum.GetValues(typeof(SyntaxKind)).Cast<SyntaxKind>().ToList();
            for (int i = 0; i < kinds.Count; i++)
            {
                // Use a modulo operator to map the large number of kinds to our fixed vector size.
                _syntaxKindToDimension[kinds[i]] = i % VECTOR_DIMENSION;
            }
        }

        /// <summary>
        /// Parses C# code, traverses its Abstract Syntax Tree (AST), and generates a structural vector.
        /// The vector represents the frequency of different syntax kinds in the code.
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
                SyntaxTree tree = CSharpSyntaxTree.ParseText(code);
                var root = tree.GetRoot();

                var vector = new float[VECTOR_DIMENSION];

                // Walk the AST and update vector counts based on syntax kind frequency.
                foreach (var node in root.DescendantNodesAndSelf())
                {
                    SyntaxKind kind = node.Kind();
                    if (_syntaxKindToDimension.TryGetValue(kind, out int index))
                    {
                        vector[index]++;
                    }
                }

                // Normalize the vector (e.g., L2 normalization) to represent structure proportionally.
                var magnitude = Math.Sqrt(vector.Sum(v => v * v));
                if (magnitude > 0)
                {
                    for (int i = 0; i < vector.Length; i++)
                    {
                        vector[i] = (float)(vector[i] / magnitude);
                    }
                }

                return new SqlString(JsonConvert.SerializeObject(vector));
            }
            catch (Exception ex)
            {
                // Return error as JSON for debugging in SQL
                return new SqlString(JsonConvert.SerializeObject(new { error = ex.Message, stackTrace = ex.StackTrace }));
            }
        }
    }
}
