using System.Security.Cryptography;
using System.Text;
using Hartonomous.Core.Interfaces.Ingestion;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Hartonomous.Infrastructure.Atomizers;

/// <summary>
/// C# code atomizer using Roslyn for semantic AST parsing.
/// Extracts classes, methods, properties, and relationships with full semantic understanding.
/// </summary>
public class RoslynAtomizer : IAtomizer<byte[]>
{
    private const int MaxAtomSize = 64;
    public int Priority => 25; // Higher priority than regex-based CodeFileAtomizer

    public bool CanHandle(string contentType, string? fileExtension)
    {
        if (contentType?.Contains("x-csharp") == true || contentType?.Contains("csharp") == true)
            return true;

        if (string.IsNullOrEmpty(fileExtension))
            return false;

        var ext = fileExtension.ToLowerInvariant();
        return ext is "cs" or "csx";
    }

    public async Task<AtomizationResult> AtomizeAsync(byte[] input, SourceMetadata source, CancellationToken cancellationToken)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var atoms = new List<AtomData>();
        var compositions = new List<AtomComposition>();
        var warnings = new List<string>();

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

            var fileHash = SHA256.HashData(input);
            var tree = CSharpSyntaxTree.ParseText(code, cancellationToken: cancellationToken);
            var root = await tree.GetRootAsync(cancellationToken);

            // Create file-level atom
            var fileAtom = CreateFileAtom(source, input, fileHash, code);
            atoms.Add(fileAtom);

