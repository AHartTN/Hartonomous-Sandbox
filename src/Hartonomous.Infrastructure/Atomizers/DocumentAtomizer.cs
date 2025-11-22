using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Hartonomous.Core.Interfaces.Ingestion;
using Hartonomous.Core.Utilities;
using Microsoft.Extensions.Logging;

namespace Hartonomous.Infrastructure.Atomizers;

public class DocumentAtomizer : BaseAtomizer<byte[]>
{
    public DocumentAtomizer(ILogger<DocumentAtomizer> logger) : base(logger) { }

    public override int Priority => 40;

    public override bool CanHandle(string contentType, string? fileExtension)
    {
        if (contentType == "application/pdf" ||
            contentType == "application/vnd.openxmlformats-officedocument.wordprocessingml.document" ||
            contentType == "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" ||
            contentType == "application/vnd.openxmlformats-officedocument.presentationml.presentation" ||
            contentType == "application/msword" ||
            contentType == "application/vnd.ms-excel" ||
            contentType == "application/vnd.ms-powerpoint" ||
            contentType == "application/vnd.oasis.opendocument.text" ||
            contentType == "application/vnd.oasis.opendocument.spreadsheet" ||
            contentType == "application/vnd.oasis.opendocument.presentation" ||
            contentType == "application/rtf" ||
            contentType == "text/rtf")
            return true;

        var docExtensions = new[] { "pdf", "docx", "doc", "xlsx", "xls", "pptx", "ppt", "odt", "ods", "odp", "rtf" };
        return fileExtension != null && docExtensions.Contains(fileExtension.ToLowerInvariant());
    }

    protected override async Task AtomizeCoreAsync(
        byte[] input,
        SourceMetadata source,
        List<AtomData> atoms,
        List<AtomComposition> compositions,
        List<string> warnings,
        CancellationToken cancellationToken)
    {
        var docType = GetDocumentType(source.ContentType, source.FileName);
        var docHash = CreateFileMetadataAtom(input, source, atoms);

        if (docType == "pdf")
        {
            await ExtractPdfContentAsync(input, docHash, atoms, compositions, warnings, cancellationToken);
        }
        else if (docType == "docx" || docType == "xlsx" || docType == "pptx")
        {
            await ExtractOfficeXmlContentAsync(input, docType, docHash, atoms, compositions, warnings, cancellationToken);
        }
        else if (docType == "rtf")
        {
            ExtractRtfContent(input, docHash, atoms, compositions, warnings);
        }
        else
        {
            warnings.Add($"Advanced extraction not implemented for {docType}, using basic binary analysis");
            ExtractBasicBinaryMetadata(input, docHash, atoms, compositions);
        }
    }

    protected override string GetDetectedFormat()
    {
        return "document";
    }

    protected override string GetModality() => "document";

    protected override byte[] GetFileMetadataBytes(byte[] input, SourceMetadata source)
    {
        var docType = GetDocumentType(source.ContentType, source.FileName);
        return Encoding.UTF8.GetBytes($"document:{docType}:{source.FileName}:{input.Length}");
    }

    protected override string GetCanonicalFileText(byte[] input, SourceMetadata source)
    {
        return $"{source.FileName ?? "document"} ({input.Length:N0} bytes)";
    }

    protected override string GetFileMetadataJson(byte[] input, SourceMetadata source)
    {
        var docType = GetDocumentType(source.ContentType, source.FileName);
        var metadata = new StringBuilder();
        metadata.Append("{");
        metadata.Append($"\"type\":\"{docType}\",");
        metadata.Append($"\"size\":{input.Length},");
        metadata.Append($"\"fileName\":\"{source.FileName ?? "unknown"}\"");
        
        if (docType == "pdf" && input.Length > 8 && Encoding.ASCII.GetString(input, 0, 5) == "%PDF-")
        {
            var versionEnd = Array.IndexOf(input, (byte)'\n', 5);
            if (versionEnd > 0 && versionEnd < 20)
            {
                var version = Encoding.ASCII.GetString(input, 5, versionEnd - 5).Trim();
                metadata.Append($",\"pdfVersion\":\"{version}\"");
            }
        }
        else if ((docType == "docx" || docType == "xlsx" || docType == "pptx") && input.Length > 4 && input[0] == 0x50 && input[1] == 0x4B)
        {
            metadata.Append(",\"format\":\"OpenXML\",\"container\":\"ZIP\"");
        }
        
        metadata.Append("}");
        return metadata.ToString();
    }

