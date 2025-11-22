using FluentAssertions;
using Hartonomous.Infrastructure.Atomizers;
using Hartonomous.UnitTests.Infrastructure;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace Hartonomous.UnitTests.Tests.Infrastructure.Atomizers;

/// <summary>
/// Comprehensive tests for DocumentAtomizer.
/// Tests PDF, DOCX, XLSX, PPTX, and other document format atomization.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Category", "Atomizer")]
[Trait("Category", "Document")]
public class DocumentAtomizerTests : UnitTestBase
{
    public DocumentAtomizerTests(ITestOutputHelper output) : base(output) { }

    #region CanHandle Tests

    [Theory]
    [InlineData("application/pdf", ".pdf", true)]
    [InlineData("application/vnd.openxmlformats-officedocument.wordprocessingml.document", ".docx", true)]
    [InlineData("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", ".xlsx", true)]
    [InlineData("application/vnd.openxmlformats-officedocument.presentationml.presentation", ".pptx", true)]
    [InlineData("application/vnd.oasis.opendocument.text", ".odt", true)]
    [InlineData("application/rtf", ".rtf", true)]
    [InlineData("text/plain", ".txt", false)]
    [InlineData("image/png", ".png", false)]
    public void CanHandle_VariousContentTypes_ReturnsExpected(string contentType, string extension, bool expected)
    {
        // Arrange
        var atomizer = new DocumentAtomizer();

        // Act
        var result = atomizer.CanHandle(contentType, extension);

        // Assert
        result.Should().Be(expected);
    }

    #endregion

    #region PDF Tests

    [Fact]
    public async Task AtomizeAsync_ValidPdf_CreatesAtoms()
    {
        // Arrange
        var atomizer = new DocumentAtomizer();
        var pdfData = CreateFileBuilder().WithPdfHeader().BuildContent();
        var metadata = CreateSourceMetadataBuilder()
            .WithFileName("test.pdf")
            .WithContentType("application/pdf")
            .Build();

        // Act
        var result = await atomizer.AtomizeAsync(pdfData, metadata);

        // Assert
        result.Should().NotBeNull();
        result.Atoms.Should().NotBeEmpty();
        result.ProcessingInfo.AtomizerType.Should().Be("DocumentAtomizer");
        result.ProcessingInfo.DetectedFormat.Should().Contain("PDF");
    }

    [Fact]
    public async Task AtomizeAsync_PdfWithPages_ExtractsPageMetadata()
    {
        // Arrange
        var atomizer = new DocumentAtomizer();
        var pdfData = CreatePdfWithPages(3);
        var metadata = CreateSourceMetadataBuilder()
            .WithFileName("document.pdf")
            .WithContentType("application/pdf")
            .Build();

        // Act
        var result = await atomizer.AtomizeAsync(pdfData, metadata);

        // Assert
        result.Atoms.Should().Contain(a => a.Subtype == "pdf-page");
    }

    [Fact]
    public async Task AtomizeAsync_PdfWithText_ExtractsTextStreams()
    {
        // Arrange
        var atomizer = new DocumentAtomizer();
        var pdfData = CreatePdfWithText("This is test text.");
        var metadata = CreateSourceMetadataBuilder().WithFileName("text.pdf").Build();

        // Act
        var result = await atomizer.AtomizeAsync(pdfData, metadata);

        // Assert
        result.Atoms.Should().Contain(a => a.Subtype == "pdf-text-stream");
    }

    #endregion

    #region Office Document Tests

    [Fact]
    public async Task AtomizeAsync_DocxFile_ExtractsStructure()
    {
        // Arrange
        var atomizer = new DocumentAtomizer();
        var docxData = CreateOfficeXmlDocument("docx");
        var metadata = CreateSourceMetadataBuilder()
            .WithFileName("document.docx")
            .WithContentType("application/vnd.openxmlformats-officedocument.wordprocessingml.document")
            .Build();

        // Act
        var result = await atomizer.AtomizeAsync(docxData, metadata);

        // Assert
        result.Atoms.Should().NotBeEmpty();
        result.Atoms.Should().Contain(a => a.Subtype == "docx-metadata");
    }