            // Extract semantic elements
            var visitor = new CSharpSemanticVisitor(atoms, compositions, fileHash, warnings);
            visitor.Visit(root);

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
                    AtomizerType = nameof(RoslynAtomizer),
                    DetectedFormat = "csharp",
                    Warnings = warnings.Count > 0 ? warnings : null
                }
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            warnings.Add($"Roslyn parsing failed: {ex.Message}");
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
                    AtomizerType = nameof(RoslynAtomizer),
                    DetectedFormat = "csharp",
                    Warnings = warnings
                }
            };
        }
    }

    private static AtomData CreateFileAtom(SourceMetadata source, byte[] input, byte[] fileHash, string code)
    {
        var fileMetadataBytes = Encoding.UTF8.GetBytes($"csharp:{source.FileName}:{input.Length}");
        return new AtomData
        {
            AtomicValue = fileMetadataBytes.Length <= MaxAtomSize ? fileMetadataBytes : fileMetadataBytes.Take(MaxAtomSize).ToArray(),
            ContentHash = fileHash,
            Modality = "code",
            Subtype = "csharp-file",
            ContentType = source.ContentType ?? "text/x-csharp",
            CanonicalText = $"{source.FileName ?? "code.cs"} ({input.Length:N0} bytes)",
            Metadata = System.Text.Json.JsonSerializer.Serialize(new
            {
                language = "csharp",
                size = input.Length,
                fileName = source.FileName,
                lines = code.Split('\n').Length,
                parsingEngine = "Roslyn"
            })
        };
    }

    private class CSharpSemanticVisitor : CSharpSyntaxWalker
    {
        private readonly List<AtomData> _atoms;
        private readonly List<AtomComposition> _compositions;
        private readonly byte[] _fileHash;
        private readonly List<string> _warnings;
        private readonly Stack<byte[]> _parentHashes = new();
        private int _sequenceIndex;

        public CSharpSemanticVisitor(
            List<AtomData> atoms,
            List<AtomComposition> compositions,
            byte[] fileHash,
            List<string> warnings)
        {
            _atoms = atoms;
            _compositions = compositions;
            _fileHash = fileHash;
            _warnings = warnings;
            _parentHashes.Push(fileHash);
        }

        public override void VisitNamespaceDeclaration(NamespaceDeclarationSyntax node)
        {
            var atom = CreateAtom(node.Name.ToString(), "namespace", $"namespace {node.Name}", node.GetLocation());
            AddComposition(_parentHashes.Peek(), atom.ContentHash);
            _parentHashes.Push(atom.ContentHash);
            base.VisitNamespaceDeclaration(node);
            _parentHashes.Pop();
        }

        public override void VisitFileScopedNamespaceDeclaration(FileScopedNamespaceDeclarationSyntax node)
        {
            var atom = CreateAtom(node.Name.ToString(), "namespace", $"namespace {node.Name}", node.GetLocation());
            AddComposition(_parentHashes.Peek(), atom.ContentHash);
            _parentHashes.Push(atom.ContentHash);
            base.VisitFileScopedNamespaceDeclaration(node);
            _parentHashes.Pop();
        }

        public override void VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            var modifiers = string.Join(" ", node.Modifiers.Select(m => m.Text));
            var atom = CreateAtom(node.Identifier.Text, "class", $"{modifiers} class {node.Identifier.Text}", node.GetLocation());
            AddComposition(_parentHashes.Peek(), atom.ContentHash);
            _parentHashes.Push(atom.ContentHash);
            base.VisitClassDeclaration(node);
            _parentHashes.Pop();
        }

        public override void VisitInterfaceDeclaration(InterfaceDeclarationSyntax node)
        {
            var atom = CreateAtom(node.Identifier.Text, "interface", $"interface {node.Identifier.Text}", node.GetLocation());
            AddComposition(_parentHashes.Peek(), atom.ContentHash);
            _parentHashes.Push(atom.ContentHash);
            base.VisitInterfaceDeclaration(node);
            _parentHashes.Pop();
        }

        public override void VisitRecordDeclaration(RecordDeclarationSyntax node)
        {
            var atom = CreateAtom(node.Identifier.Text, "record", $"record {node.Identifier.Text}", node.GetLocation());
            AddComposition(_parentHashes.Peek(), atom.ContentHash);
            _parentHashes.Push(atom.ContentHash);
            base.VisitRecordDeclaration(node);
            _parentHashes.Pop();
        }

        public override void VisitStructDeclaration(StructDeclarationSyntax node)
        {
            var atom = CreateAtom(node.Identifier.Text, "struct", $"struct {node.Identifier.Text}", node.GetLocation());
            AddComposition(_parentHashes.Peek(), atom.ContentHash);
            _parentHashes.Push(atom.ContentHash);
            base.VisitStructDeclaration(node);
            _parentHashes.Pop();
        }

        public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            var parameters = node.ParameterList.Parameters.Select(p => $"{p.Type} {p.Identifier}").ToList();
            var atom = CreateAtom(node.Identifier.Text, "method", $"{node.ReturnType} {node.Identifier.Text}({string.Join(", ", parameters)})", node.GetLocation());
            AddComposition(_parentHashes.Peek(), atom.ContentHash);
            base.VisitMethodDeclaration(node);
        }

        public override void VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
        {
            var parameters = node.ParameterList.Parameters.Select(p => $"{p.Type} {p.Identifier}").ToList();
            var atom = CreateAtom(node.Identifier.Text, "constructor", $"{node.Identifier.Text}({string.Join(", ", parameters)})", node.GetLocation());
            AddComposition(_parentHashes.Peek(), atom.ContentHash);
            base.VisitConstructorDeclaration(node);
        }

        public override void VisitPropertyDeclaration(PropertyDeclarationSyntax node)
        {
            var atom = CreateAtom(node.Identifier.Text, "property", $"{node.Type} {node.Identifier.Text}", node.GetLocation());
            AddComposition(_parentHashes.Peek(), atom.ContentHash);
            base.VisitPropertyDeclaration(node);
        }

        public override void VisitFieldDeclaration(FieldDeclarationSyntax node)
        {
            foreach (var variable in node.Declaration.Variables)
            {
                var atom = CreateAtom(variable.Identifier.Text, "field", $"{node.Declaration.Type} {variable.Identifier.Text}", node.GetLocation());
                AddComposition(_parentHashes.Peek(), atom.ContentHash);
            }
            base.VisitFieldDeclaration(node);
        }

        public override void VisitEnumDeclaration(EnumDeclarationSyntax node)
        {
            var atom = CreateAtom(node.Identifier.Text, "enum", $"enum {node.Identifier.Text}", node.GetLocation());
            AddComposition(_parentHashes.Peek(), atom.ContentHash);
            base.VisitEnumDeclaration(node);
        }

        private AtomData CreateAtom(string name, string subtype, string canonicalText, Location location)
        {
            var lineSpan = location.GetLineSpan();
            var contentBytes = Encoding.UTF8.GetBytes($"csharp:{subtype}:{name}");
            var hash = ComputeHash($"{name}:{subtype}:{lineSpan.StartLinePosition.Line}");

            var atom = new AtomData
            {
                AtomicValue = contentBytes.Length <= MaxAtomSize ? contentBytes : contentBytes.Take(MaxAtomSize).ToArray(),
                ContentHash = hash,
                Modality = "code",
                Subtype = $"csharp-{subtype}",
                ContentType = "text/x-csharp",
                CanonicalText = canonicalText,
                Metadata = System.Text.Json.JsonSerializer.Serialize(new
                {
                    name,
                    subtype,
                    startLine = lineSpan.StartLinePosition.Line + 1,
                    endLine = lineSpan.EndLinePosition.Line + 1,
                    parsingEngine = "Roslyn"
                })
            };

            _atoms.Add(atom);
            return atom;
        }

        private void AddComposition(byte[] parentHash, byte[] componentHash)
        {
            _compositions.Add(new AtomComposition
            {
                ParentAtomHash = parentHash,
                ComponentAtomHash = componentHash,
                SequenceIndex = _sequenceIndex++,
                Position = new SpatialPosition { X = 0, Y = 0, Z = 0 }
            });
        }

        private static byte[] ComputeHash(string input)
        {
            return SHA256.HashData(Encoding.UTF8.GetBytes(input));
        }
    }
}
