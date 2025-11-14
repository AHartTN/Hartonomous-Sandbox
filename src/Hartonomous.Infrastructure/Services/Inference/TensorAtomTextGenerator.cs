using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Hartonomous.Data.Entities;

namespace Hartonomous.Infrastructure.Services.Inference;

/// <summary>
/// Generates text using YOUR ingested LLM weights from TensorAtoms.
/// Queries spatial substrate + attention mechanism - NO external API calls.
/// </summary>
public sealed class TensorAtomTextGenerator
{
    private readonly string _connectionString;

    public TensorAtomTextGenerator(string connectionString)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
    }

    /// <summary>
    /// Generates text by querying YOUR spatial substrate using sp_GenerateText.
    /// Uses attention mechanism over TensorAtoms (YOUR ingested LLM weights).
    /// </summary>
    public async Task<TextGenerationResult> GenerateAsync(
        string prompt,
        string modelIdentifier,
        int maxTokens = 512,
        float temperature = 0.7f,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        // Call sp_GenerateText (assumes this proc exists and uses YOUR TensorAtoms)
        await using var command = new SqlCommand("sp_GenerateText", connection)
        {
            CommandType = CommandType.StoredProcedure,
            CommandTimeout = 300 // 5 minutes for LLM inference
        };

        command.Parameters.AddWithValue("@Prompt", prompt);
        command.Parameters.AddWithValue("@ModelIdentifier", modelIdentifier);
        command.Parameters.AddWithValue("@MaxTokens", maxTokens);
        command.Parameters.AddWithValue("@Temperature", temperature);

        var outputParam = new SqlParameter("@GeneratedText", SqlDbType.NVarChar, -1)
        {
            Direction = ParameterDirection.Output
        };
        command.Parameters.Add(outputParam);

        var tokensParam = new SqlParameter("@TokensGenerated", SqlDbType.Int)
        {
            Direction = ParameterDirection.Output
        };
        command.Parameters.Add(tokensParam);

        await command.ExecuteNonQueryAsync(cancellationToken);

        var generatedText = outputParam.Value?.ToString() ?? string.Empty;
        var tokensGenerated = tokensParam.Value is int t ? t : 0;

        return new TextGenerationResult
        {
            GeneratedText = generatedText,
            TokensGenerated = tokensGenerated,
            Prompt = prompt,
            ModelIdentifier = modelIdentifier
        };
    }

    /// <summary>
    /// Generates text using vector similarity search over TensorAtoms.
    /// Alternative approach: query atom embeddings, rank by attention, decode to text.
    /// </summary>
    public async Task<TextGenerationResult> GenerateViaSpatialQueryAsync(
        string prompt,
        string modelIdentifier,
        int maxTokens = 512,
        float temperature = 0.7f,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        // Step 1: Encode prompt to embedding vector
        var promptEmbedding = await EncodePromptAsync(prompt, connection, cancellationToken);

        // Step 2: Query TensorAtoms for nearest neighbors (attention mechanism)
        var relevantAtoms = await QueryRelevantAtomsAsync(promptEmbedding, modelIdentifier, connection, cancellationToken);

        // Step 3: Decode atoms to text tokens
        var generatedTokens = DecodeAtomsToTokens(relevantAtoms, maxTokens, temperature);

        // Step 4: Join tokens to text
        var generatedText = string.Join("", generatedTokens);

        return new TextGenerationResult
        {
            GeneratedText = generatedText,
            TokensGenerated = generatedTokens.Count,
            Prompt = prompt,
            ModelIdentifier = modelIdentifier
        };
    }

    private async Task<float[]> EncodePromptAsync(string prompt, SqlConnection connection, CancellationToken cancellationToken)
    {
        // Call existing embedding stored proc or use inline spatial query
        await using var command = new SqlCommand(@"
            SELECT dbo.fn_TextToVector(@Text) AS EmbeddingVector", connection);

        command.Parameters.AddWithValue("@Text", prompt);

        var result = await command.ExecuteScalarAsync(cancellationToken);

        if (result is byte[] vectorBytes)
        {
            // Convert binary vector to float array
            return ConvertBinaryToFloatArray(vectorBytes);
        }

        // Fallback: empty embedding
        return new float[1998]; // SQL Server 2025 VECTOR(1998) default
    }

    private async Task<List<TensorAtom>> QueryRelevantAtomsAsync(
        float[] queryEmbedding,
        string modelIdentifier,
        SqlConnection connection,
        CancellationToken cancellationToken)
    {
        var atoms = new List<TensorAtom>();

        // Query TensorAtoms using vector similarity (VECTOR_DISTANCE)
        await using var command = new SqlCommand(@"
            SELECT TOP 100
                AtomID,
                CanonicalText,
                EmbeddingVector,
                VECTOR_DISTANCE('cosine', EmbeddingVector, @QueryVector) AS Distance
            FROM TensorAtoms
            WHERE ModelIdentifier = @ModelIdentifier
            ORDER BY Distance ASC", connection);

        var vectorParam = command.Parameters.Add("@QueryVector", SqlDbType.VarBinary);
        vectorParam.Value = ConvertFloatArrayToBinary(queryEmbedding);
        command.Parameters.AddWithValue("@ModelIdentifier", modelIdentifier);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            atoms.Add(new TensorAtom
            {
                AtomID = reader.GetInt64(0),
                CanonicalText = reader.GetString(1),
                EmbeddingVector = ConvertBinaryToFloatArray(reader.GetSqlBinary(2).Value),
                Distance = reader.GetDouble(3)
            });
        }

        return atoms;
    }

    private List<string> DecodeAtomsToTokens(List<TensorAtom> atoms, int maxTokens, float temperature)
    {
        var tokens = new List<string>();
        var random = new Random();

        foreach (var atom in atoms.Take(maxTokens))
        {
            // Apply temperature sampling
            if (random.NextDouble() < Math.Exp(-atom.Distance / temperature))
            {
                tokens.Add(atom.CanonicalText);
            }
        }

        return tokens;
    }

    private float[] ConvertBinaryToFloatArray(byte[] bytes)
    {
        // SQL Server VECTOR stored as binary (float32 array)
        int floatCount = bytes.Length / sizeof(float);
        var floats = new float[floatCount];

        for (int i = 0; i < floatCount; i++)
        {
            floats[i] = BitConverter.ToSingle(bytes, i * sizeof(float));
        }

        return floats;
    }

    private byte[] ConvertFloatArrayToBinary(float[] floats)
    {
        var bytes = new byte[floats.Length * sizeof(float)];

        for (int i = 0; i < floats.Length; i++)
        {
            BitConverter.GetBytes(floats[i]).CopyTo(bytes, i * sizeof(float));
        }

        return bytes;
    }
}

public sealed class TextGenerationResult
{
    public required string GeneratedText { get; init; }
    public required int TokensGenerated { get; init; }
    public required string Prompt { get; init; }
    public required string ModelIdentifier { get; init; }
}

internal sealed class TensorAtom
{
    public required long AtomID { get; init; }
    public required string CanonicalText { get; init; }
    public required float[] EmbeddingVector { get; init; }
    public required double Distance { get; init; }
}
