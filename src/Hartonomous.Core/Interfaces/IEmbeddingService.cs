namespace Hartonomous.Core.Interfaces;

/// <summary>
/// Unified embedding service that generates embeddings in a SHARED SEMANTIC SPACE
/// across all modalities (text, image, audio, video) using DATABASE-NATIVE operations.
/// Enables zero-shot cross-modal learning WITHOUT external models or pre-trained weights.
/// 
/// THE DATABASE IS THE MODEL:
/// - Embeddings computed FROM relationships in existing database data
/// - Learning happens through spatial clustering + feedback loop SQL UPDATEs
/// - NO CLIP, NO Whisper, NO external dependencies
/// - System learns emergently from vector proximity and user feedback
/// 
/// Key Concept: Text "a cat has whiskers" and image of cat cluster together
/// in embedding space through VECTOR_DISTANCE proximity, even if system never
/// saw labeled cat images. Feedback loop refines weights via sp_UpdateModelWeightsFromFeedback.
/// 
/// Embedding Generation (Native Approach):
/// - Text: TF-IDF from existing corpus → statistical features → VECTOR(768)
/// - Image: Pixel histogram + edge detection → spatial features → VECTOR(768)
/// - Audio: FFT spectrum + MFCC → acoustic features → VECTOR(768)
/// - Video: Frame-by-frame pixel features → temporal aggregation → VECTOR(768)
/// 
/// Storage and Indexing:
/// 1. INSERT INTO Embeddings with VECTOR(768)
/// 2. Auto-trigger sp_ComputeSpatialProjection (768D → 3D GEOMETRY via distance-based coordinates)
/// 3. Spatial indexes enable O(log n) hybrid search
/// 4. Feedback loop updates ModelLayers.Weights via SQL UPDATE
/// </summary>
public interface IEmbeddingService
{
    /// <summary>
    /// Embeds text using TF-IDF from existing corpus. Tokenizes text, computes term frequencies,
    /// generates vector from vocabulary relationships stored in database.
    /// </summary>
    /// <param name="text">Text to embed (description, document, caption, etc.)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>768-dimensional embedding computed from term statistics</returns>
    Task<float[]> EmbedTextAsync(string text, CancellationToken cancellationToken = default);

    /// <summary>
    /// Embeds image using pixel histogram and edge detection. Extracts color distribution,
    /// spatial gradients, correlates with existing image embeddings in database.
    /// </summary>
    /// <param name="imageBytes">Image bytes (JPEG, PNG, etc.)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>768-dimensional embedding computed from pixel features</returns>
    Task<float[]> EmbedImageAsync(byte[] imageBytes, CancellationToken cancellationToken = default);

    /// <summary>
    /// Embeds audio using FFT spectrum and MFCC features. Computes frequency distribution,
    /// acoustic patterns, correlates with existing audio embeddings in database.
    /// </summary>
    /// <param name="audioBytes">Audio bytes (WAV, MP3, etc.)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>768-dimensional embedding computed from acoustic features</returns>
    Task<float[]> EmbedAudioAsync(byte[] audioBytes, CancellationToken cancellationToken = default);

    /// <summary>
    /// Embeds video frame using pixel features (delegates to image embedding logic).
    /// </summary>
    /// <param name="frameBytes">Video frame as image bytes</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>768-dimensional embedding for the frame</returns>
    Task<float[]> EmbedVideoFrameAsync(byte[] frameBytes, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stores embedding in database with automatic spatial projection for hybrid search.
    /// Triggers sp_ComputeSpatialProjection (768D VECTOR → 3D GEOMETRY distance-based coordinates).
    /// Key integration: embedding generation + storage + indexing in one operation.
    /// </summary>
    /// <param name="embedding">Generated 768-dimensional embedding vector</param>
    /// <param name="sourceData">Original data (text string or image/audio bytes)</param>
    /// <param name="sourceType">Type: 'text', 'image', 'audio', 'video_frame', 'scada'</param>
    /// <param name="metadata">Optional metadata (JSON)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Embedding ID in database</returns>
    Task<long> StoreEmbeddingAsync(
        float[] embedding,
        object sourceData,
        string sourceType,
        string? metadata = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Complete pipeline: Generate embedding from any input and store with spatial projection.
    /// Automatically selects embedding method based on inputType, stores in Embeddings table.
    /// </summary>
    /// <param name="input">Input data (text string or byte[] for media)</param>
    /// <param name="inputType">Type: 'text', 'image', 'audio', 'video_frame'</param>
    /// <param name="metadata">Optional metadata</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Embedding ID and 768-dimensional vector</returns>
    Task<(long embeddingId, float[] embedding)> GenerateAndStoreAsync(
        object input,
        string inputType,
        string? metadata = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Zero-shot classification: Given an image and list of text labels,
    /// returns probabilities for each label WITHOUT training examples.
    /// Uses VECTOR_DISTANCE to compute similarity between image embedding and text label embeddings.
    /// System learns which labels cluster near which images through spatial proximity + feedback loop.
    /// 
    /// Example: labels = ["a cat", "a dog", "a bird"]
    ///          Returns: {"a cat": 0.85, "a dog": 0.10, "a bird": 0.05} (85% confident it's a cat)
    /// </summary>
    /// <param name="imageBytes">Image to classify</param>
    /// <param name="labels">Text descriptions of possible classes</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Probability distribution over labels</returns>
    Task<Dictionary<string, float>> ZeroShotClassifyAsync(
        byte[] imageBytes,
        IReadOnlyList<string> labels,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Zero-shot image retrieval: Find images that match text description,
    /// even if system never saw images with this specific description.
    /// Uses sp_HybridSearch with text embedding as query against image embeddings.
    /// System learns semantic relationships through VECTOR_DISTANCE clustering.
    /// 
    /// Example: description = "a cat with medical equipment" 
    ///          Returns: Images semantically similar via spatial proximity in embedding space
    /// </summary>
    /// <param name="textDescription">Text query</param>
    /// <param name="topK">Number of results</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Image IDs and similarity scores from VECTOR_DISTANCE</returns>
    Task<IReadOnlyList<(long imageId, float similarity)>> ZeroShotImageRetrievalAsync(
        string textDescription,
        int topK = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cross-modal search: Find ANY content (text/image/audio/video) 
    /// semantically similar to query, regardless of modality mismatch.
    /// Uses sp_CrossModalQuery stored procedure (spatial correlation + exact vector rerank).
    /// Database operations implement the cross-modal learning - NO external models.
    /// 
    /// Example: Query with audio of cat meowing → finds text "cats make meowing sounds"
    ///                                           + images of cats
    ///                                           + videos with cats
    /// 
    /// All results cluster in same semantic region of embedding space through learned vector proximity.
    /// </summary>
    /// <param name="queryEmbedding">Query embedding (from any modality)</param>
    /// <param name="topK">Number of results</param>
    /// <param name="filterByType">Optional: filter results to specific type</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Results across all modalities</returns>
    Task<IReadOnlyList<CrossModalResult>> CrossModalSearchAsync(
        float[] queryEmbedding,
        int topK = 10,
        string? filterByType = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result from cross-modal search spanning multiple data types.
/// </summary>
public sealed class CrossModalResult
{
    public long Id { get; init; }
    public string SourceType { get; init; } = string.Empty; // 'text', 'image', 'audio', 'video_frame'
    public float Similarity { get; init; }
    public object? Content { get; init; } // Text string, image bytes, etc.
    public string? Metadata { get; init; }
}
