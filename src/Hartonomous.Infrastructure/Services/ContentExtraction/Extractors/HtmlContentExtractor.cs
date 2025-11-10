using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using Hartonomous.Core.Interfaces;
using Hartonomous.Infrastructure.Services.ContentExtraction;

namespace Hartonomous.Infrastructure.Services.ContentExtraction.Extractors;

/// <summary>
/// Extracts structured content from HTML documents.
/// Converts web pages into atom hierarchies with semantic relationships.
/// </summary>
public sealed class HtmlContentExtractor : IContentExtractor
{
    private static readonly HashSet<string> SupportedTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "text/html",
        "application/xhtml+xml"
    };

    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".html",
        ".htm",
        ".xhtml"
    };

    public bool CanHandle(ContentExtractionContext context)
    {
        if (!string.IsNullOrWhiteSpace(context.ContentType) && SupportedTypes.Contains(context.ContentType))
        {
            return true;
        }

        if (!string.IsNullOrWhiteSpace(context.Extension) && SupportedExtensions.Contains(context.Extension))
        {
            return true;
        }

        return false;
    }

    public async Task<ContentExtractionResult> ExtractAsync(ContentExtractionContext context, CancellationToken cancellationToken)
    {
        if (context.ContentStream == null)
        {
            throw new ArgumentException("Content stream is required for HTML extraction", nameof(context));
        }

        var config = Configuration.Default;
        var browsingContext = BrowsingContext.New(config);
        
        IDocument document;
        context.ContentStream.Position = 0;
        document = await browsingContext.OpenAsync(req => req.Content(context.ContentStream), cancellationToken);

        var requests = new List<AtomIngestionRequest>();
        var diagnostics = new Dictionary<string, string>();

        // Extract metadata
        var title = document.QuerySelector("title")?.TextContent?.Trim() ?? "Untitled";
        var metaDescription = document.QuerySelector("meta[name='description']")?.GetAttribute("content");
        var baseUrl = context.Metadata?.TryGetValue("sourceUri", out var uri) == true ? uri : null;

        diagnostics["title"] = title;
        diagnostics["url"] = baseUrl ?? "unknown";
        
        // 1. Main content extraction (article/main content)
        var mainContent = ExtractMainContent(document);
        if (!string.IsNullOrWhiteSpace(mainContent))
        {
            var metadata = new MetadataEnvelope()
                .Set("title", title)
                .Set("description", metaDescription ?? string.Empty)
                .Set("word_count", mainContent.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length)
                .Set("char_count", mainContent.Length);

            var mainAtom = new AtomIngestionRequestBuilder()
                .WithCanonicalText(mainContent)
                .WithModality("text", "html_main_content")
                .WithSource("web_scrape", baseUrl)
                .WithMetadata(metadata)
                .Build();
            
            requests.Add(mainAtom);
            diagnostics["main_content_length"] = mainContent.Length.ToString();
        }

        // 2. Extract headings as structured hierarchy
        var headings = ExtractHeadings(document, baseUrl);
        requests.AddRange(headings);
        diagnostics["headings_extracted"] = headings.Count.ToString();

        // 3. Extract links
        var links = ExtractLinks(document, baseUrl);
        requests.AddRange(links);
        diagnostics["links_extracted"] = links.Count.ToString();

        // 4. Extract images
        var images = ExtractImages(document, baseUrl);
        requests.AddRange(images);
        diagnostics["images_extracted"] = images.Count.ToString();

        // 5. Extract tables
        var tables = ExtractTables(document, baseUrl);
        requests.AddRange(tables);
        diagnostics["tables_extracted"] = tables.Count.ToString();

        // 6. Extract meta tags
        var metaTags = ExtractMetaTags(document, baseUrl);
        requests.AddRange(metaTags);
        diagnostics["meta_tags_extracted"] = metaTags.Count.ToString();

        diagnostics["total_atoms_created"] = requests.Count.ToString();

        return new ContentExtractionResult(requests, diagnostics);
    }

    private string ExtractMainContent(IDocument document)
    {
        // Priority order for main content detection
        var contentSelectors = new[]
        {
            "article",
            "main",
            "[role='main']",
            ".article-content",
            ".post-content",
            ".entry-content",
            "#content",
            ".content"
        };

        foreach (var selector in contentSelectors)
        {
            var element = document.QuerySelector(selector);
            if (element != null)
            {
                return CleanText(element.TextContent);
            }
        }

        // Fallback: extract body text excluding nav/footer/header
        var body = document.Body;
        if (body != null)
        {
            var excludeSelectors = new[] { "nav", "header", "footer", "aside", ".sidebar", ".navigation" };
            var clone = body.Clone() as IElement;
            
            foreach (var selector in excludeSelectors)
            {
                var elements = clone?.QuerySelectorAll(selector);
                if (elements != null)
                {
                    foreach (var el in elements.ToList())
                    {
                        el.Remove();
                    }
                }
            }

            return CleanText(clone?.TextContent ?? string.Empty);
        }

        return string.Empty;
    }

    private List<AtomIngestionRequest> ExtractHeadings(IDocument document, string? baseUrl)
    {
        var headings = new List<AtomIngestionRequest>();
        var headingElements = document.QuerySelectorAll("h1, h2, h3, h4, h5, h6");

        foreach (var heading in headingElements)
        {
            var text = CleanText(heading.TextContent);
            if (string.IsNullOrWhiteSpace(text)) continue;

            var level = heading.TagName.ToLower()[1] - '0'; // h1 -> 1, h2 -> 2, etc.
            
            var metadata = new MetadataEnvelope()
                .Set("heading_level", level)
                .Set("heading_id", heading.Id ?? string.Empty)
                .Set("heading_class", heading.ClassName ?? string.Empty);

            var atom = new AtomIngestionRequestBuilder()
                .WithCanonicalText(text)
                .WithModality("text", $"html_heading_{level}")
                .WithSource("web_scrape", baseUrl)
                .WithMetadata(metadata)
                .Build();

            headings.Add(atom);
        }

        return headings;
    }

    private List<AtomIngestionRequest> ExtractLinks(IDocument document, string? baseUrl)
    {
        var links = new List<AtomIngestionRequest>();
        var anchorElements = document.QuerySelectorAll("a[href]");

        foreach (var anchor in anchorElements.OfType<IHtmlAnchorElement>())
        {
            var href = anchor.Href;
            var text = CleanText(anchor.TextContent);
            
            if (string.IsNullOrWhiteSpace(href)) continue;

            var isExternal = !href.StartsWith("/") && !href.StartsWith("#") && 
                           (href.StartsWith("http://") || href.StartsWith("https://"));

            var metadata = new MetadataEnvelope()
                .Set("link_text", text)
                .Set("link_href", href)
                .Set("link_title", anchor.Title ?? string.Empty)
                .Set("is_external", isExternal ? "true" : "false");

            var atom = new AtomIngestionRequestBuilder()
                .WithCanonicalText($"{text} -> {href}")
                .WithModality("link", "html_hyperlink")
                .WithSource("web_scrape", baseUrl)
                .WithMetadata(metadata)
                .Build();

            links.Add(atom);
        }

        return links;
    }

    private List<AtomIngestionRequest> ExtractImages(IDocument document, string? baseUrl)
    {
        var images = new List<AtomIngestionRequest>();
        var imgElements = document.QuerySelectorAll("img[src]");

        foreach (var img in imgElements.OfType<IHtmlImageElement>())
        {
            var src = img.Source;
            var alt = img.AlternativeText ?? string.Empty;
            
            if (string.IsNullOrWhiteSpace(src)) continue;

            var metadata = new MetadataEnvelope()
                .Set("image_src", src)
                .Set("image_alt", alt)
                .Set("image_title", img.Title ?? string.Empty);

            var atom = new AtomIngestionRequestBuilder()
                .WithCanonicalText(alt)
                .WithModality("image", "html_image_reference")
                .WithSource("web_scrape", baseUrl)
                .WithMetadata(metadata)
                .WithPayloadLocator(src)
                .Build();

            images.Add(atom);
        }

        return images;
    }

    private List<AtomIngestionRequest> ExtractTables(IDocument document, string? baseUrl)
    {
        var tables = new List<AtomIngestionRequest>();
        var tableElements = document.QuerySelectorAll("table");

        foreach (var table in tableElements.OfType<IHtmlTableElement>())
        {
            var rows = table.Rows;
            if (rows.Length == 0) continue;

            var tableData = new List<List<string>>();
            foreach (var row in rows)
            {
                var rowData = new List<string>();
                foreach (var cell in row.Cells)
                {
                    rowData.Add(CleanText(cell.TextContent));
                }
                tableData.Add(rowData);
            }

            var tableText = string.Join("\n", tableData.Select(row => string.Join(" | ", row)));

            var metadata = new MetadataEnvelope()
                .Set("table_rows", rows.Length)
                .Set("table_columns", rows[0].Cells.Length)
                .Set("table_id", table.Id ?? string.Empty)
                .Set("table_class", table.ClassName ?? string.Empty);

            var atom = new AtomIngestionRequestBuilder()
                .WithCanonicalText(tableText)
                .WithModality("structured_data", "html_table")
                .WithSource("web_scrape", baseUrl)
                .WithMetadata(metadata)
                .Build();

            tables.Add(atom);
        }

        return tables;
    }

    private List<AtomIngestionRequest> ExtractMetaTags(IDocument document, string? baseUrl)
    {
        var metaTags = new List<AtomIngestionRequest>();
        var metaElements = document.QuerySelectorAll("meta[name], meta[property]");

        foreach (var meta in metaElements.OfType<IHtmlMetaElement>())
        {
            var name = meta.Name ?? meta.GetAttribute("property") ?? string.Empty;
            var content = meta.Content ?? string.Empty;

            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(content)) continue;

            var metadata = new MetadataEnvelope()
                .Set("meta_name", name)
                .Set("meta_content", content);

            var atom = new AtomIngestionRequestBuilder()
                .WithCanonicalText(content)
                .WithModality("metadata", "html_meta_tag")
                .WithSource("web_scrape", baseUrl)
                .WithMetadata(metadata)
                .Build();

            metaTags.Add(atom);
        }

        return metaTags;
    }

    private string CleanText(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;

        // Remove excessive whitespace
        var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var cleanedLines = lines.Select(line => line.Trim()).Where(line => !string.IsNullOrWhiteSpace(line));
        
        return string.Join("\n", cleanedLines).Trim();
    }
}