    private string GetDocumentType(string? contentType, string? fileName)
    {
        if (contentType?.Contains("pdf") == true || fileName?.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase) == true)
            return "pdf";
        if (contentType?.Contains("wordprocessingml") == true || fileName?.EndsWith(".docx", StringComparison.OrdinalIgnoreCase) == true)
            return "docx";
        if (contentType?.Contains("spreadsheetml") == true || fileName?.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase) == true)
            return "xlsx";
        if (contentType?.Contains("presentationml") == true || fileName?.EndsWith(".pptx", StringComparison.OrdinalIgnoreCase) == true)
            return "pptx";
        if (contentType?.Contains("msword") == true || fileName?.EndsWith(".doc", StringComparison.OrdinalIgnoreCase) == true)
            return "doc";
        if (fileName?.EndsWith(".rtf", StringComparison.OrdinalIgnoreCase) == true)
            return "rtf";
        
        return "unknown";
    }

    private async Task ExtractPdfContentAsync(
        byte[] input,
        byte[] docHash,
        List<AtomData> atoms,
        List<AtomComposition> compositions,
        List<string> warnings,
        CancellationToken cancellationToken)
    {
        var pdfText = Encoding.ASCII.GetString(input);
        
        var pageCount = CountOccurrences(pdfText, "/Type /Page");
        if (pageCount == 0)
            pageCount = CountOccurrences(pdfText, "/Type/Page");
        
        for (int pageNum = 0; pageNum < pageCount; pageNum++)
        {
            var pageMetadataBytes = Encoding.UTF8.GetBytes($"page:{pageNum + 1}");
            var pageHash = CreateContentAtom(
                pageMetadataBytes,
                "document",
                "pdf-page",
                $"Page {pageNum + 1}",
                $"{{\"pageNumber\":{pageNum + 1},\"pageCount\":{pageCount}}}",
                atoms);
            
            CreateAtomComposition(docHash, pageHash, pageNum, compositions, y: pageNum);
        }
        
        ExtractPdfTextStreams(pdfText, docHash, atoms, compositions, warnings);
        warnings.Add("PDF text extraction requires specialized library (e.g., iTextSharp, PdfPig) for production use");
        
        await Task.CompletedTask;
    }

    private void ExtractPdfTextStreams(
        string pdfContent,
        byte[] docHash,
        List<AtomData> atoms,
        List<AtomComposition> compositions,
        List<string> warnings)
    {
        int streamIndex = 0;
        int pos = 0;
        
        while ((pos = pdfContent.IndexOf("BT", pos, StringComparison.Ordinal)) != -1)
        {
            var endPos = pdfContent.IndexOf("ET", pos, StringComparison.Ordinal);
            if (endPos == -1) break;
            
            var textBlock = pdfContent.Substring(pos, endPos - pos + 2);
            var textContent = ExtractPdfTextOperators(textBlock);
            
            if (!string.IsNullOrWhiteSpace(textContent))
            {
                var textBytes = Encoding.UTF8.GetBytes(textContent);
                var canonicalText = textContent.Length <= 100 ? textContent : textContent[..100] + "...";
                var textHash = CreateContentAtom(
                    textBytes.Length <= MaxAtomSize ? textBytes : textBytes.Take(MaxAtomSize).ToArray(),
                    "text",
                    "pdf-text-stream",
                    canonicalText,
                    $"{{\"streamIndex\":{streamIndex},\"length\":{textContent.Length}}}",
                    atoms);
                
                CreateAtomComposition(docHash, textHash, streamIndex++, compositions);
            }
            
            pos = endPos + 2;
        }
    }

    private string ExtractPdfTextOperators(string textBlock)
    {
        var result = new StringBuilder();
        int pos = 0;
        while ((pos = textBlock.IndexOf('(', pos)) != -1)
        {
            var endPos = textBlock.IndexOf(')', pos);
            if (endPos == -1) break;
            
            var text = textBlock.Substring(pos + 1, endPos - pos - 1);
            result.Append(text).Append(' ');
            pos = endPos + 1;
        }
        
        return result.ToString().Trim();
    }

    private async Task ExtractOfficeXmlContentAsync(
        byte[] input,
        string docType,
        byte[] docHash,
        List<AtomData> atoms,
        List<AtomComposition> compositions,
        List<string> warnings,
        CancellationToken cancellationToken)
    {
        try
        {
            using var ms = new System.IO.MemoryStream(input);
            using var archive = new System.IO.Compression.ZipArchive(ms, System.IO.Compression.ZipArchiveMode.Read);
            
            int entryIndex = 0;
            foreach (var entry in archive.Entries)
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                if (ShouldExtractOfficeEntry(entry.FullName, docType))
                {
                    using var entryStream = entry.Open();
                    using var reader = new System.IO.StreamReader(entryStream);
                    var xmlContent = await reader.ReadToEndAsync(cancellationToken);
                    
                    var entryBytes = Encoding.UTF8.GetBytes(xmlContent);
                    var entryHash = CreateContentAtom(
                        entryBytes.Length <= MaxAtomSize ? entryBytes : entryBytes.Take(MaxAtomSize).ToArray(),
                        "document",
                        $"{docType}-part",
                        entry.FullName,
                        $"{{\"entryName\":\"{entry.FullName}\",\"size\":{entry.Length}}}",
                        atoms);
                    
                    CreateAtomComposition(docHash, entryHash, entryIndex++, compositions);
                }
            }
        }
        catch (Exception ex)
        {
            warnings.Add($"Office XML extraction failed: {ex.Message}");
        }
        
        warnings.Add($"{docType.ToUpper()} content extraction requires specialized library (e.g., Open XML SDK) for production use");
    }

    private bool ShouldExtractOfficeEntry(string entryName, string docType)
    {
        if (entryName.StartsWith("_rels/") || entryName.StartsWith("[Content_Types]"))
            return false;
        
        return docType switch
        {
            "docx" => entryName.Contains("word/document.xml") || entryName.Contains("word/styles.xml"),
            "xlsx" => entryName.Contains("xl/worksheets/") || entryName.Contains("xl/sharedStrings.xml"),
            "pptx" => entryName.Contains("ppt/slides/") || entryName.Contains("ppt/slideLayouts/"),
            _ => entryName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase)
        };
    }

    private void ExtractRtfContent(
        byte[] input,
        byte[] docHash,
        List<AtomData> atoms,
        List<AtomComposition> compositions,
        List<string> warnings)
    {
        var rtfText = Encoding.UTF8.GetString(input);
        var plainText = StripRtfControlWords(rtfText);
        
        if (!string.IsNullOrWhiteSpace(plainText))
        {
            var textBytes = Encoding.UTF8.GetBytes(plainText);
            var canonicalText = plainText.Length <= 200 ? plainText : plainText[..200] + "...";
            var textHash = CreateContentAtom(
                textBytes.Length <= MaxAtomSize ? textBytes : textBytes.Take(MaxAtomSize).ToArray(),
                "text",
                "rtf-content",
                canonicalText,
                $"{{\"extractedLength\":{plainText.Length}}}",
                atoms);
            
            CreateAtomComposition(docHash, textHash, 0, compositions);
        }
        
        warnings.Add("RTF parsing is basic; production use should employ specialized RTF library");
    }

    private string StripRtfControlWords(string rtfContent)
    {
        var result = new StringBuilder();
        bool inControlWord = false;
        int braceDepth = 0;
        
        for (int i = 0; i < rtfContent.Length; i++)
        {
            char c = rtfContent[i];
            
            if (c == '{')
            {
                braceDepth++;
            }
            else if (c == '}')
            {
                braceDepth--;
            }
            else if (c == '\\' && i + 1 < rtfContent.Length)
            {
                inControlWord = true;
                i++;
                while (i < rtfContent.Length && (char.IsLetterOrDigit(rtfContent[i]) || rtfContent[i] == '-'))
                    i++;
                i--;
                inControlWord = false;
            }
            else if (!inControlWord && braceDepth <= 1)
            {
                if (!char.IsControl(c) || char.IsWhiteSpace(c))
                    result.Append(c);
            }
        }
        
        return result.ToString().Trim();
    }

    private void ExtractBasicBinaryMetadata(
        byte[] input,
        byte[] docHash,
        List<AtomData> atoms,
        List<AtomComposition> compositions)
    {
        var entropy = CalculateEntropy(input.Take(Math.Min(4096, input.Length)).ToArray());
        
        var metadataBytes = Encoding.UTF8.GetBytes($"binary:entropy:{entropy:F2}");
        var metaHash = CreateContentAtom(
            metadataBytes,
            "binary",
            "entropy-analysis",
            $"Entropy: {entropy:F2}",
            $"{{\"entropy\":{entropy:F2},\"sampleSize\":{Math.Min(4096, input.Length)}}}",
            atoms);
        
        CreateAtomComposition(docHash, metaHash, 0, compositions);
    }

    private double CalculateEntropy(byte[] data)
    {
        if (data.Length == 0) return 0;
        
        var freq = new int[256];
        foreach (var b in data)
            freq[b]++;
        
        double entropy = 0;
        foreach (var count in freq)
        {
            if (count == 0) continue;
            double p = (double)count / data.Length;
            entropy -= p * Math.Log2(p);
        }
        
        return entropy;
    }

    private int CountOccurrences(string text, string pattern)
    {
        int count = 0;
        int pos = 0;
        while ((pos = text.IndexOf(pattern, pos, StringComparison.Ordinal)) != -1)
        {
            count++;
            pos += pattern.Length;
        }
        return count;
    }
}
