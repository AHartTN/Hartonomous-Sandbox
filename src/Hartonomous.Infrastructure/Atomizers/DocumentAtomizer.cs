using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Hartonomous.Core.Interfaces.Ingestion;

namespace Hartonomous.Infrastructure.Atomizers;

/// <summary>
/// Atomizes document files (PDF, DOCX, XLSX, PPTX) by extracting text, metadata, and structure.
/// Handles structured documents with pages, sections, paragraphs, and embedded content.
/// </summary>
public class DocumentAtomizer : IAtomizer<byte[]>
{
    private const int MaxAtomSize = 64;
    public int Priority => 40;

    public bool CanHandle(string contentType, string? fileExtension)
    {
        // PDF documents
        if (contentType == "application/pdf")
            return true;

        // Microsoft Office documents
        if (contentType == "application/vnd.openxmlformats-officedocument.wordprocessingml.document" || // DOCX
            contentType == "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" || // XLSX
            contentType == "application/vnd.openxmlformats-officedocument.presentationml.presentation" || // PPTX
            contentType == "application/msword" || // DOC
            contentType == "application/vnd.ms-excel" || // XLS
            contentType == "application/vnd.ms-powerpoint") // PPT
            return true;

        // OpenDocument formats
        if (contentType == "application/vnd.oasis.opendocument.text" || // ODT
            contentType == "application/vnd.oasis.opendocument.spreadsheet" || // ODS
            contentType == "application/vnd.oasis.opendocument.presentation") // ODP
            return true;

        // RTF
        if (contentType == "application/rtf" || contentType == "text/rtf")
            return true;

        var docExtensions = new[] { "pdf", "docx", "doc", "xlsx", "xls", "pptx", "ppt", "odt", "ods", "odp", "rtf" };
        return fileExtension != null && docExtensions.Contains(fileExtension.ToLowerInvariant());
    }

    public async Task<AtomizationResult> AtomizeAsync(byte[] input, SourceMetadata source, CancellationToken cancellationToken)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var atoms = new List<AtomData>();
        var compositions = new List<AtomComposition>();
        var warnings = new List<string>();

