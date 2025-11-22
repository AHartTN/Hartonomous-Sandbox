using System.Text;
using Hartonomous.Core.Interfaces.Ingestion;
using Hartonomous.Core.Utilities;
using Hartonomous.Infrastructure.Atomizers.Visitors;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.Logging;

namespace Hartonomous.Infrastructure.Atomizers;

/// <summary>
/// C# code atomizer using Roslyn for semantic AST parsing.
/// Extracts classes, methods, properties, and relationships with full semantic understanding.
/// </summary>
public class RoslynAtomizer : BaseAtomizer<byte[]>
{
    public RoslynAtomizer(ILogger<RoslynAtomizer> logger) : base(logger) { }

    public override int Priority => 25;

    public override bool CanHandle(string contentType, string? fileExtension)
    {
        if (contentType?.Contains("x-csharp") == true || contentType?.Contains("csharp") == true)
            return true;

        if (string.IsNullOrEmpty(fileExtension))
            return false;

        var ext = fileExtension.ToLowerInvariant();
        return ext is "cs" or "csx";
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

        var fileHash = CreateFileMetadataAtom(input, source, atoms);
        var tree = CSharpSyntaxTree.ParseText(code, cancellationToken: cancellationToken);
        var root = await tree.GetRootAsync(cancellationToken);

        var visitor = new CSharpSemanticVisitor(atoms, compositions, fileHash, warnings);
        visitor.Visit(root);
    }

    protected override string GetDetectedFormat() => "csharp (Roslyn AST)";

    protected override string GetModality() => "code";

    protected override byte[] GetFileMetadataBytes(byte[] input, SourceMetadata source)
    {
        return Encoding.UTF8.GetBytes($"csharp:{source.FileName}:{input.Length}");
    }

    protected override string GetCanonicalFileText(byte[] input, SourceMetadata source)
    {
        return $"{source.FileName ?? "code.cs"} ({input.Length:N0} bytes)";
    }

    protected override string GetFileMetadataJson(byte[] input, SourceMetadata source)
    {
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
            language = "csharp",
            size = input.Length,
            fileName = source.FileName,
            lines = string.IsNullOrEmpty(code) ? 0 : code.Split('\n').Length,
            parsingEngine = "Roslyn"
        });
    }
}
