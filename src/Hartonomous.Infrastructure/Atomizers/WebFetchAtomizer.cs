using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Hartonomous.Core.Interfaces.Ingestion;

namespace Hartonomous.Infrastructure.Atomizers;

/// <summary>
/// Atomizes web content from HTTP/HTTPS URLs by fetching and parsing the response.
/// Supports HTML, JSON, XML, images, and other web resources.
/// </summary>
public class WebFetchAtomizer : IAtomizer<string>
{
    private readonly HttpClient _httpClient;
    private readonly IFileTypeDetector _fileTypeDetector;
    private readonly IEnumerable<IAtomizer<byte[]>> _byteAtomizers;
    private const int MaxAtomSize = 64;
    public int Priority => 50;

    public WebFetchAtomizer(
        HttpClient httpClient,
        IFileTypeDetector fileTypeDetector,
        IEnumerable<IAtomizer<byte[]>> byteAtomizers)
    {
        _httpClient = httpClient;
        _fileTypeDetector = fileTypeDetector;
        _byteAtomizers = byteAtomizers;
    }

    public bool CanHandle(string contentType, string? fileExtension)
    {
        // This atomizer handles URL strings, not file content types
        return false; // Will be invoked explicitly via API endpoint
    }

    /// <summary>
    /// Fetch URL content and atomize based on detected type.
    /// Input is the URL string to fetch.
    /// </summary>
    public async Task<AtomizationResult> AtomizeAsync(string url, SourceMetadata source, CancellationToken cancellationToken)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var atoms = new List<AtomData>();
        var compositions = new List<AtomComposition>();
        var warnings = new List<string>();

        try
        {
            // Validate URL
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
                (uri.Scheme != "http" && uri.Scheme != "https"))
            {
                throw new ArgumentException($"Invalid HTTP/HTTPS URL: {url}");
            }

            // Create URL atom (actual URL string, properly sized)
            var urlBytes = Encoding.UTF8.GetBytes(url);
            var urlHash = SHA256.HashData(urlBytes);
            var urlAtom = new AtomData
            {
                AtomicValue = urlBytes.Length <= MaxAtomSize ? urlBytes : urlBytes.Take(MaxAtomSize).ToArray(),
                ContentHash = urlHash,
                Modality = "web",
                Subtype = "url",
                ContentType = "text/uri-list",
                CanonicalText = url,
                Metadata = $"{{\"scheme\":\"{uri.Scheme}\",\"host\":\"{uri.Host}\",\"path\":\"{uri.AbsolutePath}\",\"fullUrl\":\"{url}\"}}"
            };
            atoms.Add(urlAtom);

            // Fetch content
            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var contentBytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
            var contentType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";

            // Create response metadata atom (not the actual content)
            var responseMetadataBytes = Encoding.UTF8.GetBytes($"http-response:{(int)response.StatusCode}:{contentBytes.Length}");
            var responseHash = SHA256.HashData(contentBytes);
            var responseAtom = new AtomData
            {
                AtomicValue = responseMetadataBytes.Length <= MaxAtomSize ? responseMetadataBytes : responseMetadataBytes.Take(MaxAtomSize).ToArray(),
                ContentHash = responseHash,
                Modality = "web",
                Subtype = "http-response-metadata",
                ContentType = contentType,
                CanonicalText = $"HTTP {(int)response.StatusCode} - {contentBytes.Length:N0} bytes",
                Metadata = $"{{\"statusCode\":{(int)response.StatusCode},\"contentType\":\"{contentType}\",\"size\":{contentBytes.Length}}}"
            };
            atoms.Add(responseAtom);

            // Link response to URL
            compositions.Add(new AtomComposition
            {
                ParentAtomHash = urlHash,
                ComponentAtomHash = responseHash,
                SequenceIndex = 0,
                Position = new SpatialPosition { X = 0, Y = 0, Z = 0 }
            });

            // Detect content type and delegate to appropriate atomizer
            var fileName = uri.Segments.Length > 0 ? uri.Segments[^1] : null;
            var fileType = _fileTypeDetector.Detect(contentBytes, fileName);

            var atomizer = _byteAtomizers
                .Where(a => a.CanHandle(fileType.ContentType, fileType.Extension))
                .OrderByDescending(a => a.Priority)
                .FirstOrDefault();

            if (atomizer != null)
            {
                var contentSource = new SourceMetadata
                {
                    FileName = fileName,
                    SourceUri = url,
                    SourceType = "web-fetch",
                    ContentType = contentType,
                    SizeBytes = contentBytes.Length,
                    TenantId = source.TenantId,
                    Metadata = $"{{\"url\":\"{url}\",\"statusCode\":{(int)response.StatusCode}}}"
                };

                var result = await atomizer.AtomizeAsync(contentBytes, contentSource, cancellationToken);
                
                // Merge results
                atoms.AddRange(result.Atoms);
                
                // Link content atoms to response
                foreach (var atom in result.Atoms)
                {
                    compositions.Add(new AtomComposition
                    {
                        ParentAtomHash = responseHash,
                        ComponentAtomHash = atom.ContentHash,
                        SequenceIndex = compositions.Count,
                        Position = new SpatialPosition { X = 0, Y = 0, Z = 0 }
                    });
                }

                compositions.AddRange(result.Compositions);
                warnings.AddRange(result.ProcessingInfo.Warnings ?? Enumerable.Empty<string>());
            }
            else
            {
                warnings.Add($"No atomizer found for content type: {contentType}");
            }

            sw.Stop();

            return new AtomizationResult
            {
                Atoms = atoms,
                Compositions = compositions,
                ProcessingInfo = new ProcessingMetadata
                {
                    TotalAtoms = atoms.Count,
                    UniqueAtoms = atoms.Select(a => Convert.ToBase64String(a.ContentHash)).Distinct().Count(),
                    DurationMs = sw.ElapsedMilliseconds,
                    AtomizerType = nameof(WebFetchAtomizer),
                    DetectedFormat = $"HTTP {(int)response.StatusCode} - {fileType.ContentType}",
                    Warnings = warnings.Count > 0 ? warnings : null
                }
            };
        }
        catch (HttpRequestException ex)
        {
            warnings.Add($"HTTP request failed: {ex.Message}");
            throw;
        }
        catch (Exception ex)
        {
            warnings.Add($"Web fetch atomization failed: {ex.Message}");
            throw;
        }
    }
}
