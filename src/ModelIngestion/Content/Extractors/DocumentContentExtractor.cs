using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using DocumentFormat.OpenXml.Spreadsheet;
using Hartonomous.Core.Interfaces;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace ModelIngestion.Content.Extractors;

/// <summary>
/// Extracts structured content from document files (PDF, DOCX, XLSX).
/// Preserves document structure (headings, paragraphs, tables, lists) as atom hierarchies.
/// </summary>
public sealed class DocumentContentExtractor : IContentExtractor
{
    public bool CanHandle(ContentExtractionContext context)
    {
        if (context.SourceType != ContentSourceType.Stream || context.ContentStream == null)
        {
            return false;
        }

        var fileName = context.FileName ?? "";
        var extension = Path.GetExtension(fileName).ToLowerInvariant();

        return extension is ".pdf" or ".docx" or ".xlsx";
    }

    public async Task<ContentExtractionResult> ExtractAsync(ContentExtractionContext context, CancellationToken cancellationToken)
    {
        var fileName = context.FileName ?? "unknown";
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        var sourceUri = context.Metadata.TryGetValue("sourceUri", out var uri) ? uri : $"file:///{fileName}";

        var requests = new List<AtomIngestionRequest>();
        var diagnostics = new Dictionary<string, string>();

        diagnostics["file_name"] = fileName;
        diagnostics["file_type"] = extension;
        diagnostics["source_uri"] = sourceUri;

        try
        {
            var stream = context.EnsureSeekableClone();
            
            switch (extension)
            {
                case ".pdf":
                    await ExtractPdfAsync(stream, requests, sourceUri, diagnostics, cancellationToken);
                    break;
                case ".docx":
                    await ExtractDocxAsync(stream, requests, sourceUri, diagnostics, cancellationToken);
                    break;
                case ".xlsx":
                    await ExtractXlsxAsync(stream, requests, sourceUri, diagnostics, cancellationToken);
                    break;
            }

            diagnostics["extraction_status"] = "success";
            diagnostics["atoms_created"] = requests.Count.ToString();
        }
        catch (Exception ex)
        {
            diagnostics["extraction_status"] = "failed";
            diagnostics["error"] = ex.Message;
        }

        return new ContentExtractionResult(requests, diagnostics);
    }

    private async Task ExtractPdfAsync(
        Stream stream, 
        List<AtomIngestionRequest> requests, 
        string sourceUri, 
        Dictionary<string, string> diagnostics,
        CancellationToken cancellationToken)
    {
        using var document = PdfDocument.Open(stream);
        
        diagnostics["pdf_pages"] = document.NumberOfPages.ToString();
        diagnostics["pdf_version"] = document.Version.ToString();

        int pageNumber = 0;
        foreach (var page in document.GetPages())
        {
            pageNumber++;
            cancellationToken.ThrowIfCancellationRequested();

            var text = page.Text;
            if (string.IsNullOrWhiteSpace(text))
            {
                continue;
            }

            // Extract page content as atom
            var pageMetadata = new MetadataEnvelope()
                .Set("page_number", pageNumber)
                .Set("width", page.Width)
                .Set("height", page.Height)
                .Set("rotation", page.Rotation.ToString());

            var pageAtom = new AtomIngestionRequestBuilder()
                .WithCanonicalText(text)
                .WithModality("text", "pdf_page")
                .WithSource("document_parser", sourceUri)
                .WithMetadata(pageMetadata)
                .Build();

            requests.Add(pageAtom);

            // Extract words with positions (for spatial indexing)
            foreach (var word in page.GetWords())
            {
                var wordMetadata = new MetadataEnvelope()
                    .Set("page_number", pageNumber)
                    .Set("x", word.BoundingBox.Left)
                    .Set("y", word.BoundingBox.Bottom)
                    .Set("width", word.BoundingBox.Width)
                    .Set("height", word.BoundingBox.Height);

                var wordAtom = new AtomIngestionRequestBuilder()
                    .WithCanonicalText(word.Text)
                    .WithModality("text", "pdf_word")
                    .WithSource("document_parser", sourceUri)
                    .WithMetadata(wordMetadata)
                    .Build();

                requests.Add(wordAtom);
            }
        }

        diagnostics["pdf_words_extracted"] = requests.Count(r => r.Modality == "pdf_word").ToString();

        await Task.CompletedTask; // Satisfy async signature
    }