    [Fact]
    public async Task AtomizeAsync_XlsxFile_ExtractsWorksheets()
    {
        // Arrange
        var atomizer = new DocumentAtomizer();
        var xlsxData = CreateOfficeXmlDocument("xlsx");
        var metadata = CreateSourceMetadataBuilder()
            .WithFileName("spreadsheet.xlsx")
            .WithContentType("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
            .Build();

        // Act
        var result = await atomizer.AtomizeAsync(xlsxData, metadata);

        // Assert
        result.Atoms.Should().NotBeEmpty();
        result.Atoms.Should().Contain(a => a.Subtype == "xlsx-metadata");
    }

    [Fact]
    public async Task AtomizeAsync_PptxFile_ExtractsSlides()
    {
        // Arrange
        var atomizer = new DocumentAtomizer();
        var pptxData = CreateOfficeXmlDocument("pptx");
        var metadata = CreateSourceMetadataBuilder()
            .WithFileName("presentation.pptx")
            .WithContentType("application/vnd.openxmlformats-officedocument.presentationml.presentation")
            .Build();

        // Act
        var result = await atomizer.AtomizeAsync(pptxData, metadata);

        // Assert
        result.Atoms.Should().NotBeEmpty();
        result.Atoms.Should().Contain(a => a.Subtype == "pptx-metadata");
    }

    #endregion

    #region RTF Tests

    [Fact]
    public async Task AtomizeAsync_RtfFile_ExtractsText()
    {
        // Arrange
        var atomizer = new DocumentAtomizer();
        var rtfData = CreateRtfDocument("Plain text content");
        var metadata = CreateSourceMetadataBuilder()
            .WithFileName("document.rtf")
            .WithContentType("application/rtf")
            .Build();

        // Act
        var result = await atomizer.AtomizeAsync(rtfData, metadata);

        // Assert
        result.Atoms.Should().Contain(a => a.Subtype == "rtf-content");
    }

    #endregion

    #region Metadata Tests

    [Fact]
    public async Task AtomizeAsync_AllAtoms_HaveDocumentModality()
    {
        // Arrange
        var atomizer = new DocumentAtomizer();
        var pdfData = CreateFileBuilder().WithPdfHeader().BuildContent();
        var metadata = CreateSourceMetadataBuilder().WithFileName("test.pdf").Build();

        // Act
        var result = await atomizer.AtomizeAsync(pdfData, metadata);

        // Assert
        result.Atoms.Should().OnlyContain(a => a.Modality == "document" || a.Modality == "text");
    }

    [Fact]
    public async Task AtomizeAsync_ExtractsFileMetadata()
    {
        // Arrange
        var atomizer = new DocumentAtomizer();
        var pdfData = CreateFileBuilder().WithPdfHeader().BuildContent();
        var metadata = CreateSourceMetadataBuilder()
            .WithFileName("test.pdf")
            .WithSizeBytes(pdfData.Length)
            .Build();

        // Act
        var result = await atomizer.AtomizeAsync(pdfData, metadata);

        // Assert
        result.Atoms.Should().Contain(a => 
            a.Metadata?.Contains("fileName") == true &&
            a.Metadata?.Contains("size") == true);
    }

    #endregion

    #region Composition Tests

    [Fact]
    public async Task AtomizeAsync_CreatesDocumentHierarchy()
    {
        // Arrange
        var atomizer = new DocumentAtomizer();
        var pdfData = CreatePdfWithPages(2);
        var metadata = CreateSourceMetadataBuilder().WithFileName("multi-page.pdf").Build();

        // Act
        var result = await atomizer.AtomizeAsync(pdfData, metadata);

        // Assert
        result.Compositions.Should().NotBeEmpty();
        result.Compositions.Should().OnlyContain(c => c.ParentAtomHash != null);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task AtomizeAsync_CorruptedPdf_HandlesGracefully()
    {
        // Arrange
        var atomizer = new DocumentAtomizer();
        var corruptedData = System.Text.Encoding.ASCII.GetBytes("%PDF-CORRUPTED");
        var metadata = CreateSourceMetadataBuilder().WithFileName("corrupted.pdf").Build();

        // Act
        var result = await atomizer.AtomizeAsync(corruptedData, metadata);

        // Assert
        result.Should().NotBeNull();
        result.ProcessingInfo.Warnings.Should().NotBeNull();
    }

    [Fact]
    public async Task AtomizeAsync_EmptyDocument_ReturnsBasicMetadata()
    {
        // Arrange
        var atomizer = new DocumentAtomizer();
        var emptyPdf = CreateFileBuilder().WithPdfHeader().BuildContent();
        var metadata = CreateSourceMetadataBuilder().WithFileName("empty.pdf").Build();

        // Act
        var result = await atomizer.AtomizeAsync(emptyPdf, metadata);

        // Assert
        result.Atoms.Should().NotBeEmpty(); // At least metadata atom
    }

    #endregion

    #region Helper Methods

    private byte[] CreatePdfWithPages(int pageCount)
    {
        var pdf = new StringBuilder();
        pdf.AppendLine("%PDF-1.7");
        pdf.AppendLine("1 0 obj << /Type /Catalog /Pages 2 0 R >> endobj");
        pdf.AppendLine($"2 0 obj << /Type /Pages /Count {pageCount} /Kids [");
        
        for (int i = 0; i < pageCount; i++)
        {
            pdf.AppendLine($"3 {i} R");
        }
        
        pdf.AppendLine("] >> endobj");
        
        for (int i = 0; i < pageCount; i++)
        {
            pdf.AppendLine($"{3 + i} 0 obj << /Type /Page /Parent 2 0 R >> endobj");
        }
        
        return System.Text.Encoding.ASCII.GetBytes(pdf.ToString());
    }

    private byte[] CreatePdfWithText(string text)
    {
        var pdf = new StringBuilder();
        pdf.AppendLine("%PDF-1.7");
        pdf.AppendLine("BT");
        pdf.AppendLine($"({text}) Tj");
        pdf.AppendLine("ET");
        return System.Text.Encoding.ASCII.GetBytes(pdf.ToString());
    }

    private byte[] CreateOfficeXmlDocument(string type)
    {
        // Create minimal ZIP structure (Office Open XML format)
        using var ms = new System.IO.MemoryStream();
        using (var archive = new System.IO.Compression.ZipArchive(ms, System.IO.Compression.ZipArchiveMode.Create, true))
        {
            var entry = archive.CreateEntry($"word/document.xml");
            using var writer = new System.IO.StreamWriter(entry.Open());
            writer.Write("<?xml version=\"1.0\"?><document></document>");
        }
        return ms.ToArray();
    }

    private byte[] CreateRtfDocument(string plainText)
    {
        var rtf = $"{{\\rtf1\\ansi {plainText}}}";
        return System.Text.Encoding.ASCII.GetBytes(rtf);
    }

    #endregion
}
