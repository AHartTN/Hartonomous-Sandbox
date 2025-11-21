using System.Threading;
using System.Threading.Tasks;

namespace Hartonomous.Core.Interfaces.Atomization;

/// <summary>
/// Service for atomization operations via stored procedures.
/// These complement the in-memory atomizers by providing database-level atomization.
/// </summary>
public interface IAtomizationService
{
    /// <summary>
    /// Atomizes source code via Roslyn AST parsing and 3D spatial projection.
    /// Calls sp_AtomizeCode stored procedure.
    /// </summary>
    Task AtomizeCodeAsync(
        long atomId,
        int tenantId = 0,
        string language = "csharp",
        bool debug = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Atomizes text with governance checks.
    /// Calls sp_AtomizeText_Governed stored procedure.
    /// </summary>
    Task AtomizeTextAsync(
        long atomId,
        int tenantId = 0,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Atomizes image with governance checks.
    /// Calls sp_AtomizeImage_Governed stored procedure.
    /// </summary>
    Task AtomizeImageAsync(
        long atomId,
        int tenantId = 0,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Atomizes model with governance checks.
    /// Calls sp_AtomizeModel_Governed stored procedure.
    /// </summary>
    Task AtomizeModelAsync(
        long atomId,
        int tenantId = 0,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Tokenizes text to token IDs using vocabulary lookup.
    /// Calls sp_TokenizeText stored procedure.
    /// </summary>
    Task<int[]> TokenizeTextAsync(
        string text,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Converts text to embedding vector.
    /// Calls sp_TextToEmbedding stored procedure.
    /// </summary>
    Task<(byte[] Embedding, int Dimension)> TextToEmbeddingAsync(
        string text,
        string? modelName = null,
        CancellationToken cancellationToken = default);
}