        try
        {
            // Create parent atom for the document file
            var docHash = SHA256.HashData(input);
            var docType = GetDocumentType(source.ContentType, source.FileName);
            var docMetadataBytes = Encoding.UTF8.GetBytes($"document:{docType}:{source.FileName}:{input.Length}");
            
            var docAtom = new AtomData
            {
                AtomicValue = docMetadataBytes.Length <= MaxAtomSize ? docMetadataBytes : docMetadataBytes.Take(MaxAtomSize).ToArray(),
                ContentHash = docHash,
                Modality = "document",
                Subtype = $"{docType}-metadata",
                ContentType = source.ContentType,
                CanonicalText = $"{source.FileName ?? "document"} ({input.Length:N0} bytes)",
                Metadata = BuildDocumentMetadata(input, docType, source.FileName)
            };
            atoms.Add(docAtom);

            // Extract document structure and content based on type
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
                // Fallback: treat as binary with basic metadata
                warnings.Add($"Advanced extraction not implemented for {docType}, using basic binary analysis");
                ExtractBasicBinaryMetadata(input, docHash, atoms, compositions);
            }

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
                    AtomizerType = nameof(DocumentAtomizer),
                    DetectedFormat = docType.ToUpperInvariant(),
                    Warnings = warnings.Count > 0 ? warnings : null
                }
            };
        }
        catch (Exception ex)
        {
            warnings.Add($"Document atomization failed: {ex.Message}");
            throw;
        }
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

    private string BuildDocumentMetadata(byte[] input, string docType, string? fileName)
    {
        var metadata = new StringBuilder();
        metadata.Append("{");
        metadata.Append($"\"type\":\"{docType}\",");
        metadata.Append($"\"size\":{input.Length},");
        metadata.Append($"\"fileName\":\"{fileName ?? "unknown"}\"");
        
        // Add format-specific metadata hints
        if (docType == "pdf")
        {
            // Check for PDF header
            if (input.Length > 8 && Encoding.ASCII.GetString(input, 0, 5) == "%PDF-")
            {
                var versionEnd = Array.IndexOf(input, (byte)'\n', 5);
                if (versionEnd > 0 && versionEnd < 20)
                {
                    var version = Encoding.ASCII.GetString(input, 5, versionEnd - 5).Trim();
                    metadata.Append($",\"pdfVersion\":\"{version}\"");
                }
            }
        }
        else if (docType == "docx" || docType == "xlsx" || docType == "pptx")
        {
            // Office Open XML is ZIP-based
            if (input.Length > 4 && input[0] == 0x50 && input[1] == 0x4B)
            {
                metadata.Append(",\"format\":\"OpenXML\",\"container\":\"ZIP\"");
            }
        }
        
        metadata.Append("}");
        return metadata.ToString();
    }

    private async Task ExtractPdfContentAsync(
        byte[] input,
        byte[] docHash,
        List<AtomData> atoms,
        List<AtomComposition> compositions,
        List<string> warnings,
        CancellationToken cancellationToken)
    {
        // PDF structure analysis
        // Look for common PDF objects: pages, text streams, images, fonts
        var pdfText = Encoding.ASCII.GetString(input);
        
        // Count pages (basic heuristic: count /Type /Page occurrences)
        var pageCount = CountOccurrences(pdfText, "/Type /Page");
        if (pageCount == 0)
            pageCount = CountOccurrences(pdfText, "/Type/Page");
        
        // Create page metadata atoms
        for (int pageNum = 0; pageNum < pageCount; pageNum++)
        {
            var pageMetadataBytes = Encoding.UTF8.GetBytes($"page:{pageNum + 1}");
            var pageHash = SHA256.HashData(pageMetadataBytes);
            
            var pageAtom = new AtomData
            {
                AtomicValue = pageMetadataBytes,
                ContentHash = pageHash,
                Modality = "document",
                Subtype = "pdf-page",
                ContentType = "application/pdf",
                CanonicalText = $"Page {pageNum + 1}",
                Metadata = $"{{\"pageNumber\":{pageNum + 1},\"pageCount\":{pageCount}}}"
            };
            atoms.Add(pageAtom);
            
            compositions.Add(new AtomComposition
            {
                ParentAtomHash = docHash,
                ComponentAtomHash = pageHash,
                SequenceIndex = pageNum,
                Position = new SpatialPosition { X = 0, Y = pageNum, Z = 0 }
            });
        }
        
        // Extract text streams (look for BT...ET blocks)
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
        // Basic text extraction: find BT (Begin Text) ... ET (End Text) blocks
        int streamIndex = 0;
        int pos = 0;
        
        while ((pos = pdfContent.IndexOf("BT", pos, StringComparison.Ordinal)) != -1)
        {
            var endPos = pdfContent.IndexOf("ET", pos, StringComparison.Ordinal);
            if (endPos == -1) break;
            
            var textBlock = pdfContent.Substring(pos, endPos - pos + 2);
            
            // Extract Tj and TJ operators (text showing)
            var textContent = ExtractPdfTextOperators(textBlock);
            
            if (!string.IsNullOrWhiteSpace(textContent))
            {
                var textBytes = Encoding.UTF8.GetBytes(textContent);
                var textHash = SHA256.HashData(textBytes);
                
                var textAtom = new AtomData
                {
                    AtomicValue = textBytes.Length <= MaxAtomSize ? textBytes : textBytes.Take(MaxAtomSize).ToArray(),
                    ContentHash = textHash,
                    Modality = "text",
                    Subtype = "pdf-text-stream",
                    ContentType = "text/plain",
                    CanonicalText = textContent.Length <= 100 ? textContent : textContent[..100] + "...",
                    Metadata = $"{{\"streamIndex\":{streamIndex},\"length\":{textContent.Length}}}"
                };
                
                if (!atoms.Any(a => a.ContentHash.SequenceEqual(textHash)))
                {
                    atoms.Add(textAtom);
                }
                
                compositions.Add(new AtomComposition
                {
                    ParentAtomHash = docHash,
                    ComponentAtomHash = textHash,
                    SequenceIndex = streamIndex++,
                    Position = new SpatialPosition { X = 0, Y = 0, Z = 0 }
                });
            }
            
            pos = endPos + 2;
        }
    }

    private string ExtractPdfTextOperators(string textBlock)
    {
        var result = new StringBuilder();
        
        // Look for Tj (show text) operators: (text) Tj
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
        // Office Open XML files are ZIP archives containing XML files
        try
        {
            using var ms = new System.IO.MemoryStream(input);
            using var archive = new System.IO.Compression.ZipArchive(ms, System.IO.Compression.ZipArchiveMode.Read);
            
            int entryIndex = 0;
            foreach (var entry in archive.Entries)
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                // Focus on content files
                if (ShouldExtractOfficeEntry(entry.FullName, docType))
                {
                    using var entryStream = entry.Open();
                    using var reader = new System.IO.StreamReader(entryStream);
                    var xmlContent = await reader.ReadToEndAsync(cancellationToken);
                    
                    var entryBytes = Encoding.UTF8.GetBytes(xmlContent);
                    var entryHash = SHA256.HashData(entryBytes);
                    
                    var entryAtom = new AtomData
                    {
                        AtomicValue = entryBytes.Length <= MaxAtomSize ? entryBytes : entryBytes.Take(MaxAtomSize).ToArray(),
                        ContentHash = entryHash,
                        Modality = "document",
                        Subtype = $"{docType}-part",
                        ContentType = "application/xml",
                        CanonicalText = entry.FullName,
                        Metadata = $"{{\"entryName\":\"{entry.FullName}\",\"size\":{entry.Length}}}"
                    };
                    
                    atoms.Add(entryAtom);
                    
                    compositions.Add(new AtomComposition
                    {
                        ParentAtomHash = docHash,
                        ComponentAtomHash = entryHash,
                        SequenceIndex = entryIndex++,
                        Position = new SpatialPosition { X = 0, Y = 0, Z = 0 }
                    });
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
        // Extract main content files, skip metadata and relationships
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
        // RTF is text-based but with control words
        var rtfText = Encoding.UTF8.GetString(input);
        
        // Strip RTF control words for basic text extraction
        var plainText = StripRtfControlWords(rtfText);
        
        if (!string.IsNullOrWhiteSpace(plainText))
        {
            var textBytes = Encoding.UTF8.GetBytes(plainText);
            var textHash = SHA256.HashData(textBytes);
            
            var textAtom = new AtomData
            {
                AtomicValue = textBytes.Length <= MaxAtomSize ? textBytes : textBytes.Take(MaxAtomSize).ToArray(),
                ContentHash = textHash,
                Modality = "text",
                Subtype = "rtf-content",
                ContentType = "text/plain",
                CanonicalText = plainText.Length <= 200 ? plainText : plainText[..200] + "...",
                Metadata = $"{{\"extractedLength\":{plainText.Length}}}"
            };
            
            atoms.Add(textAtom);
            
            compositions.Add(new AtomComposition
            {
                ParentAtomHash = docHash,
                ComponentAtomHash = textHash,
                SequenceIndex = 0,
                Position = new SpatialPosition { X = 0, Y = 0, Z = 0 }
            });
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
                // Skip control word
                i++;
                while (i < rtfContent.Length && (char.IsLetterOrDigit(rtfContent[i]) || rtfContent[i] == '-'))
                    i++;
                i--;
                inControlWord = false;
            }
            else if (!inControlWord && braceDepth <= 1)
            {
                // Only include text content
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
        // Analyze file structure: magic bytes, sections, entropy
        var entropy = CalculateEntropy(input.Take(Math.Min(4096, input.Length)).ToArray());
        
        var metadataBytes = Encoding.UTF8.GetBytes($"binary:entropy:{entropy:F2}");
        var metaHash = SHA256.HashData(metadataBytes);
        
        var metaAtom = new AtomData
        {
            AtomicValue = metadataBytes,
            ContentHash = metaHash,
            Modality = "binary",
            Subtype = "entropy-analysis",
            ContentType = "application/octet-stream",
            CanonicalText = $"Entropy: {entropy:F2}",
            Metadata = $"{{\"entropy\":{entropy:F2},\"sampleSize\":{Math.Min(4096, input.Length)}}}"
        };
        
        atoms.Add(metaAtom);
        
        compositions.Add(new AtomComposition
        {
            ParentAtomHash = docHash,
            ComponentAtomHash = metaHash,
            SequenceIndex = 0,
            Position = new SpatialPosition { X = 0, Y = 0, Z = 0 }
        });
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
