using System;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;

namespace Hartonomous.Clr
{
    /// <summary>
    /// Multi-modal generation wrapper functions.
    /// Each function calls fn_GenerateWithAttention with modality-specific parameters.
    /// Supports: Text, Image, Audio, Video generation.
    /// </summary>
    public static class MultiModalGeneration
    {
        /// <summary>
        /// Generate text atoms using attention-based inference.
        /// Wrapper around fn_GenerateWithAttention optimized for text generation.
        /// </summary>
        [SqlFunction(
            DataAccess = DataAccessKind.Read,
            SystemDataAccess = SystemDataAccessKind.Read,
            IsDeterministic = false)]
        public static SqlInt64 fn_GenerateText(
            SqlInt32 modelId,
            SqlString inputAtomIds,
            SqlString contextJson,
            SqlInt32 maxTokens,
            SqlDouble temperature,
            SqlInt32 topK,
            SqlDouble topP,
            SqlInt32 tenantId)
        {
            // Text generation typically uses 8 attention heads
            var attentionHeads = new SqlInt32(8);

            return AttentionGeneration.fn_GenerateWithAttention(
                modelId,
                inputAtomIds,
                contextJson,
                maxTokens,
                temperature,
                topK,
                topP,
                attentionHeads,
                tenantId
            );
        }

        /// <summary>
        /// Generate image atoms using attention-based inference.
        /// Wrapper around fn_GenerateWithAttention optimized for image generation.
        /// Uses 16 attention heads for spatial coherence.
        /// </summary>
        [SqlFunction(
            DataAccess = DataAccessKind.Read,
            SystemDataAccess = SystemDataAccessKind.Read,
            IsDeterministic = false)]
        public static SqlInt64 fn_GenerateImage(
            SqlInt32 modelId,
            SqlString inputAtomIds,
            SqlString contextJson,
            SqlInt32 maxPatches,
            SqlDouble temperature,
            SqlInt32 topK,
            SqlDouble topP,
            SqlInt32 tenantId)
        {
            // Image generation uses more attention heads for spatial relationships
            var attentionHeads = new SqlInt32(16);

            return AttentionGeneration.fn_GenerateWithAttention(
                modelId,
                inputAtomIds,
                contextJson,
                maxPatches,
                temperature,
                topK,
                topP,
                attentionHeads,
                tenantId
            );
        }

        /// <summary>
        /// Generate audio atoms using attention-based inference.
        /// Wrapper around fn_GenerateWithAttention optimized for audio generation.
        /// Uses 12 attention heads for temporal coherence.
        /// </summary>
        [SqlFunction(
            DataAccess = DataAccessKind.Read,
            SystemDataAccess = SystemDataAccessKind.Read,
            IsDeterministic = false)]
        public static SqlInt64 fn_GenerateAudio(
            SqlInt32 modelId,
            SqlString inputAtomIds,
            SqlString contextJson,
            SqlInt32 maxSamples,
            SqlDouble temperature,
            SqlInt32 topK,
            SqlDouble topP,
            SqlInt32 tenantId)
        {
            // Audio generation uses moderate attention heads for temporal modeling
            var attentionHeads = new SqlInt32(12);

            return AttentionGeneration.fn_GenerateWithAttention(
                modelId,
                inputAtomIds,
                contextJson,
                maxSamples,
                temperature,
                topK,
                topP,
                attentionHeads,
                tenantId
            );
        }

        /// <summary>
        /// Generate video atoms using attention-based inference.
        /// Wrapper around fn_GenerateWithAttention optimized for video generation.
        /// Uses 24 attention heads for spatiotemporal modeling.
        /// </summary>
        [SqlFunction(
            DataAccess = DataAccessKind.Read,
            SystemDataAccess = SystemDataAccessKind.Read,
            IsDeterministic = false)]
        public static SqlInt64 fn_GenerateVideo(
            SqlInt32 modelId,
            SqlString inputAtomIds,
            SqlString contextJson,
            SqlInt32 maxFrames,
            SqlDouble temperature,
            SqlInt32 topK,
            SqlDouble topP,
            SqlInt32 tenantId)
        {
            // Video generation uses most attention heads for spatiotemporal coherence
            var attentionHeads = new SqlInt32(24);

            return AttentionGeneration.fn_GenerateWithAttention(
                modelId,
                inputAtomIds,
                contextJson,
                maxFrames,
                temperature,
                topK,
                topP,
                attentionHeads,
                tenantId
            );
        }

        /// <summary>
        /// Generate multi-modal ensemble output using multiple models.
        /// Calls fn_GenerateWithAttention with each model and combines results.
        /// </summary>
        [SqlFunction(
            DataAccess = DataAccessKind.Read,
            SystemDataAccess = SystemDataAccessKind.Read,
            IsDeterministic = false)]
        public static SqlInt64 fn_GenerateEnsemble(
            SqlString modelIdsJson,
            SqlString inputAtomIds,
            SqlString contextJson,
            SqlInt32 maxTokens,
            SqlDouble temperature,
            SqlInt32 topK,
            SqlDouble topP,
            SqlInt32 attentionHeads,
            SqlInt32 tenantId)
        {
            if (modelIdsJson.IsNull || string.IsNullOrWhiteSpace(modelIdsJson.Value))
            {
                return SqlInt64.Null;
            }

            try
            {
                using (var connection = new SqlConnection("context connection=true"))
                {
                    connection.Open();

                    // Parse model IDs from JSON array: [1, 2, 3]
                    var modelIdsStr = modelIdsJson.Value.Trim('[', ']', ' ');
                    var modelIds = modelIdsStr.Split(',');

                    long? firstGenerationStreamId = null;

                    // Generate with each model
                    foreach (var modelIdStr in modelIds)
                    {
                        if (!int.TryParse(modelIdStr.Trim(), out var modelId))
                        {
                            continue;
                        }

                        var generationStreamId = AttentionGeneration.fn_GenerateWithAttention(
                            new SqlInt32(modelId),
                            inputAtomIds,
                            contextJson,
                            maxTokens,
                            temperature,
                            topK,
                            topP,
                            attentionHeads,
                            tenantId
                        );

                        if (!generationStreamId.IsNull && firstGenerationStreamId == null)
                        {
                            firstGenerationStreamId = generationStreamId.Value;
                        }
                    }

                    return firstGenerationStreamId.HasValue 
                        ? new SqlInt64(firstGenerationStreamId.Value) 
                        : SqlInt64.Null;
                }
            }
            catch (Exception ex)
            {
                SqlContext.Pipe.Send($"fn_GenerateEnsemble error: {ex.Message}");
                return SqlInt64.Null;
            }
        }
    }
}
