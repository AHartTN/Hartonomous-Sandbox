using System.Threading;
using System.Threading.Tasks;

namespace Hartonomous.Infrastructure.Services.ContentExtraction;

/// <summary>
/// Contract implemented by parsers that transform raw content into atom ingestion requests.
/// </summary>
public interface IContentExtractor
{
    bool CanHandle(ContentExtractionContext context);

    Task<ContentExtractionResult> ExtractAsync(ContentExtractionContext context, CancellationToken cancellationToken);
}
