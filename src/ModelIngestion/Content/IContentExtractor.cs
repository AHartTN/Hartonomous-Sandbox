using System.Threading;
using System.Threading.Tasks;

namespace ModelIngestion.Content;

/// <summary>
/// Contract implemented by parsers that transform raw content into atom ingestion requests.
/// </summary>
public interface IContentExtractor
{
    bool CanHandle(ContentExtractionContext context);

    Task<ContentExtractionResult> ExtractAsync(ContentExtractionContext context, CancellationToken cancellationToken);
}