    private async Task ExtractDocxAsync(
        Stream stream, 
        List<AtomIngestionRequest> requests, 
        string sourceUri, 
        Dictionary<string, string> diagnostics,
        CancellationToken cancellationToken)
    {
        using var document = WordprocessingDocument.Open(stream, false);
        var body = document.MainDocumentPart?.Document?.Body;

        if (body == null)
        {
            diagnostics["error"] = "Document body is empty";
            return;
        }

        int paragraphCount = 0;
        int headingCount = 0;
        int tableCount = 0;

        foreach (var element in body.Elements())
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (element is Paragraph paragraph)
            {
                var text = paragraph.InnerText;
                if (string.IsNullOrWhiteSpace(text))
                {
                    continue;
                }

                // Detect heading style
                var styleId = paragraph.ParagraphProperties?.ParagraphStyleId?.Val?.Value ?? "";
                var isHeading = styleId.StartsWith("Heading", StringComparison.OrdinalIgnoreCase);

                if (isHeading)
                {
                    headingCount++;
                    var level = int.TryParse(styleId.Replace("Heading", ""), out var l) ? l : 1;

                    var headingMetadata = new MetadataEnvelope()
                        .Set("heading_level", level)
                        .Set("style", styleId);

                    var headingAtom = new AtomIngestionRequestBuilder()
                        .WithCanonicalText(text)
                        .WithModality("text", "docx_heading")
                        .WithSource("document_parser", sourceUri)
                        .WithMetadata(headingMetadata)
                        .Build();

                    requests.Add(headingAtom);
                }
                else
                {
                    paragraphCount++;

                    var paragraphMetadata = new MetadataEnvelope()
                        .Set("paragraph_index", paragraphCount)
                        .Set("style", styleId);

                    var paragraphAtom = new AtomIngestionRequestBuilder()
                        .WithCanonicalText(text)
                        .WithModality("text", "docx_paragraph")
                        .WithSource("document_parser", sourceUri)
                        .WithMetadata(paragraphMetadata)
                        .Build();

                    requests.Add(paragraphAtom);
                }
            }
            else if (element is DocumentFormat.OpenXml.Wordprocessing.Table table)
            {
                tableCount++;
                var tableText = ExtractTableText(table);

                var tableMetadata = new MetadataEnvelope()
                    .Set("table_index", tableCount)
                    .Set("row_count", table.Elements<TableRow>().Count());

                var tableAtom = new AtomIngestionRequestBuilder()
                    .WithCanonicalText(tableText)
                    .WithModality("structured_data", "docx_table")
                    .WithSource("document_parser", sourceUri)
                    .WithMetadata(tableMetadata)
                    .Build();

                requests.Add(tableAtom);
            }
        }

        diagnostics["docx_paragraphs"] = paragraphCount.ToString();
        diagnostics["docx_headings"] = headingCount.ToString();
        diagnostics["docx_tables"] = tableCount.ToString();

        await Task.CompletedTask;
    }

    private async Task ExtractXlsxAsync(
        Stream stream, 
        List<AtomIngestionRequest> requests, 
        string sourceUri, 
        Dictionary<string, string> diagnostics,
        CancellationToken cancellationToken)
    {
        using var document = SpreadsheetDocument.Open(stream, false);
        var workbookPart = document.WorkbookPart;

        if (workbookPart == null)
        {
            diagnostics["error"] = "Workbook is empty";
            return;
        }

        var sheets = workbookPart.Workbook.Sheets?.Elements<Sheet>() ?? Enumerable.Empty<Sheet>();
        int sheetCount = 0;
        int totalRows = 0;

        foreach (var sheet in sheets)
        {
            sheetCount++;
            cancellationToken.ThrowIfCancellationRequested();

            var worksheetPart = (WorksheetPart)workbookPart.GetPartById(sheet.Id!);
            var sheetData = worksheetPart.Worksheet.Elements<SheetData>().FirstOrDefault();

            if (sheetData == null)
            {
                continue;
            }

            var sheetName = sheet.Name?.Value ?? $"Sheet{sheetCount}";
            var rows = sheetData.Elements<Row>().ToList();
            totalRows += rows.Count;

            foreach (var row in rows)
            {
                var cellValues = row.Elements<Cell>()
                    .Select(cell => GetCellValue(cell, workbookPart))
                    .Where(v => !string.IsNullOrWhiteSpace(v))
                    .ToList();

                if (cellValues.Count == 0)
                {
                    continue;
                }

                var rowText = string.Join(" | ", cellValues);

                var rowMetadata = new MetadataEnvelope()
                    .Set("sheet_name", sheetName)
                    .Set("row_index", row.RowIndex?.Value ?? 0)
                    .Set("cell_count", cellValues.Count);

                var rowAtom = new AtomIngestionRequestBuilder()
                    .WithCanonicalText(rowText)
                    .WithModality("structured_data", "xlsx_row")
                    .WithSource("document_parser", sourceUri)
                    .WithMetadata(rowMetadata)
                    .Build();

                requests.Add(rowAtom);
            }
        }

        diagnostics["xlsx_sheets"] = sheetCount.ToString();
        diagnostics["xlsx_total_rows"] = totalRows.ToString();

        await Task.CompletedTask;
    }

    private string ExtractTableText(DocumentFormat.OpenXml.Wordprocessing.Table table)
    {
        var rows = new List<string>();

        foreach (var row in table.Elements<TableRow>())
        {
            var cells = row.Elements<TableCell>()
                .Select(cell => cell.InnerText.Trim())
                .Where(text => !string.IsNullOrWhiteSpace(text));

            rows.Add(string.Join(" | ", cells));
        }

        return string.Join("\n", rows);
    }

    private string GetCellValue(Cell cell, WorkbookPart workbookPart)
    {
        if (cell.CellValue == null)
        {
            return string.Empty;
        }

        var value = cell.CellValue.Text;

        // If it's a shared string, look it up
        if (cell.DataType?.Value == CellValues.SharedString)
        {
            var stringTable = workbookPart.SharedStringTablePart?.SharedStringTable;
            if (stringTable != null && int.TryParse(value, out var index))
            {
                return stringTable.Elements<SharedStringItem>().ElementAtOrDefault(index)?.InnerText ?? value;
            }
        }

        return value;
    }
}
